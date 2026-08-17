namespace CivicFlow.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkPath { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}
