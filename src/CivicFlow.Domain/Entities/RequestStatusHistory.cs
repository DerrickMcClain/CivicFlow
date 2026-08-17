namespace CivicFlow.Domain.Entities;

public class RequestStatusHistory
{
    public int HistoryId { get; set; }
    public int RequestId { get; set; }
    public int? OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public int ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public RequestStatus? OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
