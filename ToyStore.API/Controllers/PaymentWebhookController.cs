using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.API.Controllers;

/// <summary>
/// Nhận webhook từ SE_PAY.
/// KHÔNG cần JWT auth — verify bằng header X-SePay-Secret.
/// Luôn trả HTTP 200.
/// </summary>
[ApiController]
[Route("api/payment")]
[AllowAnonymous]
public class PaymentWebhookController : ControllerBase
{
    private readonly ISePayWebhookService _webhookService;
    private readonly SePayOptions _sePayOpts;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        ISePayWebhookService webhookService,
        IOptions<SePayOptions> sePayOpts,
        ILogger<PaymentWebhookController> logger)
    {
        _webhookService = webhookService;
        _sePayOpts = sePayOpts.Value;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/payment/webhook
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromBody] SePayWebhookPayload payload,
        CancellationToken ct)
    {
        // Verify API Key from Authorization header: "Apikey API_KEY_CUA_BAN"
        if (!string.IsNullOrWhiteSpace(_sePayOpts.ApiKey))
        {
            Request.Headers.TryGetValue("Authorization", out var authHeader);
            var authStr = authHeader.ToString();

            // Expected: "Apikey <YourApiKey>"
            bool isValid = !string.IsNullOrWhiteSpace(authStr) && 
                          authStr.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase) && 
                          authStr[7..].Trim() == _sePayOpts.ApiKey;

            if (!isValid)
            {
                var masked = MaskSecretTail(authStr);
                _logger.LogWarning("SE_PAY webhook: invalid API Key from {Ip}, header='{Header}'",
                    HttpContext.Connection.RemoteIpAddress, masked);
                return Ok(new { success = false, message = "Invalid API Key" });
            }
        }

        try
        {
            await _webhookService.HandleAsync(payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SE_PAY webhook unhandled exception — content='{Content}'", payload.Content);
        }

        // Luôn trả 200 để SE_PAY không retry
        return Ok(new { success = true });
    }

    private static string MaskSecretTail(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return "(missing)";
        }

        var tailLength = Math.Min(4, secret.Length);
        return new string('*', Math.Max(0, secret.Length - tailLength)) + secret[^tailLength..];
    }
}
