// Rules: R-01 Single bridge door, B-02 Inspect job status on write timeout, N-06, N-07
import React, { useState, useMemo, useCallback } from 'react';
import { View, Text, Pressable } from 'react-native';
import {
  SurfaceCard,
  SectionHeader,
  ActionButton,
  Badge,
  BadgeTone,
  EmptyState,
} from '../../../ui';
import { JOB_STATUSES, JobStatus } from '../../../contracts/jobStatus';
import { usePendingOperationsStore } from '../../../state/stores/pendingOperationsStore';
import { breadcrumbs } from '../../../observability/breadcrumbs';
import { createCorrelationId } from '../../../core/ids/correlationId';
import { JobsApi, JobRecordItem } from '../api/jobsApi';

export interface JobsWorkspaceScreenProps {
  readonly api: JobsApi;
}

const STATUS_TONE_MAP: Record<JobStatus, BadgeTone> = {
  Pending: 'neutral',
  Validating: 'info',
  Running: 'info',
  Failed: 'danger',
  Succeeded: 'success',
  RetryScheduled: 'warning',
  NeedsReauthorization: 'warning',
  Cancelled: 'neutral',
};

export function JobsWorkspaceScreen({ api: jobsApi }: JobsWorkspaceScreenProps) {
  const pendingOpsMap = usePendingOperationsStore((s) => s.operations);
  const pendingOps = useMemo(() => Array.from(pendingOpsMap.values()), [pendingOpsMap]);

  const [selectedFilter, setSelectedFilter] = useState<'ALL' | JobStatus>('ALL');
  const [jobs, setJobs] = useState<JobRecordItem[]>([]);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const recordedEvents = breadcrumbs.getAll();

  const handleRefreshJobs = useCallback(async () => {
    setIsRefreshing(true);
    const correlationId = createCorrelationId();
    const res = await jobsApi.listJobs(correlationId);
    setIsRefreshing(false);
    if (res.ok && Array.isArray(res.value?.jobs)) {
      setJobs(res.value.jobs);
    }
  }, [jobsApi]);

  const filteredJobs = useMemo(() => {
    if (selectedFilter === 'ALL') return jobs;
    return jobs.filter((j) => j.status === selectedFilter);
  }, [jobs, selectedFilter]);

  return (
    <View className="space-y-6">
      {/* Live In-Flight Operations from PendingOperationsStore */}
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="In-Flight Operations"
          description="Active mutations currently awaiting confirmation or host completion."
          action={
            <ActionButton
              label="Refresh Status"
              onPress={handleRefreshJobs}
              isSubmitting={isRefreshing}
              submittingLabel="Refreshing…"
              variant="secondary"
            />
          }
        />

        {pendingOps.length === 0 ? (
          <Text className="text-sm text-canvas-500 dark:text-canvas-400">
            No operations are currently in flight.
          </Text>
        ) : (
          <View className="space-y-2">
            {pendingOps.map((op) => (
              <SurfaceCard key={op.idempotencyKey} variant="subtle" padding="sm">
                <View className="flex-row items-center justify-between">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    {op.command}
                  </Text>
                  <Badge label="Running" tone="info" />
                </View>
                <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400 mt-1">
                  Idempotency Key: {op.idempotencyKey}
                </Text>
              </SurfaceCard>
            ))}
          </View>
        )}
      </SurfaceCard>

      {/* Filterable Backend Job Queue */}
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Job History & Status"
          description="Filter background jobs by execution state. Never retry a write job until its status is confirmed."
        />

        <View className="flex-row flex-wrap gap-2 mb-4">
          <Pressable
            accessibilityRole="button"
            onPress={() => setSelectedFilter('ALL')}
            className={`px-3 py-1.5 rounded-sm border ${
              selectedFilter === 'ALL'
                ? 'bg-accent-50 dark:bg-accent-950 border-accent-600'
                : 'bg-canvas-100 dark:bg-canvas-800 border-canvas-200 dark:border-canvas-700'
            }`}
          >
            <Text
              className={`text-xs font-medium ${
                selectedFilter === 'ALL'
                  ? 'text-accent-600 dark:text-accent-400'
                  : 'text-canvas-900 dark:text-canvas-50'
              }`}
            >
              All ({jobs.length})
            </Text>
          </Pressable>

          {JOB_STATUSES.map((status) => {
            const active = selectedFilter === status;
            return (
              <Pressable
                key={status}
                accessibilityRole="button"
                onPress={() => setSelectedFilter(status)}
                className={`px-3 py-1.5 rounded-sm border ${
                  active
                    ? 'bg-accent-50 dark:bg-accent-950 border-accent-600'
                    : 'bg-canvas-100 dark:bg-canvas-800 border-canvas-200 dark:border-canvas-700'
                }`}
              >
                <Text
                  className={`text-xs font-medium ${
                    active
                      ? 'text-accent-600 dark:text-accent-400'
                      : 'text-canvas-900 dark:text-canvas-50'
                  }`}
                >
                  {status}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {filteredJobs.length === 0 ? (
          <EmptyState
            title="No recorded jobs in this view"
            description="No backend jobs have been reported for the selected filter in this session."
            actionLabel="Query Host Jobs"
            onAction={handleRefreshJobs}
          />
        ) : (
          <View className="space-y-3">
            {filteredJobs.map((job) => (
              <SurfaceCard key={job.id} variant="subtle" padding="md">
                <View className="flex-row items-center justify-between">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    {job.command}
                  </Text>
                  <Badge label={job.status} tone={STATUS_TONE_MAP[job.status]} />
                </View>
                <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
                  Updated: {job.updatedAtUtc} • Ref: {job.correlationId}
                </Text>
              </SurfaceCard>
            ))}
          </View>
        )}
      </SurfaceCard>

      {/* Real Session Audit Trail from Observability Ring Buffer */}
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Session Event Log"
          description="Real-time audit trail captured during your current session."
        />
        {recordedEvents.length === 0 ? (
          <Text className="text-sm text-canvas-500 dark:text-canvas-400">
            No user actions or bridge events recorded in this session yet.
          </Text>
        ) : (
          <View className="space-y-2">
            {recordedEvents.slice(-8).reverse().map((entry, idx) => (
              <View
                key={`${entry.timestamp}-${idx}`}
                className="py-2 border-b border-canvas-200 dark:border-canvas-800 flex-row items-center justify-between"
              >
                <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
                  {entry.event}
                </Text>
                <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400">
                  {entry.timestamp.slice(11, 19)} UTC
                </Text>
              </View>
            ))}
          </View>
        )}
      </SurfaceCard>
    </View>
  );
}

