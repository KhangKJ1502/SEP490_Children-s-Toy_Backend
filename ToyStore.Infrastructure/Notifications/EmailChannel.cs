using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Notifications;

/// <summary>
/// Kênh EMAIL: INSERT Delivery với EmailStatus=Pending, gửi SMTP ngay, cập nhật trạng thái.
/// EmailDispatchJob vẫn chạy như "safety net" cho những Delivery Pending cũ (nếu có),
/// nhưng các delivery mới được gửi ngay tại đây.
/// </summary>
public class EmailChannel : INotificationChannel
{
    public string Channel => NotificationChannels.Email;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        ITimeProvider timeProvider,
        ILogger<EmailChannel> logger)
    {
        _unitOfWork  = unitOfWork;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger      = logger;
    }

    public async Task SendAsync(NotificationDeliveryRequest request, CancellationToken ct = default)
    {
        var delivery = new Delivery
        {
            AccountId        = request.AccountId,
            RecipientType    = request.RecipientType,
            Channel          = NotificationChannels.Email,
            NotificationType = request.NotificationType,
            Title            = request.Title,
            Message          = request.Message,
            Payload          = request.PayloadJson ?? "{}",
            Status           = NotificationStatuses.Unread,
            EmailStatus      = EmailStatuses.Pending,
            TemplateCode     = request.TemplateCode,
            ImageUrl         = request.ImageUrl,
            ActionType       = request.ActionType,
            ActionTarget     = request.ActionTarget,
            IdempotencyKey   = request.IdempotencyKey,
            CampaignId       = request.CampaignId,
            CreatedAt        = DateTime.Now,
        };

        try
        {
            _unitOfWork.Deliveries.Add(delivery);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _unitOfWork.Detach(delivery);
            _logger.LogWarning(ex,
                "Could not save email delivery (possible duplicate). Key={Key}",
                request.IdempotencyKey);
            return;
        }

        // Lấy email thực của người nhận từ Account
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct);
        if (account is null)
        {
            _logger.LogWarning(
                "Account not found for email send. AccountID={Id} DeliveryID={DeliveryId}",
                request.AccountId, delivery.DeliveryId);
            delivery.EmailStatus = EmailStatuses.Failed;
            delivery.UpdatedAt   = _timeProvider.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        try
        {
            var emailMsg = new EmailMessage(
                ToEmail:  account.Email,
                ToName:   account.AccountName,
                Subject:  request.Title,
                HtmlBody: BuildHtmlBody(request.Title, request.Message, request.ActionTarget));

            await _emailSender.SendAsync(emailMsg, ct);

            delivery.EmailStatus = EmailStatuses.Sent;
            delivery.UpdatedAt   = _timeProvider.UtcNow;

            _logger.LogInformation(
                "Email sent. DeliveryID={Id} To={Email}",
                delivery.DeliveryId, account.Email);
        }
        catch (Exception ex)
        {
            delivery.EmailStatus = EmailStatuses.Failed;
            delivery.UpdatedAt   = _timeProvider.UtcNow;

            _logger.LogError(ex,
                "Email send failed. DeliveryID={Id} To={Email}",
                delivery.DeliveryId, account.Email);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string BuildHtmlBody(string title, string message, string? actionTarget)
    {
        var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
        // Replace newlines with <br> tags so multi-line messages render correctly
        var encodedMessage = System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br/>");

        var linkSection = actionTarget is not null
            ? $@"
              <div style=""text-align:center;margin-top:32px;"">
                <a href=""{System.Net.WebUtility.HtmlEncode(actionTarget)}"" style=""display:inline-block;background-color:#ff6a00;color:#ffffff;text-decoration:none;padding:14px 28px;border-radius:8px;font-size:15px;font-weight:600;letter-spacing:0.5px;"">
                  View Details
                </a>
              </div>"
            : string.Empty;

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{encodedTitle}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">
          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#ff6a00 0%,#ff9500 100%);padding:32px 40px;text-align:center;"">
              <div style=""display:inline-block;background:rgba(255,255,255,0.15);border-radius:50%;width:56px;height:56px;line-height:56px;margin-bottom:12px;font-size:28px;"">🔔</div>
              <h1 style=""margin:0;color:#ffffff;font-size:24px;font-weight:700;letter-spacing:-0.5px;"">ToyStore</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding:40px;"">
              <h2 style=""margin:0 0 16px;color:#1a1a2e;font-size:20px;font-weight:600;"">{encodedTitle}</h2>
              <div style=""margin:0;color:#555b6e;font-size:15px;line-height:1.6;"">
                {encodedMessage}
              </div>
              {linkSection}
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background:#f8f9fa;padding:24px 40px;border-top:1px solid #e9ecef;"">
              <p style=""margin:0;color:#aaa;font-size:12px;text-align:center;"">
                © {DateTime.Now.Year} ToyStore - Premium Kids Toys. All rights reserved.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
