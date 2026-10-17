using Pipeline.Domain.Jobs;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Jobs;

[Trait("Category", "Path")]
public class JobStateMachinePathTests
{
    public static IEnumerable<object[]> All64StatePairs()
    {
        var allStatuses = Enum.GetValues<JobStatus>();
        foreach (var from in allStatuses)
        {
            foreach (var to in allStatuses)
            {
                var isLegal = from switch
                {
                    JobStatus.Pending => to is JobStatus.Validating or JobStatus.Cancelled,
                    JobStatus.Validating => to is JobStatus.Running or JobStatus.Failed or JobStatus.Cancelled,
                    JobStatus.Running => to is JobStatus.Succeeded or JobStatus.Failed or JobStatus.RetryScheduled or JobStatus.NeedsReauthorization or JobStatus.Cancelled,
                    JobStatus.Failed => to is JobStatus.RetryScheduled,
                    JobStatus.RetryScheduled => to is JobStatus.Running or JobStatus.Cancelled,
                    JobStatus.NeedsReauthorization => to is JobStatus.Running or JobStatus.Cancelled,
                    JobStatus.Succeeded => false,
                    JobStatus.Cancelled => false,
                    _ => false
                };

                yield return new object[] { from, to, isLegal };
            }
        }
    }

    [Theory]
    [MemberData(nameof(All64StatePairs))]
    public void Transition_AllPairs_MatchLegalTable(JobStatus from, JobStatus to, bool expectedLegal)
    {
        var canTransition = JobStateMachine.CanTransition(from, to);
        canTransition.ShouldBe(expectedLegal);

        var result = JobStateMachine.Transition(from, to);
        result.IsSuccess.ShouldBe(expectedLegal);
        if (expectedLegal)
        {
            result.Value.ShouldBe(to);
        }
        else
        {
            result.ErrorCode.ShouldBe("ILLEGAL_STATE_TRANSITION");
        }
    }

    [Fact]
    public void Terminal_States_CannotTransition()
    {
        JobStateMachine.IsTerminal(JobStatus.Succeeded).ShouldBeTrue();
        JobStateMachine.IsTerminal(JobStatus.Cancelled).ShouldBeTrue();
        JobStateMachine.IsTerminal(JobStatus.Running).ShouldBeFalse();

        foreach (var target in Enum.GetValues<JobStatus>())
        {
            JobStateMachine.CanTransition(JobStatus.Succeeded, target).ShouldBeFalse();
            JobStateMachine.CanTransition(JobStatus.Cancelled, target).ShouldBeFalse();
        }
    }

    [Fact]
    public void AllSimplePaths_PendingToTerminal_AreLegal()
    {
        var pathsToSucceeded = JobStateMachine.GetAllSimplePaths(JobStatus.Pending, JobStatus.Succeeded);
        pathsToSucceeded.ShouldNotBeEmpty();

        foreach (var path in pathsToSucceeded)
        {
            path[0].ShouldBe(JobStatus.Pending);
            path[^1].ShouldBe(JobStatus.Succeeded);

            for (var i = 0; i < path.Count - 1; i++)
            {
                var hopLegal = JobStateMachine.CanTransition(path[i], path[i + 1]);
                hopLegal.ShouldBeTrue($"Hop from {path[i]} to {path[i + 1]} in path must be legal");
            }
        }

        var pathsToCancelled = JobStateMachine.GetAllSimplePaths(JobStatus.Pending, JobStatus.Cancelled);
        pathsToCancelled.ShouldNotBeEmpty();
        foreach (var path in pathsToCancelled)
        {
            for (var i = 0; i < path.Count - 1; i++)
            {
                JobStateMachine.CanTransition(path[i], path[i + 1]).ShouldBeTrue();
            }
        }
    }
}
