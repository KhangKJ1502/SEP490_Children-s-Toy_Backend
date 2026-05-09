using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Interfaces.Notifications;

namespace ToyStore.Infrastructure.Notifications;

/// <summary>
/// SendGrid email sender. Configure "SendGrid:ApiKey" in appsettings.
/// Install SendGrid NuGet package (SendGrid >= 9.x) when integrating.
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IConfiguration config, ILogger<SendGridEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SendGrid:ApiKey not configured — email skipped To={Email}", message.ToEmail);
            return;
        }

        // TODO: Replace with actual SendGrid client call when SendGrid NuGet is installed:
        //   var client = new SendGridClient(apiKey);
        //   var from   = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
        //   var to     = new EmailAddress(message.ToEmail, message.ToName);
        //   var msg    = MailHelper.CreateSingleEmail(from, to, message.Subject, message.PlainTextBody, message.HtmlBody);
        //   var resp   = await client.SendEmailAsync(msg, ct);
        //   if (!resp.IsSuccessStatusCode) throw new InvalidOperationException($"SendGrid failed: {resp.StatusCode}");

        _logger.LogInformation(
            "[SendGrid stub] Would send email To={Email} Subject={Subject}",
            message.ToEmail, message.Subject);

        await Task.CompletedTask;
    }
}
