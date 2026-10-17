export const JOB_STATUSES = [
  'Pending',
  'Validating',
  'Running',
  'Failed',
  'Succeeded',
  'RetryScheduled',
  'NeedsReauthorization',
  'Cancelled',
] as const;

export type JobStatus = typeof JOB_STATUSES[number];
