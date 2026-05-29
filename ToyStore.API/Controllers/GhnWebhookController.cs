using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Options;

namespace ToyStore.API.Controllers;

/// <summary>
/// Dedicated controller for handling GHN specific webhook callbacks.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/webhooks/ghn")]
public class GhnWebhookController : ControllerBase
{
    private readonly IGhnWebhookService _webhookService;
    private readonly WebhookOptions _webhookOptions;
    private readonly SEP490ToyStoreContext _context;
    private readonly ILogger<GhnWebhookController> _logger;

    public GhnWebhookController(
        IGhnWebhookService webhookService,
        IOptions<WebhookOptions> webhookOptions,
        SEP490ToyStoreContext context,
        ILogger<GhnWebhookController> logger)
    {
        _webhookService = webhookService;
        _webhookOptions = webhookOptions.Value;
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] GhnWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        // 1. Verify X-Webhook-Token Header
        if (!Request.Headers.TryGetValue("X-Webhook-Token", out var tokenHeader))
        {
            _logger.LogWarning("GHN Webhook: missing X-Webhook-Token header");
            return Ok(); // Always return 200 to prevent retries on unauthorized attempts
        }

        var receivedToken = tokenHeader.ToString();
        if (!_webhookOptions.Shipping.Tokens.TryGetValue("GHN", out var expectedToken)
            || !string.Equals(receivedToken, expectedToken, StringComparison.Ordinal))
        {
            _logger.LogWarning("GHN Webhook: invalid or mismatch X-Webhook-Token received");
            return Ok(); // Always return 200 to prevent retries
        }

        if (payload == null)
        {
            _logger.LogWarning("GHN Webhook: Null payload received");
            return Ok();
        }

        try
        {
            // 2. Invoke processing logic
            await _webhookService.ProcessAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during GHN Webhook processing for OrderCode={OrderCode}", payload.OrderCode);

            // 3. Log business error back to the transaction database
            try
            {
                var tx = await _context.ShippingProviderTransactions
                    .FirstOrDefaultAsync(t => t.ProviderOrderCode == payload.OrderCode && t.Provider == "GHN", cancellationToken);

                if (tx != null)
                {
                    tx.LastErrorMessage = ex.Message;
                    tx.UpdatedAt = DateTime.UtcNow;
                    _context.ShippingProviderTransactions.Update(tx);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to record LastErrorMessage for GHN Webhook error on OrderCode={OrderCode}", payload.OrderCode);
            }
        }

        // 4. Always return 200 OK
        return Ok();
    }
}
