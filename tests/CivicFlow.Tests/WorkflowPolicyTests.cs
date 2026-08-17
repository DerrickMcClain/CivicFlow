using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;

namespace CivicFlow.Tests;

public class WorkflowPolicyTests
{
    [Fact]
    public void Citizen_owner_can_submit_draft()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen, isOwner: true));
    }

    [Fact]
    public void Citizen_non_owner_cannot_submit()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen, isOwner: false));
    }

    [Fact]
    public void Employee_cannot_approve()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Employee, isOwner: false));
    }

    [Fact]
    public void Supervisor_can_approve_from_supervisor_review()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Completed_is_immutable()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Completed, RequestStatusName.UnderReview, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Citizen_cannot_cancel_under_review()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.UnderReview, RequestStatusName.Cancelled, RoleName.Citizen, isOwner: true));
    }

    [Fact]
    public void Supervisor_cannot_cancel_approved()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Approved, RequestStatusName.Cancelled, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Citizen_owner_response_returns_to_under_review()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Citizen, isOwner: true));
    }

    [Theory]
    [InlineData(RequestStatusName.Completed)]
    [InlineData(RequestStatusName.Cancelled)]
    public void Terminal_statuses(RequestStatusName status)
    {
        Assert.True(WorkflowPolicy.IsTerminal(status));
    }
}
