using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Carts;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddItem(
        [FromBody] AddToCartDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _cartService.AddItemAsync(dto, cancellationToken);
        return ToApiResponse(result, "Add to cart successfully.");
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateQuantity(
        int id,
        [FromBody] UpdateCartItemQuantityDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _cartService.UpdateItemQuantityAsync(id, dto, cancellationToken);
        return ToApiResponse(result, "Cart quantity updated successfully.");
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveItem(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _cartService.RemoveItemAsync(id, cancellationToken);
        return ToApiResponse(result, "Cart item removed successfully.");
    }

    [HttpGet("my-cart")]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetMyCart(CancellationToken cancellationToken = default)
    {
        var result = await _cartService.GetMyCartAsync(cancellationToken);
        return ToApiResponse(result, "Cart loaded successfully.");
    }

    private ActionResult<ApiResponse<CartDto>> ToApiResponse(Result<CartDto> result, string successMessage)
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<CartDto>.Ok(result.Data!, successMessage));
        }

        var errors = BuildErrors(result);
        var message = result.ErrorMessage ?? "Operation failed.";
        var response = ApiResponse<CartDto>.Fail(message, errors);

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(response),
            "UNAUTHORIZED" => Unauthorized(response),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, response),
            "CONFLICT" => Conflict(response),
            _ => BadRequest(response)
        };
    }

    private static List<string> BuildErrors(Result<CartDto> result)
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
