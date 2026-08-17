using CivicFlow.Domain.Enums;

namespace CivicFlow.Application.Requests;

public sealed class CreateRequestRequest
{
    public int RequestTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.Medium;
}

public sealed class RespondRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class ChangeStatusRequest
{
    public RequestStatusName Status { get; set; }
    public string? Reason { get; set; }
}

public sealed class AddNoteRequest
{
    public string NoteText { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = true;
}

public sealed class AssignRequest
{
    public int AssignedToUserId { get; set; }
    public string? Reason { get; set; }
}

public sealed class DecisionRequest
{
    public string? Reason { get; set; }
}

public sealed class SupervisorDashboardDto
{
    public int OpenCount { get; set; }
    public int CompletedCount { get; set; }
    public int AgingOverSevenDaysCount { get; set; }
}

public sealed class StaffAssigneeDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class ServiceRequestListDto
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public sealed class ServiceRequestDetailDto
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public string RequestTypeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? AssignedEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<NoteDto> Notes { get; set; } = [];
    public IReadOnlyList<StatusHistoryDto> History { get; set; } = [];
}

public sealed class NoteDto
{
    public int NoteId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsInternal { get; set; }
}

public sealed class StatusHistoryDto
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
