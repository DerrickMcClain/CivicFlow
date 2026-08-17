using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class ServiceRequest
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int CitizenId { get; set; }
    public int RequestTypeId { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public int StatusId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public User Citizen { get; set; } = null!;
    public ServiceRequestType RequestType { get; set; } = null!;
    public User? AssignedEmployee { get; set; }
    public RequestStatus Status { get; set; } = null!;
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
    public ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();
    public ICollection<AssignmentHistory> Assignments { get; set; } = new List<AssignmentHistory>();
}
