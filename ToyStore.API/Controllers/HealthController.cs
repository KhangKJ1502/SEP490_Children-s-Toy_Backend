using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

/// <summary>
/// Endpoint health check cho Docker/Kubernetes.
/// </summary>
[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Health check co ban.
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Ready check - kiem tra ket noi DB.
    /// </summary>
    [HttpGet("/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken = default)
    {
        var result = await _healthService.CheckDatabaseAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new
            {
                Status = "Ready",
                Database = "Connected",
                Timestamp = DateTime.UtcNow
            });
        }

        return StatusCode(503, new
        {
            Status = "NotReady",
            Database = "Disconnected",
            Error = result.ErrorMessage,
            Timestamp = DateTime.UtcNow
        });
    }
}
