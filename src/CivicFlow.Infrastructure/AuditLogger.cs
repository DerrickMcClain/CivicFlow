using CivicFlow.Application.Abstractions;
using CivicFlow.Domain.Entities;

namespace CivicFlow.Infrastructure;

public sealed class AuditLogger(IAppDbContext db) : IAuditLogger
{
    public Task WriteAsync(
        int? userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ip,
            Timestamp = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }
}
