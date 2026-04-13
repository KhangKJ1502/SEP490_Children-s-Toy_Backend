using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

// TODO: Implement khi feature Recommendation được assign
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    [HttpGet]
    public IActionResult Index() => Ok(new { message = "Recommendations — coming soon" });
}
