using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("webhooks/payos-payout")]
public class PayosPayoutWebhookController : ControllerBase
{
    private readonly IPayOsPayoutWebhookService _webhookService;

    public PayosPayoutWebhookController(IPayOsPayoutWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken = default)
    {
        string rawPayload;
        using (var reader = new System.IO.StreamReader(Request.Body))
            rawPayload = await reader.ReadToEndAsync(cancellationToken);

        // Always return 200 — PayOS retries on non-200
        try
        {
            await _webhookService.HandleAsync(rawPayload, cancellationToken);
        }
        catch (Exception)
        {
            // Swallow — webhook processing errors are logged internally
        }

        return Ok(new { code = "00", desc = "success" });
    }
}
