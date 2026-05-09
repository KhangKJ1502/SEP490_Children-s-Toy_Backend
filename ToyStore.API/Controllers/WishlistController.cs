using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wishlists;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse>> AddItem(
        [FromBody] AddToWishlistDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _wishlistService.AddItemAsync(dto, cancellationToken);
        return ToApiResponse(result, "Added to wishlist successfully.");
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<ApiResponse>> RemoveItem(
        [FromRoute] int productId,
        CancellationToken cancellationToken = default)
    {
        var result = await _wishlistService.RemoveItemAsync(productId, cancellationToken);
        return ToApiResponse(result, "Removed from wishlist successfully.");
    }

    [HttpGet("my-wishlist")]
    public async Task<ActionResult<ApiResponse<List<WishlistItemDto>>>> GetMyWishlist(
        CancellationToken cancellationToken = default)
    {
        var result = await _wishlistService.GetMyWishlistAsync(cancellationToken);
        return ToApiResponse(result, "Wishlist loaded successfully.");
    }

    private ActionResult<ApiResponse> ToApiResponse(Result result, string successMessage)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse.Ok(successMessage));
        }

        var errors = BuildErrors(result);
        var message = result.ErrorMessage ?? "Operation failed.";
        var response = ApiResponse.Fail(message, errors);

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(response),
            "UNAUTHORIZED" => Unauthorized(response),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, response),
            "CONFLICT" => Conflict(response),
            _ => BadRequest(response)
        };
    }

    private ActionResult<ApiResponse<List<WishlistItemDto>>> ToApiResponse(
        Result<List<WishlistItemDto>> result,
        string successMessage)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<List<WishlistItemDto>>.Ok(result.Data!, successMessage));
        }

        var errors = BuildErrors(result);
        var message = result.ErrorMessage ?? "Operation failed.";
        var response = ApiResponse<List<WishlistItemDto>>.Fail(message, errors);

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(response),
            "UNAUTHORIZED" => Unauthorized(response),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, response),
            "CONFLICT" => Conflict(response),
            _ => BadRequest(response)
        };
    }

    private static List<string> BuildErrors(Result result)
    {
        if (result.ValidationErrors is { Count: > 0 })
        {
            return result.ValidationErrors
                .SelectMany(x => x.Value.Select(v => $"{x.Key}: {v}"))
                .ToList();
        }

        return string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? []
            : [result.ErrorMessage];
    }

    private static List<string> BuildErrors(Result<List<WishlistItemDto>> result)
    {
        if (result.ValidationErrors is { Count: > 0 })
        {
            return result.ValidationErrors
                .SelectMany(x => x.Value.Select(v => $"{x.Key}: {v}"))
                .ToList();
        }

        return string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? []
            : [result.ErrorMessage];
    }
}
