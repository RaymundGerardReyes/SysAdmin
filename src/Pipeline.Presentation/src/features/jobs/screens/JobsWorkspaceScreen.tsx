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
  IntentStatusBanner,
  useAppTheme,
} from '../../../ui';
import { JOB_STATUSES, JobStatus } from '../../../contracts/jobStatus';
import { usePendingOperationsStore } from '../../../state/stores/pendingOperationsStore';
import { breadcrumbs } from '../../../observability/breadcrumbs';
import { createCorrelationId } from '../../../core/ids/correlationId';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { JobsApi, JobRecordItem, createJobsApi } from '../api/jobsApi';

export interface JobsWorkspaceScreenProps {
  readonly api?: JobsApi | undefined;
  readonly bridge?: Parameters<typeof createJobsApi>[0] | undefined;
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

export function JobsWorkspaceScreen({ api, bridge }: JobsWorkspaceScreenProps) {
  const { isCompact } = useAppTheme();

  const jobsApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createJobsApi(bridge);
    throw new Error('JobsWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const pendingOpsMap = usePendingOperationsStore((s) => s.operations);
  const pendingOps = useMemo(() => Array.from(pendingOpsMap.values()), [pendingOpsMap]);

  const [selectedFilter, setSelectedFilter] = useState<'ALL' | JobStatus>('ALL');
  const [jobs, setJobs] = useState<readonly JobRecordItem[]>([]);
  const [selectedJob, setSelectedJob] = useState<JobRecordItem | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const recordedEvents = breadcrumbs.getAll();

  const handleRefreshJobs = useCallback(async () => {
    setIsRefreshing(true);
    const correlationId = createCorrelationId();
    const res = await jobsApi.listJobs(correlationId);
    setIsRefreshing(false);
    if (res.ok && Array.isArray(res.value?.jobs)) {
      setJobs(res.value.jobs);
      if (selectedJob) {
        const updatedSelected = res.value.jobs.find((j) => j.id === selectedJob.id);
        if (updatedSelected) setSelectedJob(updatedSelected);
      }
    }
  }, [jobsApi, selectedJob]);

  // Cancel Job Mutation (Tier T1 Mutation)
  const cancelMutation = useCallback(
    async (payload: { jobId: string }, correlationId: any, idempotencyKey: any) => {
      const res = await jobsApi.cancelJob(payload.jobId, correlationId, idempotencyKey);
      if (res.ok) {
        // Update local jobs list
        setJobs((prev) =>
          prev.map((j) => (j.id === payload.jobId ? { ...j, status: 'Cancelled' as JobStatus } : j))
        );
        setSelectedJob((prev) =>
          prev && prev.id === payload.jobId ? { ...prev, status: 'Cancelled' as JobStatus } : prev
        );
      }
      return res;
    },
    [jobsApi]
  );

  const {
    state: cancelIntent,
    execute: executeCancel,
    confirm: confirmCancel,
    reset: resetCancel,
  } = useMutationIntent(cancelMutation, 'jobs.cancel');

  const handleCancelSelectedJob = () => {
    if (!selectedJob) return;
    executeCancel({ jobId: selectedJob.id }, true);
  };

  const filteredJobs = useMemo(() => {
    if (selectedFilter === 'ALL') return jobs;
    return jobs.filter((j) => j.status === selectedFilter);
  }, [jobs, selectedFilter]);

  const isCancelable =
    selectedJob &&
    (selectedJob.status === 'Running' ||
      selectedJob.status === 'Pending' ||
      selectedJob.status === 'RetryScheduled');

  return (
    <View className="gap-6">
      <IntentStatusBanner
        state={cancelIntent}
        operationSummary={`Cancel execution of background job "${selectedJob?.id ?? ''}" (${selectedJob?.command ?? ''})`}
        onConfirm={confirmCancel}
        onReset={resetCancel}
      />

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
          <View className="gap-2">
            {pendingOps.map((op) => (
              <SurfaceCard key={op.idempotencyKey} variant="subtle" padding="sm">
                <View className="flex-row items-center justify-between">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    {op.command}
                  </Text>
                  <Badge label="Running" tone="info" />
                </View>
                <Text className="text-xs font-mono text-canvas-500 dark:text-canvas-400 mt-1">
                  Idempotency Key: {op.idempotencyKey} • Started: {new Date(op.startedAt).toLocaleTimeString()}
                </Text>
              </SurfaceCard>
            ))}
          </View>
        )}
      </SurfaceCard>

      {/* Master-Detail Filterable Backend Job Queue */}
      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Job History & Status"
          description="Filter background jobs by execution state. Select any job to inspect details or safely cancel execution."
        />

        {/* Filter Tabs */}
        <View className="flex-row flex-wrap gap-2 mb-4">
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Filter all jobs"
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
                  ? 'text-accent-600 dark:text-accent-400 font-semibold'
                  : 'text-canvas-900 dark:text-canvas-50'
              }`}
            >
              All ({jobs.length})
            </Text>
          </Pressable>

          {JOB_STATUSES.map((status) => {
            const active = selectedFilter === status;
            const count = jobs.filter((j) => j.status === status).length;
            return (
              <Pressable
                key={status}
                accessibilityRole="button"
                accessibilityLabel={`Filter ${status} jobs`}
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
                      ? 'text-accent-600 dark:text-accent-400 font-semibold'
                      : 'text-canvas-900 dark:text-canvas-50'
                  }`}
                >
                  {status} ({count})
                </Text>
              </Pressable>
            );
          })}
        </View>

        {/* Master List + Detail Inspector */}
        <View className={isCompact ? 'flex-col gap-4' : 'flex-row gap-6'}>
          {/* Master Job List */}
          <View className={selectedJob && !isCompact ? 'w-1/2' : 'w-full'}>
            {filteredJobs.length === 0 ? (
              <EmptyState
                title="No recorded jobs in this view"
                description="No backend jobs match the selected filter."
                actionLabel="Query Host Jobs"
                onAction={handleRefreshJobs}
              />
            ) : (
              <View className="gap-3">
                {filteredJobs.map((job) => {
                  const isSelected = selectedJob?.id === job.id;
                  return (
                    <Pressable
                      key={job.id}
                      accessibilityRole="button"
                      accessibilityLabel={`Inspect job ${job.command}`}
                      onPress={() => setSelectedJob(job)}
                    >
                      <SurfaceCard
                        variant={isSelected ? 'accent' : 'subtle'}
                        padding="md"
                      >
                        <View className="flex-row items-center justify-between">
                          <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                            {job.command}
                          </Text>
                          <Badge label={job.status} tone={STATUS_TONE_MAP[job.status]} />
                        </View>
                        <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
                          Updated: {job.updatedAtUtc} • ID: {job.id}
                        </Text>
                      </SurfaceCard>
                    </Pressable>
                  );
                })}
              </View>
            )}
          </View>

          {/* Detail Inspector Pane */}
          {selectedJob && (
            <View className={!isCompact ? 'w-1/2' : 'w-full'}>
              <SurfaceCard variant="elevated" padding="md">
                <View className="flex-row items-center justify-between mb-3 border-b border-canvas-200 dark:border-canvas-700 pb-2">
                  <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50">
                    Job Inspector
                  </Text>
                  <Pressable
                    accessibilityRole="button"
                    accessibilityLabel="Close inspector"
                    onPress={() => setSelectedJob(null)}
                    className="p-1"
                  >
                    <Text className="text-xs text-canvas-400 dark:text-canvas-500 font-bold">✕ Close</Text>
                  </Pressable>
                </View>

                <View className="gap-2 mb-4">
                  <View className="flex-row justify-between py-1 border-b border-canvas-100 dark:border-canvas-800">
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400">Command</Text>
                    <Text className="text-xs font-semibold text-canvas-900 dark:text-canvas-50">
                      {selectedJob.command}
                    </Text>
                  </View>
                  <View className="flex-row justify-between py-1 border-b border-canvas-100 dark:border-canvas-800">
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400">Status</Text>
                    <Badge label={selectedJob.status} tone={STATUS_TONE_MAP[selectedJob.status]} />
                  </View>
                  <View className="flex-row justify-between py-1 border-b border-canvas-100 dark:border-canvas-800">
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400">Job ID</Text>
                    <Text className="text-xs font-mono text-canvas-900 dark:text-canvas-50">
                      {selectedJob.id}
                    </Text>
                  </View>
                  <View className="flex-row justify-between py-1 border-b border-canvas-100 dark:border-canvas-800">
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400">Correlation ID</Text>
                    <Text className="text-xs font-mono text-canvas-900 dark:text-canvas-50">
                      {selectedJob.correlationId}
                    </Text>
                  </View>
                  <View className="flex-row justify-between py-1">
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400">Last Update</Text>
                    <Text className="text-xs text-canvas-900 dark:text-canvas-50">
                      {selectedJob.updatedAtUtc}
                    </Text>
                  </View>
                </View>

                {/* Safe Cancellation Button */}
                {isCancelable && (
                  <View className="mt-3 pt-3 border-t border-canvas-200 dark:border-canvas-700">
                    <ActionButton
                      label="Cancel Running Job"
                      variant="destructive"
                      isSubmitting={cancelIntent.phase === 'Submitting'}
                      submittingLabel="Canceling job…"
                      onPress={handleCancelSelectedJob}
                    />
                    <Text className="text-xs text-canvas-500 dark:text-canvas-400 mt-1">
                      Canceling will request immediate termination of this background job on the host.
                    </Text>
                  </View>
                )}
              </SurfaceCard>
            </View>
          )}
        </View>
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
          <View className="gap-2">
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
