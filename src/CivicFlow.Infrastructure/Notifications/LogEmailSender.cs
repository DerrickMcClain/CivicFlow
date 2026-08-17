using CivicFlow.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CivicFlow.Infrastructure.Notifications;

public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email notification to {Email}: {Subject} — {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
