using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

/// <summary>
/// Webhooks API — sẽ implement khi feature Payment webhook được assign.
/// Dùng để nhận callback từ VNPay, MoMo, ZaloPay.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    /// <summary>
    /// [Placeholder] VNPay payment callback — chưa implement.
    /// </summary>
    [HttpPost("vnpay")]
    public IActionResult VnPay()
        => Ok(new { message = "VNPay webhook — coming soon" });

    /// <summary>
    /// [Placeholder] MoMo payment callback — chưa implement.
    /// </summary>
    [HttpPost("momo")]
    public IActionResult MoMo()
        => Ok(new { message = "MoMo webhook — coming soon" });
}
