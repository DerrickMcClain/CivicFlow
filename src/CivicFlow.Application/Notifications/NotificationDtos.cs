namespace CivicFlow.Application.Notifications;

public sealed class NotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkPath { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
