using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Constants;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using System.Text.Json;
using System.Text;
using System;
using System.Linq;

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
    private readonly IConfiguration _configuration;

    public EmailChannel(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        ITimeProvider timeProvider,
        ILogger<EmailChannel> logger,
        IConfiguration configuration)
    {
        _unitOfWork  = unitOfWork;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger      = logger;
        _configuration = configuration;
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
            var absoluteActionTarget = string.IsNullOrEmpty(request.ActionTarget)
                ? null
                : request.ActionTarget.StartsWith("http") 
                    ? request.ActionTarget 
                    : (request.RecipientType == RecipientTypes.Customer 
                        ? _configuration["FrontendUrls:Customer"] ?? "http://localhost:3000" 
                        : _configuration["FrontendUrls:Admin"] ?? "http://localhost:3001") + request.ActionTarget;

            var htmlBody = (request.TemplateCode == NotificationTemplates.OrderShipping || request.TemplateCode == NotificationTemplates.OrderPlaced)
                ? await BuildRichOrderShippingEmailAsync(request, absoluteActionTarget, ct)
                : (request.TemplateCode == NotificationTemplates.BirthdayChild)
                    ? BuildBirthdayChildEmail(request, absoluteActionTarget)
                    : BuildHtmlBody(request.Title, request.Message, absoluteActionTarget);

            var emailMsg = new EmailMessage(
                ToEmail:  account.Email,
                ToName:   account.AccountName,
                Subject:  request.Title,
                HtmlBody: htmlBody);

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

    private string BuildBirthdayChildEmail(NotificationDeliveryRequest request, string? actionTarget)
    {
        var childName = "";
        var childNickname = "";
        var childGender = "";
        var childBirthDate = "";

        if (!string.IsNullOrEmpty(request.PayloadJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(request.PayloadJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("childName", out var propName)) childName = propName.GetString() ?? "";
                if (root.TryGetProperty("childNickname", out var propNick)) childNickname = propNick.GetString() ?? "";
                if (root.TryGetProperty("childGender", out var propGender)) childGender = propGender.GetString() ?? "";
                if (root.TryGetProperty("childBirthDate", out var propDob)) childBirthDate = propDob.GetString() ?? "";
            }
            catch {}
        }

        // Fallbacks
        if (string.IsNullOrEmpty(childName)) childName = "your little one";
        if (string.IsNullOrEmpty(childNickname)) childNickname = "N/A";
        if (string.IsNullOrEmpty(childGender)) childGender = "N/A";
        if (string.IsNullOrEmpty(childBirthDate)) childBirthDate = "N/A";

        var url = actionTarget ?? _configuration["FrontendUrls:Customer"] ?? "http://localhost:3000";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8""/>
    <meta content=""width=device-width, initial-scale=1.0"" name=""viewport""/>
    <title>Happy Birthday!</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #fff1ed; font-family: 'Segoe UI', Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #fff1ed; padding: 40px 0;"">
        <tr>
            <td align=""center"">
                <table width=""580"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 8px 30px rgba(169, 49, 0, 0.08); border: 1px solid #cbd5e1;"">
                    <!-- Hero Image Section -->
                    <tr>
                        <td align=""center"" style=""background-color: #ffe9e3; padding: 32px; border-bottom: 1px solid #cbd5e1;"">
                            <img alt=""Happy Birthday Graphic"" width=""300"" style=""display: block; max-width: 100%; height: auto; border: 0;"" src=""https://lh3.googleusercontent.com/aida-public/AB6AXuC-gSAhclG4f1-gtfSw1vynh3SOC1JpFS8mHQ_QT7FSMT1dppYUdINh9hPWsw7AYtB89Q_TnvQWCvdEaNwZlJISfm-ZE9xTdMRbpqbwkPNzijMKqgrBqoRQIa5XdT8t8lQF4ZqbL72y7Nvp8tOl8b8qi4JeAeSsjqAbhiFfHK3zpi7jlq3Jy_JwNLoNuqXbJedecvg8qtpxXOaa15MBdHeY5Edc4R8OjAf_UPgrMfVsa-JCpogu66xnWRF4R_BLsDGAtYJtAOoyMXO5""/>
                        </td>
                    </tr>
                    <!-- Content Body -->
                    <tr>
                        <td style=""padding: 40px 48px; text-align: center;"">
                            <h1 style=""margin: 0 0 20px 0; font-family: 'Segoe UI', Arial, sans-serif; font-size: 32px; font-weight: 700; color: #1e293b; line-height: 1.2;"">
                                Happy Birthday to your little one! 🎂
                            </h1>
                            <p style=""margin: 0 0 32px 0; font-family: 'Segoe UI', Arial, sans-serif; font-size: 16px; color: #64748b; line-height: 1.6;"">
                                Each passing year is a wonderful journey of love and growth. As your little angel welcomes a new age, we send the warmest wishes to both parents and your child. May this special day be filled with joy, laughter, and unforgettable memories!
                            </p>
                            
                            <!-- Birthday Profile Card -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #fafafa; border-radius: 16px; border: 1px solid #cbd5e1; padding: 24px; margin-bottom: 32px;"">
                                <tr>
                                    <td align=""center"">
                                        <h2 style=""margin: 0 0 16px 0; font-family: 'Segoe UI', Arial, sans-serif; font-size: 20px; font-weight: 600; color: #1e293b;"">
                                            🎉 Birthday Profile
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                            <tr>
                                                <td width=""50%"" valign=""top"" style=""padding: 0 8px 16px 0;"">
                                                    <div style=""background-color: #fff1ed; border: 1px solid #ffdbd0; border-radius: 12px; padding: 12px 8px; text-align: center;"">
                                                        <div style=""font-size: 20px; margin-bottom: 2px;"">👤</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 2px;"">NAME</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 15px; font-weight: bold; color: #1e293b;"">{System.Net.WebUtility.HtmlEncode(childName)}</div>
                                                    </div>
                                                </td>
                                                <td width=""50%"" valign=""top"" style=""padding: 0 0 16px 8px;"">
                                                    <div style=""background-color: #fff1ed; border: 1px solid #ffdbd0; border-radius: 12px; padding: 12px 8px; text-align: center;"">
                                                        <div style=""font-size: 20px; margin-bottom: 2px;"">🧸</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 2px;"">NICKNAME</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 15px; font-weight: bold; color: #1e293b;"">{System.Net.WebUtility.HtmlEncode(childNickname)}</div>
                                                    </div>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td width=""50%"" valign=""top"" style=""padding: 0 8px 0 0;"">
                                                    <div style=""background-color: #fff1ed; border: 1px solid #ffdbd0; border-radius: 12px; padding: 12px 8px; text-align: center;"">
                                                        <div style=""font-size: 20px; margin-bottom: 2px;"">🚻</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 2px;"">GENDER</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 15px; font-weight: bold; color: #1e293b;"">{System.Net.WebUtility.HtmlEncode(childGender)}</div>
                                                    </div>
                                                </td>
                                                <td width=""50%"" valign=""top"" style=""padding: 0 0 0 8px;"">
                                                    <div style=""background-color: #fff1ed; border: 1px solid #ffdbd0; border-radius: 12px; padding: 12px 8px; text-align: center;"">
                                                        <div style=""font-size: 20px; margin-bottom: 2px;"">🎂</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 2px;"">BIRTH DATE</div>
                                                        <div style=""font-family: Arial, sans-serif; font-size: 15px; font-weight: bold; color: #1e293b;"">{System.Net.WebUtility.HtmlEncode(childBirthDate)}</div>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            
                            <!-- CTA Button -->
                            <div style=""margin-top: 32px;"">
                                <a href=""{url}"" style=""display: inline-block; background-color: #a93100; color: #ffffff; text-decoration: none; padding: 14px 36px; border-radius: 30px; font-family: 'Segoe UI', Arial, sans-serif; font-size: 16px; font-weight: 600; letter-spacing: 0.5px; box-shadow: 0 4px 12px rgba(169, 49, 0, 0.25);"">
                                    Visit our website &rarr;
                                </a>
                            </div>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 24px; text-align: center; border-top: 1px solid #cbd5e1;"">
                            <p style=""margin: 0; font-family: 'Segoe UI', Arial, sans-serif; color: #94a3b8; font-size: 12px;"">
                                &copy; {DateTime.Now.Year} ToyStore. All rights reserved.
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

    private async Task<string> BuildRichOrderShippingEmailAsync(
        NotificationDeliveryRequest request, 
        string? actionTarget, 
        CancellationToken ct)
    {
        int orderId = 0;
        if (!string.IsNullOrEmpty(request.PayloadJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(request.PayloadJson);
                if (doc.RootElement.TryGetProperty("orderId", out var orderIdProp))
                {
                    orderId = orderIdProp.GetInt32();
                }
            }
            catch {}
        }

        if (orderId == 0 && !string.IsNullOrEmpty(request.ActionTarget))
        {
            var parts = request.ActionTarget.Split('/');
            if (parts.Length > 0 && int.TryParse(parts[^1], out var parsedId))
            {
                orderId = parsedId;
            }
        }

        if (orderId == 0)
        {
            return BuildHtmlBody(request.Title, request.Message, actionTarget);
        }

        var order = await _unitOfWork.Orders.GetByIdForCustomerAsync(orderId, request.AccountId, ct);
        if (order is null)
        {
            return BuildHtmlBody(request.Title, request.Message, actionTarget);
        }

        var itemsHtmlBuilder = new System.Text.StringBuilder();
        foreach (var detail in order.OrderDetails)
        {
            var imageUrl = detail.ProductImage ?? "https://via.placeholder.com/60";
            if (imageUrl.StartsWith("//"))
            {
                imageUrl = "https:" + imageUrl;
            }
            else if (imageUrl.StartsWith("/"))
            {
                var baseUrl = _configuration["BackendUrls:Api"] ?? _configuration["FrontendUrls:Customer"] ?? "http://localhost:5000";
                if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
                imageUrl = baseUrl + imageUrl;
            }
            else if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = _configuration["BackendUrls:Api"] ?? _configuration["FrontendUrls:Customer"] ?? "http://localhost:5000";
                if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
                imageUrl = baseUrl + "/" + imageUrl.Replace("\\", "/");
            }

            var spec = $"Product ID: #{detail.ProductId}";
            itemsHtmlBuilder.Append($@"
                                                                <!-- Product Item -->
                                                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin-bottom:20px;"">
                                                                    <tr>
                                                                        <td width=""80"" align=""left"" valign=""top"">
                                                                            <img src=""{imageUrl}"" width=""80"" height=""65"" style=""display:block; border:0; object-fit:cover; border-radius:8px;"" alt=""{System.Net.WebUtility.HtmlEncode(detail.ProductName)}"" />
                                                                        </td>
                                                                        <td style=""padding-left:15px;"" align=""left"" valign=""middle"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; font-weight:600; color:#141414;"">{System.Net.WebUtility.HtmlEncode(detail.ProductName).ToUpper()}</div>
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:12px; font-weight:600; color:#979da6; letter-spacing:0.9px;"">QUANTITY: {detail.Quantity}</div>
                                                                        </td>
                                                                        <td align=""right"" valign=""top"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; font-weight:600; color:#333;"">{detail.UnitPrice:N0} VND</div>
                                                                        </td>
                                                                    </tr>
                                                                </table>");
        }

        var shippingFeeVal = order.ActualShippingFee ?? order.EstimatedShippingFee;

        var discountText = order.VoucherDiscountAmount > 0 ? $"-{order.VoucherDiscountAmount:N0} VND" : "0 VND";

        var tx = order.ShippingProviderTransactions.FirstOrDefault();
        var expectedDeliveryText = tx?.EstimatedDelivery?.ToString("MM/dd/yyyy") ?? "2-5 business days";

        var paymentMethodDetail = order.PaymentMethod == "SHIP_COD" ? "Cash on Delivery (COD)" : 
                                  order.PaymentMethod == "SE_PAY" ? "Bank Transfer" : 
                                  order.PaymentMethod == "WALLET" ? "ToyStore Wallet" : order.PaymentMethod;

        var paymentStatusText = order.PaymentStatus == "PAID" ? "Paid" : "Pending";
        var paymentStatusBg = order.PaymentStatus == "PAID" ? "#dcfce7" : "#fff3cd";
        var paymentStatusColor = order.PaymentStatus == "PAID" ? "#15803d" : "#856404";

        var trackingUrl = actionTarget ?? "#";

        var isPlaced = request.TemplateCode == NotificationTemplates.OrderPlaced;
        var greetingMsg = isPlaced 
            ? $"Thank you for your order at Toy Store! We've received your order and it is expected to arrive within {expectedDeliveryText}."
            : $"Great news! Your order has been confirmed and shipped. It is expected to arrive within {expectedDeliveryText}.";

        return $@"<!DOCTYPE html>
<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"">
<head>
    <meta charset=""UTF-8"" />
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <!--[if !mso]><!-- -->
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <!--<![endif]-->
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <meta name=""format-detection"" content=""telephone=no, date=no, address=no, email=no"" />
    <meta name=""x-apple-disable-message-reformatting"" />
    <link href=""https://fonts.googleapis.com/css?family=Bebas+Neue:ital,wght@0,400"" rel=""stylesheet"" />
    <link href=""https://fonts.googleapis.com/css?family=Onest:ital,wght@0,400;0,500;0,600"" rel=""stylesheet"" />
    <title>Toy Store - Order Confirmation</title>
    
    <style>
        html, body {{ margin: 0 !important; padding: 0 !important; min-height: 100% !important; width: 100% !important; -webkit-font-smoothing: antialiased; }}
        * {{ -ms-text-size-adjust: 100%; }}
        #outlook a {{ padding: 0; }}
        .ReadMsgBody, .ExternalClass {{ width: 100%; }}
        .ExternalClass, .ExternalClass p, .ExternalClass td, .ExternalClass div, .ExternalClass span, .ExternalClass font {{ line-height: 100%; }}
        table, td, th {{ mso-table-lspace: 0 !important; mso-table-rspace: 0 !important; border-collapse: collapse; }}
        u + .body table, u + .body td, u + .body th {{ will-change: transform; }}
        body, td, th, p, div, li, a, span {{ -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; mso-line-height-rule: exactly; }}
        img {{ border: 0; outline: 0; line-height: 100%; text-decoration: none; -ms-interpolation-mode: bicubic; }}
        a[x-apple-data-detectors] {{ color: inherit !important; text-decoration: none !important; }}
        .body .pc-project-body {{ background-color: transparent !important; }}
        @media (min-width:621px) {{ .pc-lg-hide {{ display: none; }} .pc-lg-bg-img-hide {{ background-image: none !important; }} }}
        @media (max-width:620px){{
            .pc-project-body{{min-width:0 !important;}}
            .pc-project-container,.pc-component{{width:100% !important;}}
            .pc-w620-padding-0-0-0-0{{padding:0 !important;}}
            .pc-w620-padding-20-0-0-0{{padding:20px 0 0 !important;}}
            .pc-w620-itemsVSpacings-30{{padding-top:15px !important;padding-bottom:15px !important;}}
            .pc-w620-itemsHSpacings-0{{padding-left:0 !important;padding-right:0 !important;}}
            .pc-w620-font-size-20px{{font-size:20px !important;}}
            .pc-w620-padding-12-16-12-16{{padding:12px 16px !important;}}
            .pc-w620-width-auto{{width:auto !important;}}
            .pc-w620-padding-12-12-12-12{{padding:12px !important;}}
            .pc-w620-padding-20-20-20-20{{padding:20px !important;}}
        }}
    </style>
    <!--[if !mso]><!-- -->
    <style>
        @font-face {{ font-family: 'Bebas Neue'; font-style: normal; font-weight: 400; src: url('https://fonts.gstatic.com/l/font?kit=JTUSjIg69CK48gW7PXoo9WdhzQ&skey=6bd981f07b300212&v=v16') format('woff2'); }}
        @font-face {{ font-family: 'Onest'; font-style: normal; font-weight: 400; src: url('https://fonts.gstatic.com/l/font?kit=gNMZW3F-SZuj7zOT0IfSjTS16cPh9R-puxtL&skey=45f9610178ce01c5&v=v9') format('woff2'); }}
        @font-face {{ font-family: 'Onest'; font-style: normal; font-weight: 600; src: url('https://fonts.gstatic.com/l/font?kit=gNMZW3F-SZuj7zOT0IfSjTS16cPhKxipuxtL&skey=45f9610178ce01c5&v=v9') format('woff2'); }}
    </style>
    <!--<![endif]-->
    <!--[if mso]><style type=""text/css"">.pc-font-alt{{font-family:Arial,Helvetica,sans-serif !important;}}</style><![endif]-->
</head>

<body class=""body pc-font-alt"" style=""width:100% !important; min-height:100% !important; margin:0 !important; padding:0 !important; background-color:#ffe1d8;"" bgcolor=""#ffe1d8"">
    <table class=""pc-project-body"" style=""table-layout:fixed; width:100%; min-width:600px; background-color:#ffe1d8"" bgcolor=""#ffe1d8"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
        <tr>
            <td align=""center"" valign=""top"">
                
                <table class=""pc-project-container"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                    <tr>
                        <td style=""padding:20px 0"" align=""left"" valign=""top"">
                            
                            <!-- COMPONENT: Greeting and Order Number -->
                            <table class=""pc-component"" style=""width:600px; max-width:600px"" width=""600"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                <tr>
                                    <td width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                        <table width=""100%"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                            <tr>
                                                <td valign=""top"" style=""padding:12px; background-color:#f6f6f6"" bgcolor=""#f6f6f6"">
                                                    <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                        <tr>
                                                            <td align=""left"" valign=""middle"" style=""padding:32px; background-color:#fff; border-radius:0 0 16px 16px"">
                                                                <div style=""font-family:'Onest',Arial,sans-serif; font-size:18px; line-height:140%; color:#141414;"">
                                                                    <span>Hi there 👋</span><br>
                                                                    <span>{greetingMsg}</span>
                                                                </div>
                                                                <!-- Order Date -->
                                                                <div style=""font-family:'Onest',Arial,sans-serif; font-size:15px; font-weight:500; color:#6b7280; margin-top:16px;"">
                                                                    Order Date: {order.OrderDate.ToString("MM/dd/yyyy 'at' HH:mm")}
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td align=""right"" valign=""middle"" style=""padding:12px 12px 12px 30px; background-color:#f6f6f6; border-radius:100px;"">
                                                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                                    <tr>
                                                                        <td align=""left"" valign=""middle"" width=""50%"">
                                                                            <div style=""font-family:'Bebas Neue',Arial,sans-serif; font-size:30px;"">
                                                                                <span style=""color:#979da6;"">ORDER #:</span> 
                                                                                <span style=""color:#283c5c;"">#{order.OrderCode}</span>
                                                                            </div>
                                                                        </td>
                                                                        <td align=""right"" valign=""middle"" width=""50%"">
                                                                            <a href=""{trackingUrl}"" style=""display:inline-block; border-radius:100px; background-color:#141414; padding:12px 20px; font-family:'Onest',Arial,sans-serif; font-weight:500; font-size:16px; color:#fff; text-decoration:none;"">
                                                                                Track your order
                                                                            </a>
                                                                        </td>
                                                                    </tr>
                                                                </table>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- COMPONENT: Order Summary -->
                            <table class=""pc-component"" style=""width:600px; max-width:600px"" width=""600"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                <tr>
                                    <td width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                        <table width=""100%"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                            <tr>
                                                <td valign=""top"" style=""padding:12px; background-color:#f6f6f6"" bgcolor=""#f6f6f6"">
                                                    <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                        <tr>
                                                            <td align=""left"" valign=""middle"" style=""padding:32px; background-color:#fff; border-radius:16px;"">
                                                                
                                                                <div style=""font-family:'Bebas Neue',Arial,sans-serif; font-size:30px; margin-bottom:32px;"">
                                                                    ORDER SUMMARY
                                                                </div>

                                                                {itemsHtmlBuilder}

                                                                <!-- Divider -->
                                                                <div style=""border-bottom:1px solid #dfdfdf; margin-bottom:32px;""></div>

                                                                <!-- Pricing Section (Vertical Layout) -->
                                                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                                    <!-- Subtotal -->
                                                                    <tr>
                                                                        <td align=""left"" valign=""middle"" style=""padding-bottom: 12px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:13px; font-weight:600; color:#979da6; text-transform:uppercase;"">Subtotal</div>
                                                                        </td>
                                                                        <td align=""right"" valign=""middle"" style=""padding-bottom: 12px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; font-weight:600; color:#141414;"">{order.SubTotal:N0} VND</div>
                                                                        </td>
                                                                    </tr>
                                                                    
                                                                    <!-- Shipping -->
                                                                    <tr>
                                                                        <td align=""left"" valign=""middle"" style=""padding-bottom: 12px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:13px; font-weight:600; color:#979da6; text-transform:uppercase;"">Shipping</div>
                                                                        </td>
                                                                        <td align=""right"" valign=""middle"" style=""padding-bottom: 12px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; font-weight:600; color:#141414;"">{shippingFeeVal:N0} VND</div>
                                                                        </td>
                                                                    </tr>
                                                                    
                                                                    <!-- Discount -->
                                                                    <tr>
                                                                        <td align=""left"" valign=""middle"" style=""padding-bottom: 16px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:13px; font-weight:600; color:#979da6; text-transform:uppercase;"">Discount</div>
                                                                        </td>
                                                                        <td align=""right"" valign=""middle"" style=""padding-bottom: 16px;"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; font-weight:600; color:#d14f2d;"">{discountText}</div>
                                                                        </td>
                                                                    </tr>
                                                                    
                                                                    <!-- Divider -->
                                                                    <tr>
                                                                        <td colspan=""2"" style=""border-top:1px dashed #dfdfdf; padding-top:16px;""></td>
                                                                    </tr>

                                                                    <!-- Total -->
                                                                    <tr>
                                                                        <td align=""left"" valign=""middle"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:14px; font-weight:600; color:#141414; text-transform:uppercase;"">Total</div>
                                                                        </td>
                                                                        <td align=""right"" valign=""middle"">
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:22px; font-weight:600; color:#141414;"">{order.TotalAmount:N0} VND</div>
                                                                        </td>
                                                                    </tr>
                                                                </table>
                                                                
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- COMPONENT: Customer Details (Shipping & Payment) -->
                            <table class=""pc-component"" style=""width:600px; max-width:600px"" width=""600"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                <tr>
                                    <td width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                        <table width=""100%"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                            <tr>
                                                <td valign=""top"" style=""padding:12px; background-color:#f6f6f6"" bgcolor=""#f6f6f6"">
                                                    <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                        <tr>
                                                            <td align=""left"" valign=""top"" style=""padding:32px; background-color:#fff; border-radius:16px;"">
                                                                
                                                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                                    <tr>
                                                                        <!-- Shipping Address -->
                                                                        <td width=""50%"" align=""left"" valign=""top"" style=""padding-right:15px;"">
                                                                            <div style=""font-family:'Bebas Neue',Arial,sans-serif; font-size:26px; color:#141414; margin-bottom:12px; letter-spacing: 0.5px;"">
                                                                                SHIPPING ADDRESS
                                                                            </div>
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:15px; line-height:160%; color:#455a64;"">
                                                                                <strong style=""color:#141414;"">{System.Net.WebUtility.HtmlEncode(order.ShippingName)}</strong><br>
                                                                                {System.Net.WebUtility.HtmlEncode(order.ShippingPhone)}<br>
                                                                                {System.Net.WebUtility.HtmlEncode(order.ShippingAddress)}<br>
                                                                                {System.Net.WebUtility.HtmlEncode(order.ShippingWardName)}, {System.Net.WebUtility.HtmlEncode(order.ShippingDistrictName)}<br>
                                                                                {System.Net.WebUtility.HtmlEncode(order.ShippingProvinceName)}
                                                                            </div>
                                                                        </td>
                                                                        
                                                                        <!-- Payment Details -->
                                                                        <td width=""50%"" align=""left"" valign=""top"" style=""padding-left:15px;"">
                                                                            <div style=""font-family:'Bebas Neue',Arial,sans-serif; font-size:26px; color:#141414; margin-bottom:12px; letter-spacing: 0.5px;"">
                                                                                PAYMENT DETAILS
                                                                            </div>
                                                                            <div style=""font-family:'Onest',Arial,sans-serif; font-size:15px; line-height:160%; color:#455a64;"">
                                                                                <strong style=""color:#141414;"">{paymentMethodDetail}</strong><br>
                                                                                Method: {paymentMethodDetail}<br>
                                                                                Status: <span style=""display:inline-block; background-color:{paymentStatusBg}; color:{paymentStatusColor}; padding:2px 10px; border-radius:4px; font-size:13px; font-weight:600; margin-top:6px;"">{paymentStatusText}</span>
                                                                            </div>
                                                                        </td>
                                                                    </tr>
                                                                </table>

                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- COMPONENT: Support -->
                            <table class=""pc-component"" style=""width:600px; max-width:600px"" width=""600"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                <tr>
                                    <td width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                        <table width=""100%"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                            <tr>
                                                <td valign=""top"" style=""padding:12px; background-color:#f6f6f6"" bgcolor=""#f6f6f6"">
                                                    <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                        <tr>
                                                            <td align=""left"" valign=""top"" style=""background-color:#ffe7d6; border-radius:16px;"">
                                                                <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                                    <tr>
                                                                        <td style=""padding:10px 32px 0 0"" align=""left"" valign=""middle"">
                                                                            <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" dir=""rtl"">
                                                                                <tr>
                                                                                    <td width=""50%"" align=""left"" valign=""middle"" dir=""ltr"" style=""padding-left:32px;"">
                                                                                        <div style=""font-family:'Bebas Neue',Arial,sans-serif; font-size:40px; color:#333; line-height:100%; margin-bottom:15px;"">
                                                                                            HAVE ANY QUESTIONS?
                                                                                        </div>
                                                                                        <div style=""font-family:'Onest',Arial,sans-serif; font-size:16px; line-height:140%; color:#455a64;"">
                                                                                            If you need any help or have any questions, feel free to contact us at <br/>
                                                                                            <span style=""color:#d14f2d; font-weight:600;"">toystoresupport@gmail.com</span>
                                                                                        </div>
                                                                                    </td>
                                                                                    <td width=""50%"" align=""left"" valign=""middle"" dir=""ltr"">
                                                                                        <img src=""https://cloudfilesdm.com/postcards/image-1737475166054-54716500.png"" width=""156"" style=""display:block; width:100%; height:auto; border:0;"" alt=""Support"" />
                                                                                    </td>
                                                                                </tr>
                                                                            </table>
                                                                        </td>
                                                                    </tr>
                                                                </table>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- COMPONENT: Footer -->
                            <table class=""pc-component"" style=""width:600px; max-width:600px"" width=""600"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                <tr>
                                    <td width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                        <table width=""100%"" align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                                            <tr>
                                                <td valign=""top"" style=""padding:48px; background-color:#f6f6f6"" bgcolor=""#f6f6f6"">
                                                    
                                                    <table width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                                                        <tr>
                                                            <td align=""center"" valign=""top"" style=""padding-bottom:32px;"">
                                                                <img src=""https://cloudfilesdm.com/postcards/image_2-dc6e0d12.png"" width=""200"" height=""133"" style=""display:block; border:0;"" alt=""Logo"" />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                    
                                                    <div style=""border-bottom:1px solid #e9e9e9; margin-bottom:32px;""></div>
                                                    
                                                    <div style=""font-family:'Onest',Arial,sans-serif; font-size:14px; line-height:20px; color:#141414; text-align:center;"">
                                                        Want to change which emails you receive from us? You can 
                                                        <a href=""#"" style=""color:#141414; font-weight:600; text-decoration:underline;"">update your preferences</a> or 
                                                        You can view our <a href=""#"" style=""color:#141414; font-weight:600; text-decoration:underline;"">privacy policy</a>.
                                                    </div>

                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

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
