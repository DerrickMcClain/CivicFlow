using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Workflow;

public static class SlaPolicy
{
    public static DateTime ComputeDueAt(Priority priority, DateTime submittedAtUtc) =>
        submittedAtUtc.AddDays(priority switch
        {
            Priority.Low => 10,
            Priority.High => 2,
            _ => 5
        });

    public static bool IsOverdue(RequestStatusName status, DateTime? slaDueAt)
    {
        if (slaDueAt is null)
        {
            return false;
        }

        if (status is RequestStatusName.Completed
            or RequestStatusName.Cancelled
            or RequestStatusName.Approved
            or RequestStatusName.Rejected)
        {
            return false;
        }

        return DateTime.UtcNow > slaDueAt.Value;
    }
}
