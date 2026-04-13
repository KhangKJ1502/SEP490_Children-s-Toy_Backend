using FluentValidation.Results;

namespace ToyStore.Application.Common.Models;

/// <summary>
/// Extension methods for Result pattern.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts FluentValidation ValidationResult to Result.
    /// </summary>
    public static Result ToResult(this ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return Result.Success();

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Result.ValidationFailure(errors);
    }

    /// <summary>
    /// Converts FluentValidation ValidationResult to Result&lt;T&gt;.
    /// </summary>
    public static Result<T> ToResult<T>(this ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            throw new InvalidOperationException("Cannot convert valid result without data.");

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Result<T>.ValidationFailure(errors);
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
