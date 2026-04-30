namespace ToyStore.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendRegisterOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);

    Task SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
}
