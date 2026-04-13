using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;
    
    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }
    
    /// <summary>
    /// Handles payment webhook from VNPay.
    /// </summary>
    [HttpPost("vnpay")]
    public async Task<ActionResult<WebhookResultDto>> VNPayWebhook(
        [FromBody] PaymentWebhookDto webhook,
        CancellationToken cancellationToken)
    {
        webhook.Provider = "VNPay";
        return await ProcessWebhook(webhook, cancellationToken);
    }
    
    /// <summary>
    /// Handles payment webhook from Momo.
    /// </summary>
    [HttpPost("momo")]
    public async Task<ActionResult<WebhookResultDto>> MomoWebhook(
        [FromBody] PaymentWebhookDto webhook,
        CancellationToken cancellationToken)
    {
        webhook.Provider = "Momo";
        return await ProcessWebhook(webhook, cancellationToken);
    }
    
    /// <summary>
    /// Handles payment webhook from ZaloPay.
    /// </summary>
    [HttpPost("zalopay")]
    public async Task<ActionResult<WebhookResultDto>> ZaloPayWebhook(
        [FromBody] PaymentWebhookDto webhook,
        CancellationToken cancellationToken)
    {
        webhook.Provider = "ZaloPay";
        return await ProcessWebhook(webhook, cancellationToken);
    }
    
    /// <summary>
    /// Generic payment webhook handler.
    /// </summary>
    [HttpPost("payment")]
    public async Task<ActionResult<WebhookResultDto>> PaymentWebhook(
        [FromBody] PaymentWebhookDto webhook,
        CancellationToken cancellationToken)
    {
        return await ProcessWebhook(webhook, cancellationToken);
    }
    
    private async Task<ActionResult<WebhookResultDto>> ProcessWebhook(
        PaymentWebhookDto webhook,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received payment webhook from {Provider}: TransactionId={TransactionId}, Status={Status}",
            webhook.Provider, webhook.TransactionId, webhook.Status);
            
        try
        {
            // Get client IP for logging
            var sourceIP = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            var result = await _webhookService.ProcessPaymentWebhookAsync(
                webhook, sourceIP, cancellationToken);
                
            if (result.Success)
            {
                _logger.LogInformation(
                    "Payment webhook processed successfully for Order {OrderNumber}",
                    result.OrderNumber);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning(
                    "Payment webhook processing failed: {Message}",
                    result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook from {Provider}", webhook.Provider);
            return StatusCode(500, new WebhookResultDto
            {
                Success = false,
                Message = "Internal server error processing webhook"
            });
        }
    }
}
