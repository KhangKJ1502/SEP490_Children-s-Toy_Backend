using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendRegisterOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var subject = "Xác nhận đăng ký tài khoản - ToyStore";
        var htmlBody = BuildRegisterOtpEmailHtml(toEmail, otpCode);
        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var subject = "Đặt lại mật khẩu - ToyStore";
        var htmlBody = BuildForgotPasswordOtpEmailHtml(toEmail, otpCode);
        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }


    public async Task SendForgotWalletPinOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var subject = "ToyStore Wallet PIN Reset Verification Code";
        var htmlBody = BuildForgotWalletPinOtpEmailHtml(toEmail, otpCode);
        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:Host"]!;
        var port = int.Parse(_configuration["Email:Port"]!);
        var username = _configuration["Email:Username"]!;
        var password = _configuration["Email:Password"]!;
        var displayName = _configuration["Email:DisplayName"]!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(displayName, username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Email} with subject '{Subject}'.", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}.", toEmail);
            throw;
        }
    }

    private static string BuildRegisterOtpEmailHtml(string toEmail, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Xác nhận đăng ký - ToyStore</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#ff6a00 0%,#ff9500 100%);padding:40px 40px 36px;text-align:center;"">
              <div style=""display:inline-block;background:rgba(255,255,255,0.15);border-radius:50%;width:64px;height:64px;line-height:64px;margin-bottom:16px;font-size:32px;"">🧸</div>
              <h1 style=""margin:0;color:#ffffff;font-size:26px;font-weight:700;letter-spacing:-0.5px;"">ToyStore</h1>
              <p style=""margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;"">Đồ Chơi Trẻ Em Chất Lượng Cao</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:40px 40px 32px;"">
              <h2 style=""margin:0 0 12px;color:#1a1a2e;font-size:20px;font-weight:600;"">Xác nhận đăng ký tài khoản</h2>
              <p style=""margin:0 0 24px;color:#555b6e;font-size:15px;line-height:1.6;"">
                Cảm ơn bạn đã đăng ký tài khoản tại <strong>ToyStore</strong>. Để hoàn tất đăng ký, vui lòng nhập mã OTP bên dưới:
              </p>

              <!-- OTP Box -->
              <div style=""background:linear-gradient(135deg,#fff7f0 0%,#fff3e8 100%);border:2px solid #ff6a00;border-radius:12px;padding:28px;text-align:center;margin:24px 0;"">
                <p style=""margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:2px;font-weight:600;"">Mã xác nhận OTP</p>
                <div style=""font-size:42px;font-weight:800;letter-spacing:10px;color:#ff6a00;font-family:'Courier New',monospace;line-height:1.2;"">{otpCode}</div>
                <p style=""margin:12px 0 0;color:#888;font-size:12px;"">⏱ Mã có hiệu lực trong <strong>5 phút</strong></p>
              </div>

              <p style=""margin:0 0 16px;color:#555b6e;font-size:14px;line-height:1.6;"">
                Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này. Tài khoản sẽ không được tạo nếu mã OTP không được xác nhận.
              </p>

              <div style=""background:#f8f9fa;border-left:4px solid #ff6a00;border-radius:4px;padding:16px;margin-top:24px;"">
                <p style=""margin:0;color:#666;font-size:13px;"">
                  🔒 <strong>Lưu ý bảo mật:</strong> Không chia sẻ mã OTP này với bất kỳ ai, kể cả nhân viên ToyStore.
                </p>
              </div>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f8f9fa;padding:24px 40px;border-top:1px solid #e9ecef;"">
              <p style=""margin:0 0 8px;color:#aaa;font-size:12px;text-align:center;"">
                Email này được gửi đến <strong>{toEmail}</strong>
              </p>
              <p style=""margin:0;color:#aaa;font-size:12px;text-align:center;"">
                © 2024 ToyStore - Đồ Chơi Trẻ Em. Mọi quyền được bảo lưu.
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

    private static string BuildForgotPasswordOtpEmailHtml(string toEmail, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Đặt lại mật khẩu - ToyStore</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""580"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#ff6a00 0%,#ff9500 100%);padding:40px 40px 36px;text-align:center;"">
              <div style=""display:inline-block;background:rgba(255,255,255,0.15);border-radius:50%;width:64px;height:64px;line-height:64px;margin-bottom:16px;font-size:32px;"">🔐</div>
              <h1 style=""margin:0;color:#ffffff;font-size:26px;font-weight:700;letter-spacing:-0.5px;"">ToyStore</h1>
              <p style=""margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;"">Đồ Chơi Trẻ Em Chất Lượng Cao</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:40px 40px 32px;"">
              <h2 style=""margin:0 0 12px;color:#1a1a2e;font-size:20px;font-weight:600;"">Yêu cầu đặt lại mật khẩu</h2>
              <p style=""margin:0 0 24px;color:#555b6e;font-size:15px;line-height:1.6;"">
                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email này. Vui lòng sử dụng mã OTP bên dưới để tiếp tục:
              </p>

              <!-- OTP Box -->
              <div style=""background:linear-gradient(135deg,#fff7f0 0%,#fff3e8 100%);border:2px solid #ff6a00;border-radius:12px;padding:28px;text-align:center;margin:24px 0;"">
                <p style=""margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:2px;font-weight:600;"">Mã OTP đặt lại mật khẩu</p>
                <div style=""font-size:42px;font-weight:800;letter-spacing:10px;color:#ff6a00;font-family:'Courier New',monospace;line-height:1.2;"">{otpCode}</div>
                <p style=""margin:12px 0 0;color:#888;font-size:12px;"">⏱ Mã có hiệu lực trong <strong>5 phút</strong></p>
              </div>

              <p style=""margin:0 0 16px;color:#555b6e;font-size:14px;line-height:1.6;"">
                Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Mật khẩu của bạn sẽ không thay đổi.
              </p>

              <div style=""background:#fff3f3;border-left:4px solid #ff4444;border-radius:4px;padding:16px;margin-top:24px;"">
                <p style=""margin:0;color:#cc0000;font-size:13px;"">
                  ⚠️ <strong>Cảnh báo:</strong> Nếu bạn không thực hiện yêu cầu này, tài khoản của bạn có thể đang bị xâm phạm. Hãy liên hệ hỗ trợ ngay lập tức.
                </p>
              </div>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f8f9fa;padding:24px 40px;border-top:1px solid #e9ecef;"">
              <p style=""margin:0 0 8px;color:#aaa;font-size:12px;text-align:center;"">
                Email này được gửi đến <strong>{toEmail}</strong>
              </p>
              <p style=""margin:0;color:#aaa;font-size:12px;text-align:center;"">
                © 2024 ToyStore - Đồ Chơi Trẻ Em. Mọi quyền được bảo lưu.
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

    private static string BuildForgotWalletPinOtpEmailHtml(string toEmail, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Wallet PIN Reset Verification - ToyStore</title>
</head>
<body style=""margin:0;padding:0;background-color:#f7f7f7;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:24px 0;"">
    <tr>
      <td align=""center"">
        <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:12px;overflow:hidden;"">
          <tr>
            <td style=""background:#ff7a00;color:#ffffff;padding:24px;text-align:center;"">
              <h2 style=""margin:0;"">ToyStore Wallet Security</h2>
            </td>
          </tr>
          <tr>
            <td style=""padding:28px;color:#333333;"">
              <p style=""margin:0 0 12px;"">Hello,</p>
              <p style=""margin:0 0 20px;"">
                We received a request to reset the PIN for your ToyStore Wallet account. To protect your wallet, please verify this request by entering the one-time password (OTP) below:
              </p>
              <div style=""text-align:center;background:#fff4eb;border:2px solid #ff7a00;border-radius:10px;padding:16px;margin:16px 0;"">
                <div style=""font-size:34px;letter-spacing:8px;font-weight:700;color:#ff7a00;"">{otpCode}</div>
                <p style=""margin:8px 0 0;font-size:12px;color:#666;"">This OTP is valid for 10 minutes.</p>
              </div>
              <p style=""margin:0 0 12px;font-size:14px;color:#555;line-height:1.6;"">
                For your security, never share this OTP with anyone, including people claiming to be ToyStore staff. ToyStore will never ask for your OTP through phone calls, messages, or social media.
              </p>
              <p style=""margin:0 0 12px;font-size:14px;color:#555;line-height:1.6;"">
                If you did not request a PIN reset, please ignore this email. Your current wallet PIN will remain unchanged unless the correct OTP is entered.
              </p>
              <p style=""margin:0;font-size:13px;color:#666;"">
                Need help? Please contact ToyStore Support so we can assist you right away.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""background:#fafafa;padding:16px;text-align:center;color:#999;font-size:12px;"">
              Email sent to <strong>{toEmail}</strong>
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

