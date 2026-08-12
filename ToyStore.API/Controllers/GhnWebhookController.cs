using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
/// Supports both application/json and multipart/form-data binary image stream uploads.
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
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
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

        GhnWebhookPayload payload = new();
        IFormFile? binaryFile = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            payload.OrderCode = form["OrderCode"].FirstOrDefault() ?? form["order_code"].FirstOrDefault();
            payload.ClientOrderCode = form["ClientOrderCode"].FirstOrDefault() ?? form["client_order_code"].FirstOrDefault();
            payload.Status = form["Status"].FirstOrDefault() ?? form["status"].FirstOrDefault();
            payload.Type = form["Type"].FirstOrDefault() ?? form["type"].FirstOrDefault();
            payload.ReasonCode = form["ReasonCode"].FirstOrDefault() ?? form["reason_code"].FirstOrDefault();
            payload.Reason = form["Reason"].FirstOrDefault() ?? form["reason"].FirstOrDefault();
            payload.Image = form["image"].FirstOrDefault() ?? form["image_pod"].FirstOrDefault() ?? form["pod"].FirstOrDefault();

            if (long.TryParse(form["CODAmount"].FirstOrDefault() ?? form["cod_amount"].FirstOrDefault(), out var cod))
                payload.CODAmount = cod;
            if (long.TryParse(form["TotalFee"].FirstOrDefault() ?? form["total_fee"].FirstOrDefault(), out var fee))
                payload.TotalFee = fee;
            if (int.TryParse(form["Weight"].FirstOrDefault() ?? form["weight"].FirstOrDefault(), out var weight))
                payload.Weight = weight;

            // Retrieve attached binary file stream from multipart/form-data
            binaryFile = form.Files.GetFile("file") 
                      ?? form.Files.GetFile("image") 
                      ?? form.Files.GetFile("image_pod") 
                      ?? form.Files.GetFile("pod") 
                      ?? form.Files.GetFile("file_pod")
                      ?? form.Files.FirstOrDefault();
        }
        else
        {
            using var reader = new StreamReader(Request.Body);
            var bodyText = await reader.ReadToEndAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                payload = JsonSerializer.Deserialize<GhnWebhookPayload>(bodyText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new GhnWebhookPayload();
            }
        }

        var orderCode = payload.OrderCode ?? payload.ClientOrderCode;
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            _logger.LogWarning("GHN Webhook: Empty OrderCode and ClientOrderCode in request.");
            return Ok();
        }

        try
        {
            // 2. Invoke processing logic with optional binary stream
            if (binaryFile != null && binaryFile.Length > 0)
            {
                await using var stream = binaryFile.OpenReadStream();
                await _webhookService.ProcessAsync(payload, stream, binaryFile.FileName, cancellationToken);
            }
            else
            {
                await _webhookService.ProcessAsync(payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during GHN Webhook processing for OrderCode={OrderCode}", orderCode);

            // 3. Log business error back to the transaction database
            try
            {
                var tx = await _context.ShippingProviderTransactions
                    .FirstOrDefaultAsync(t => t.ProviderOrderCode == orderCode && t.Provider == "GHN", cancellationToken);

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
                _logger.LogError(dbEx, "Failed to record LastErrorMessage for GHN Webhook error on OrderCode={OrderCode}", orderCode);
            }
        }

        // 4. Always return 200 OK
        return Ok();
    }
}
