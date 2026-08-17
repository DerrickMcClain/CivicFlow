using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Notifications;

public sealed class NotificationService(IAppDbContext db, IEmailSender emailSender)
{
    public Task NotifyCaseAssignedAsync(
        int assigneeUserId,
        string requestNumber,
        int requestId,
        CancellationToken cancellationToken = default) =>
        NotifyAsync(
            assigneeUserId,
            "Case assigned to you",
            $"Case {requestNumber} was assigned to you.",
            $"/staff/requests/{requestId}",
            cancellationToken);

    public Task NotifyCitizenStatusAsync(
        int citizenId,
        string requestNumber,
        int requestId,
        RequestStatusName status,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var (title, message) = status switch
        {
            RequestStatusName.AdditionalInfoRequired => (
                "More information needed",
                $"Case {requestNumber} needs additional information from you."),
            RequestStatusName.Approved => (
                "Case approved",
                $"Case {requestNumber} was approved."),
            RequestStatusName.Rejected => (
                "Case rejected",
                $"Case {requestNumber} was rejected.{(string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}")}"),
            RequestStatusName.Completed => (
                "Case completed",
                $"Case {requestNumber} is now completed."),
            _ => (
                "Case status updated",
                $"Case {requestNumber} is now {status}.")
        };

        return NotifyAsync(
            citizenId,
            title,
            message,
            $"/citizen/requests/{requestId}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await db.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new NotificationDto
            {
                NotificationId = x.NotificationId,
                Title = x.Title,
                Message = x.Message,
                LinkPath = x.LinkPath,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        await db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), cancellationToken);
    }

    private async Task NotifyAsync(
        int userId,
        string title,
        string message,
        string? linkPath,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (user is null)
        {
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkPath = linkPath,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await emailSender.SendAsync(user.Email, title, message, cancellationToken);
    }
}
