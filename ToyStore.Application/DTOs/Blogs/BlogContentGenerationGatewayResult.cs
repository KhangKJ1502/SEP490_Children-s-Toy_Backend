namespace ToyStore.Application.DTOs.Blogs;

public sealed class BlogContentGenerationGatewayResult
{
    public bool IsSuccess { get; init; }

    public int? StatusCode { get; init; }

    public string? ErrorBody { get; init; }

    public string? ErrorMessage { get; init; }

    public PythonBlogGenerateResponse? Data { get; init; }

    public static BlogContentGenerationGatewayResult Success(PythonBlogGenerateResponse data)
        => new()
        {
            IsSuccess = true,
            Data = data
        };

    public static BlogContentGenerationGatewayResult HttpError(int statusCode, string? errorBody)
        => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorBody = errorBody
        };

    public static BlogContentGenerationGatewayResult ServiceUnavailable(string errorMessage)
        => new()
        {
            IsSuccess = false,
            StatusCode = 502,
            ErrorMessage = errorMessage
        };
}
