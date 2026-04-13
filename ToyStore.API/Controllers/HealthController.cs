using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

/// <summary>
/// Health check endpoint for Docker/Kubernetes.
/// </summary>
[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Simple health check.
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
    /// Ready check - verifies database connection.
    /// </summary>
    [HttpGet("/ready")]
    public async Task<IActionResult> Ready(
        [FromServices] Infrastructure.Data.ToyStoreDbContext dbContext)
    {
        try
        {
            // Check database connection
            await dbContext.Database.CanConnectAsync();
            
            return Ok(new 
            { 
                Status = "Ready",
                Database = "Connected",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new 
            { 
                Status = "NotReady",
                Database = "Disconnected",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
