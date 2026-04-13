using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

/// <summary>
/// Orders API — sẽ implement khi feature Orders được assign.
/// DB First: dùng ToyStore.Infrastructure.Models.Order + SEP490ToyStoreContext.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // TODO: Inject SEP490ToyStoreContext hoặc IOrderRepository khi implement
    // private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// [Placeholder] Lấy danh sách đơn hàng — chưa implement.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
        => Ok(new { message = "Orders endpoint — coming soon" });
}
