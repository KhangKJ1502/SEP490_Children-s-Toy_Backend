using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ToyStore.Application.Interfaces.Notifications;

namespace ToyStore.Infrastructure.Notifications;

public class SmtpNotificationEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpNotificationEmailSender> _logger;

    public SmtpNotificationEmailSender(IConfiguration configuration, ILogger<SmtpNotificationEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var host = _configuration["Email:Host"];
        var portStr = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var displayName = _configuration["Email:DisplayName"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Email SMTP not configured properly — email skipped To={Email}", message.ToEmail);
            return;
        }

        var port = int.TryParse(portStr, out var p) ? p : 587;

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(displayName ?? "ToyStore Notification", username));
        
        // Add To address
        var toName = string.IsNullOrWhiteSpace(message.ToName) ? message.ToEmail : message.ToName;
        mimeMessage.To.Add(new MailboxAddress(toName, message.ToEmail));
        
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            bodyBuilder.HtmlBody = message.HtmlBody;
        }
        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            bodyBuilder.TextBody = message.PlainTextBody;
        }
        
        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(mimeMessage, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Notification email sent to {Email} with subject '{Subject}'.", message.ToEmail, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification email to {Email}.", message.ToEmail);
            throw;
        }
    }
}
