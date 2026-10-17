using Pipeline.Domain.Common;

namespace Pipeline.Domain.Jobs;

public static class JobStateMachine
{
    private static readonly IReadOnlyDictionary<JobStatus, HashSet<JobStatus>> LegalTransitions =
        new Dictionary<JobStatus, HashSet<JobStatus>>
        {
            [JobStatus.Pending] = [JobStatus.Validating, JobStatus.Cancelled],
            [JobStatus.Validating] = [JobStatus.Running, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Running] = [JobStatus.Succeeded, JobStatus.Failed, JobStatus.RetryScheduled, JobStatus.NeedsReauthorization, JobStatus.Cancelled],
            [JobStatus.Failed] = [JobStatus.RetryScheduled],
            [JobStatus.RetryScheduled] = [JobStatus.Running, JobStatus.Cancelled],
            [JobStatus.NeedsReauthorization] = [JobStatus.Running, JobStatus.Cancelled],
            [JobStatus.Succeeded] = [],
            [JobStatus.Cancelled] = []
        };

    public static bool IsTerminal(JobStatus status) =>
        status is JobStatus.Succeeded or JobStatus.Cancelled;

    public static bool CanTransition(JobStatus from, JobStatus to)
    {
        if (IsTerminal(from))
        {
            return false;
        }

        return LegalTransitions.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    public static Result<JobStatus> Transition(JobStatus from, JobStatus to)
    {
        if (!CanTransition(from, to))
        {
            return Result<JobStatus>.Failure(
                "ILLEGAL_STATE_TRANSITION",
                $"Cannot transition job from {from} to {to}");
        }

        return Result<JobStatus>.Success(to);
    }

    public static IReadOnlySet<JobStatus> GetLegalTargets(JobStatus from) =>
        LegalTransitions.TryGetValue(from, out var targets) ? targets : new HashSet<JobStatus>();

    public static IReadOnlyList<IReadOnlyList<JobStatus>> GetAllSimplePaths(JobStatus start, JobStatus target)
    {
        var results = new List<IReadOnlyList<JobStatus>>();
        var currentPath = new List<JobStatus> { start };
        var visited = new HashSet<JobStatus> { start };

        void Dfs(JobStatus current)
        {
            if (current == target)
            {
                results.Add([.. currentPath]);
                return;
            }

            foreach (var next in GetLegalTargets(current))
            {
                if (visited.Add(next))
                {
                    currentPath.Add(next);
                    Dfs(next);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    visited.Remove(next);
                }
            }
        }

        Dfs(start);
        return results;
    }
}
