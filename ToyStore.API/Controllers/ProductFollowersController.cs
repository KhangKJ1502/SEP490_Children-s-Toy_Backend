using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/product-followers")]
[Authorize]
public class ProductFollowersController : ControllerBase
{
    private readonly IProductFollowerService _followerService;

    public ProductFollowersController(IProductFollowerService followerService)
    {
        _followerService = followerService;
    }

    /// <summary>
    /// Đăng ký theo dõi sản phẩm.
    /// </summary>
    [HttpPost("follow/{productId:int}")]
    public async Task<ActionResult<ApiResponse>> Follow(int productId, CancellationToken cancellationToken = default)
    {
        var result = await _followerService.FollowProductAsync(productId, cancellationToken);
        return result.IsSuccess 
            ? Ok(ApiResponse.Ok("Successfully followed the product.")) 
            : BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "Failed to follow the product."));
    }

    /// <summary>
    /// Hủy theo dõi sản phẩm.
    /// </summary>
    [HttpDelete("unfollow/{productId:int}")]
    public async Task<ActionResult<ApiResponse>> Unfollow(int productId, CancellationToken cancellationToken = default)
    {
        var result = await _followerService.UnfollowProductAsync(productId, cancellationToken);
        return result.IsSuccess 
            ? Ok(ApiResponse.Ok("Successfully unfollowed the product.")) 
            : BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "Failed to unfollow the product."));
    }

    /// <summary>
    /// Kiểm tra trạng thái theo dõi sản phẩm.
    /// </summary>
    [HttpGet("is-following/{productId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> IsFollowing(int productId, CancellationToken cancellationToken = default)
    {
        var result = await _followerService.IsFollowingAsync(productId, cancellationToken);
        return result.IsSuccess 
            ? Ok(ApiResponse<bool>.Ok(result.Data, "Checked follow status successfully.")) 
            : BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage ?? "Failed to check follow status."));
    }

    /// <summary>
    /// Lấy danh sách ID các sản phẩm đang theo dõi.
    /// </summary>
    [HttpGet("my-follows")]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetMyFollows(CancellationToken cancellationToken = default)
    {
        var result = await _followerService.GetFollowedProductIdsAsync(cancellationToken);
        return result.IsSuccess 
            ? Ok(ApiResponse<List<int>>.Ok(result.Data!, "Followed product IDs loaded successfully.")) 
            : BadRequest(ApiResponse<List<int>>.Fail(result.ErrorMessage ?? "Failed to load followed products."));
    }
}
