using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.API.Controllers;

/// <summary>
/// Nhan callback trang thai van chuyen tu GHN va cac shipper khac.
/// Public endpoint — xac thuc bang X-Webhook-Token trong header.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/webhooks/shipping")]
public class ShippingWebhooksController : ControllerBase
{
    private readonly IShippingWebhookService _webhookService;
    private readonly WebhookOptions _webhookOptions;
    private readonly ILogger<ShippingWebhooksController> _logger;

    public ShippingWebhooksController(
        IShippingWebhookService webhookService,
        IOptions<WebhookOptions> webhookOptions,
        ILogger<ShippingWebhooksController> logger)
    {
        _webhookService  = webhookService;
        _webhookOptions  = webhookOptions.Value;
        _logger          = logger;
    }

    /// <summary>
    /// Nhan webhook tu shipper (POST /api/webhooks/shipping/{provider}).
    /// Luon tra ve 200 OK de shipper khong retry bao.
    /// </summary>
    [HttpPost("{provider}")]
    public async Task<IActionResult> HandleShippingWebhook(
        [FromRoute] string provider,
        CancellationToken cancellationToken)
    {
        // Xac thuc token trong header
        if (!Request.Headers.TryGetValue("X-Webhook-Token", out var tokenHeader))
        {
            _logger.LogWarning("Shipping webhook from {Provider}: missing X-Webhook-Token header", provider);
            return Ok(); // Luon tra 200 de tranh retry
        }

        var receivedToken = tokenHeader.ToString();
        var providerKey   = provider.ToUpperInvariant();

        if (!_webhookOptions.Shipping.Tokens.TryGetValue(providerKey, out var expectedToken)
            || !string.Equals(receivedToken, expectedToken, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Shipping webhook from {Provider}: invalid or unknown token", provider);
            return Ok(); // Van tra 200 de tranh retry
        }

        // Doc raw body
        string rawPayload;
        try
        {
            using var reader = new StreamReader(Request.Body);
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping webhook from {Provider}: failed to read body", provider);
            return Ok();
        }

        // Xu ly bat dong bo — khong de loi lan ra HTTP response
        _ = Task.Run(async () =>
        {
            await _webhookService.HandleAsync(provider, rawPayload, CancellationToken.None);
        }, CancellationToken.None);

        return Ok();
    }
}
