using Microsoft.AspNetCore.Mvc;

namespace ToyStore.API.Controllers;

/// <summary>
/// Products API — sẽ implement khi feature Products được assign.
/// DB First: dùng ToyStore.Infrastructure.Models.Product + SEP490ToyStoreContext.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // TODO: Inject SEP490ToyStoreContext hoặc IProductRepository khi implement
    // private readonly SEP490ToyStoreContext _context;

    /// <summary>
    /// [Placeholder] Lấy danh sách sản phẩm — chưa implement.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
        => Ok(new { message = "Products endpoint — coming soon" });
}
