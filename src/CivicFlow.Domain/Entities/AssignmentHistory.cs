namespace CivicFlow.Domain.Entities;

public class AssignmentHistory
{
    public int AssignmentId { get; set; }
    public int RequestId { get; set; }
    public int? AssignedFromUserId { get; set; }
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? Reason { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public User? AssignedFromUser { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
