namespace CivicFlow.Application.Abstractions;

public interface IAuditLogger
{
    Task WriteAsync(
        int? userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? ip,
        CancellationToken cancellationToken = default);
}
