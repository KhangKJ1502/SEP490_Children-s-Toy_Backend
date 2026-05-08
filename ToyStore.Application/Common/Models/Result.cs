namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Use this pattern for service methods to avoid throwing exceptions for expected failures.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }
    
    protected Result(bool isSuccess, T? data, string? errorCode, string? errorMessage, Dictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }
    
    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, null, null, null);
    }
    
    /// <summary>
    /// Creates a failure result with error message.
    /// </summary>
    public static Result<T> Failure(string errorCode, string errorMessage)
    {
        return new Result<T>(false, default, errorCode, errorMessage, null);
    }
    
    /// <summary>
    /// Creates a failure result for validation errors.
    /// </summary>
    public static Result<T> ValidationFailure(Dictionary<string, string[]> errors)
    {
        return new Result<T>(false, default, "VALIDATION_ERROR", "One or more validation errors occurred.", errors);
    }
    
    /// <summary>
    /// Creates a failure result for not found errors.
    /// </summary>
    public static Result<T> NotFound(string resourceType, object? id = null)
    {
        var message = id != null 
            ? $"{resourceType} with ID '{id}' was not found."
            : $"{resourceType} was not found.";
        return new Result<T>(false, default, "NOT_FOUND", message, null);
    }
    
    /// <summary>
    /// Creates a failure result for conflict errors.
    /// </summary>
    public static Result<T> Conflict(string message)
    {
        return new Result<T>(false, default, "CONFLICT", message, null);
    }
    
    /// <summary>
    /// Creates a failure result for business rule violations.
    /// </summary>
    public static Result<T> BusinessError(string message)
    {
        return new Result<T>(false, default, "BUSINESS_RULE_VIOLATION", message, null);
    }
    
    /// <summary>
    /// Creates a failure result for unauthorized access.
    /// </summary>
    public static Result<T> Unauthorized(string message = "You are not authorized to perform this action.")
    {
        return new Result<T>(false, default, "UNAUTHORIZED", message, null);
    }

    /// <summary>
    /// Creates a failure result for unprocessable entity (invalid state transition, business rule).
    /// Maps to HTTP 422.
    /// </summary>
    public static Result<T> UnprocessableEntity(string message)
    {
        return new Result<T>(false, default, "UNPROCESSABLE_ENTITY", message, null);
    }

    /// <summary>
    /// Creates a failure result for upstream/provider failures.
    /// Maps to HTTP 502.
    /// </summary>
    public static Result<T> BadGateway(string message)
    {
        return new Result<T>(false, default, "BAD_GATEWAY", message, null);
    }
    
    /// <summary>
    /// Converts to non-generic Result.
    /// </summary>
    public Result ToResult()
    {
        return IsSuccess 
            ? Result.Success() 
            : Result.Failure(ErrorCode!, ErrorMessage!);
    }
}

/// <summary>
/// Represents the result of an operation without return value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }
    
    protected Result(bool isSuccess, string? errorCode, string? errorMessage, Dictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }
    
    public static Result Success()
    {
        return new Result(true, null, null, null);
    }
    
    public static Result Failure(string errorCode, string errorMessage)
    {
        return new Result(false, errorCode, errorMessage, null);
    }
    
    public static Result ValidationFailure(Dictionary<string, string[]> errors)
    {
        return new Result(false, "VALIDATION_ERROR", "One or more validation errors occurred.", errors);
    }
    
    public static Result NotFound(string resourceType, object? id = null)
    {
        var message = id != null 
            ? $"{resourceType} with ID '{id}' was not found."
            : $"{resourceType} was not found.";
        return new Result(false, "NOT_FOUND", message, null);
    }
    
    public static Result Conflict(string message)
    {
        return new Result(false, "CONFLICT", message, null);
    }
    
    public static Result BusinessError(string message)
    {
        return new Result(false, "BUSINESS_RULE_VIOLATION", message, null);
    }

    /// <summary>
    /// Creates a failure result for unprocessable entity (invalid state transition).
    /// Maps to HTTP 422.
    /// </summary>
    public static Result UnprocessableEntity(string message)
    {
        return new Result(false, "UNPROCESSABLE_ENTITY", message, null);
    }

    /// <summary>
    /// Creates a failure result for upstream/provider failures.
    /// Maps to HTTP 502.
    /// </summary>
    public static Result BadGateway(string message)
    {
        return new Result(false, "BAD_GATEWAY", message, null);
    }
}
