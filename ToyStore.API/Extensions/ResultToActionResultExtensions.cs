using Microsoft.AspNetCore.Mvc;
using ToyStore.Domain.Entities;

namespace ToyStore.API.Extensions;

/// <summary>
/// Extension methods to convert Result to ActionResult.
/// </summary>
public static class ResultToActionResultExtensions
{
    /// <summary>
    /// Converts Result<T> to appropriate ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Data);
            
        return result.ErrorCode switch
        {
            "NOT_FOUND" => new NotFoundObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "VALIDATION_ERROR" => new BadRequestObjectResult(new ValidationErrorResponse(result.ErrorMessage!, result.ValidationErrors!)),
            "CONFLICT" => new ConflictObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "UNAUTHORIZED" or "INVALID_CREDENTIALS" => new UnauthorizedObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "FORBIDDEN" or "ACCOUNT_INACTIVE" => new ObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)) { StatusCode = 403 },
            "BUSINESS_RULE_VIOLATION" => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "OTP_EXPIRED" or "OTP_INVALID" => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            _ => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage!))
        };
    }
    
    /// <summary>
    /// Converts Result<T> to ActionResult with custom success status.
    /// </summary>
    public static ActionResult<T> ToCreatedResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess)
            return new CreatedResult(location, result.Data);
            
        return result.ToActionResult();
    }
    
    /// <summary>
    /// Converts Result to appropriate ActionResult.
    /// </summary>
    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();
            
        return result.ErrorCode switch
        {
            "NOT_FOUND" => new NotFoundObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "VALIDATION_ERROR" => new BadRequestObjectResult(new ValidationErrorResponse(result.ErrorMessage!, result.ValidationErrors!)),
            "CONFLICT" => new ConflictObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "UNAUTHORIZED" or "INVALID_CREDENTIALS" => new UnauthorizedObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "FORBIDDEN" or "ACCOUNT_INACTIVE" => new ObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)) { StatusCode = 403 },
            "BUSINESS_RULE_VIOLATION" => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            "OTP_EXPIRED" or "OTP_INVALID" => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode, result.ErrorMessage!)),
            _ => new BadRequestObjectResult(new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage!))
        };
    }
    
    /// <summary>
    /// Converts Result to NoContent result on success.
    /// </summary>
    public static ActionResult ToNoContentResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();
            
        return result.ToActionResult();
    }
}

/// <summary>
/// Standard error response.
/// </summary>
public record ErrorResponse(string Code, string Message);

/// <summary>
/// Validation error response with field errors.
/// </summary>
public record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors)
{
    public string Code => "VALIDATION_ERROR";
}
