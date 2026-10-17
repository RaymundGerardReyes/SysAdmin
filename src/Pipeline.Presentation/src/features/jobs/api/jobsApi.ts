// Rules: R-01, B-01 Single bridge door for jobs feature
import { PipelineBridge } from '../../../bridge/PipelineBridge';
import { CorrelationId } from '../../../core/ids/correlationId';
import { IdempotencyKey } from '../../../core/ids/idempotencyKey';
import { JobStatus } from '../../../contracts/jobStatus';

export interface JobRecordItem {
  readonly id: string;
  readonly command: string;
  readonly status: JobStatus;
  readonly updatedAtUtc: string;
  readonly correlationId: string;
}

export interface JobsApi {
  listJobs(correlationId: CorrelationId): Promise<{ ok: true; value: { jobs?: JobRecordItem[] } } | { ok: false; error: any }>;
  cancelJob(jobId: string, correlationId: CorrelationId, idempotencyKey: IdempotencyKey): Promise<{ ok: true; value: { cancelled: boolean } } | { ok: false; error: any }>;
}

export function createJobsApi(bridge: PipelineBridge): JobsApi {
  return {
    async listJobs(correlationId: CorrelationId) {
      return bridge.command<Record<string, never>, { jobs?: JobRecordItem[] }>(
        'jobs.read',
        {},
        correlationId
      );
    },
    async cancelJob(jobId: string, correlationId: CorrelationId, idempotencyKey: IdempotencyKey) {
      return bridge.command<{ jobId: string }, { cancelled: boolean }>(
        'jobs.cancel',
        { jobId },
        correlationId,
        idempotencyKey
      );
    },
  };
}

