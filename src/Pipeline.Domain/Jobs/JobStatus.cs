namespace Pipeline.Domain.Jobs;

public enum JobStatus
{
    Pending = 0,
    Validating = 1,
    Running = 2,
    Failed = 3,
    Succeeded = 4,
    RetryScheduled = 5,
    NeedsReauthorization = 6,
    Cancelled = 7
}
