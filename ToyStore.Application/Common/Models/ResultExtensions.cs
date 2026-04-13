using ToyStore.Application.Validators;

namespace ToyStore.Application.Common.Models;

/// <summary>
/// Extension methods for Result pattern.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts ValidationResult to Result.
    /// </summary>
    public static Result ToResult(this ValidationResult validationResult)
    {
        return validationResult.IsValid 
            ? Result.Success() 
            : Result.ValidationFailure(validationResult.ToErrorDictionary());
    }
    
    /// <summary>
    /// Converts ValidationResult to Result<T>.
    /// </summary>
    public static Result<T> ToResult<T>(this ValidationResult validationResult)
    {
        return validationResult.IsValid 
            ? throw new InvalidOperationException("Cannot convert valid result without data.")
            : Result<T>.ValidationFailure(validationResult.ToErrorDictionary());
    }
    
    /// <summary>
    /// Maps the result value if successful.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.ErrorCode!, result.ErrorMessage!);
            
        return Result<TOut>.Success(mapper(result.Data!));
    }
    
    /// <summary>
    /// Executes action if result is successful.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Data!);
        return result;
    }
    
    /// <summary>
    /// Executes action if result is failure.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<string, string> action)
    {
        if (result.IsFailure)
            action(result.ErrorCode!, result.ErrorMessage!);
        return result;
    }
    
    /// <summary>
    /// Combines multiple results into one.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }
        return Result.Success();
    }
}
