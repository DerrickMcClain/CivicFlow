using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Workflow;

public static class WorkflowPolicy
{
    public static bool IsTerminal(RequestStatusName status) =>
        status is RequestStatusName.Completed or RequestStatusName.Cancelled;

    public static bool CanTransition(
        RequestStatusName from,
        RequestStatusName to,
        RoleName role,
        bool isOwner)
    {
        if (IsTerminal(from))
        {
            return false;
        }

        return (from, to, role) switch
        {
            (RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen) => isOwner,
            (RequestStatusName.Submitted, RequestStatusName.UnderReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.UnderReview, RequestStatusName.AdditionalInfoRequired, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Citizen) => isOwner,
            (RequestStatusName.UnderReview, RequestStatusName.EmployeeRecommendation, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.EmployeeRecommendation, RequestStatusName.SupervisorReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Supervisor) => true,
            (RequestStatusName.SupervisorReview, RequestStatusName.Rejected, RoleName.Supervisor) => true,
            (RequestStatusName.Approved, RequestStatusName.Completed, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.Rejected, RequestStatusName.Completed, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.Draft, RequestStatusName.Cancelled, RoleName.Citizen) => isOwner,
            (RequestStatusName.Submitted, RequestStatusName.Cancelled, RoleName.Citizen) => isOwner,
            (_, RequestStatusName.Cancelled, RoleName.Supervisor or RoleName.Administrator)
                when from is not RequestStatusName.Approved and not RequestStatusName.Rejected => true,
            _ => false
        };
    }
}
