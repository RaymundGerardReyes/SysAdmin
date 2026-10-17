using Pipeline.Domain.Jobs;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Jobs;

[Trait("Category", "Unit")]
public class JobStateMachineTests
{
    [Fact]
    public void Pending_CanTransitionTo_Validating()
    {
        var result = JobStateMachine.Transition(JobStatus.Pending, JobStatus.Validating);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(JobStatus.Validating);
    }

    [Fact]
    public void Running_CanTransitionTo_NeedsReauthorization()
    {
        var result = JobStateMachine.Transition(JobStatus.Running, JobStatus.NeedsReauthorization);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(JobStatus.NeedsReauthorization);
    }

    [Fact]
    public void DirectJump_PendingToSucceeded_Fails()
    {
        var result = JobStateMachine.Transition(JobStatus.Pending, JobStatus.Succeeded);
        result.IsFailure.ShouldBeTrue();
    }
}
