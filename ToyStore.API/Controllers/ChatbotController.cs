using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

// TODO: Implement khi feature Chatbot được assign
[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    [HttpGet]
    public IActionResult Index() => Ok(new { message = "Chatbot — coming soon" });
}
