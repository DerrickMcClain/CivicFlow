using System.Net;
using System.Net.Mail;
using CivicFlow.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CivicFlow.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost is required for SMTP email.");
        var port = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        var from = configuration["Email:FromAddress"] ?? "noreply@civicflow.local";
        var user = configuration["Email:Username"];
        var password = configuration["Email:Password"];

        using var message = new MailMessage(from, toEmail, subject, body);
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.Parse(configuration["Email:UseSsl"] ?? "true")
        };

        if (!string.IsNullOrWhiteSpace(user))
        {
            client.Credentials = new NetworkCredential(user, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
