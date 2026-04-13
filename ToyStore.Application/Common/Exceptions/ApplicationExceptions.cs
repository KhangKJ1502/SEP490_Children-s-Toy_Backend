namespace ToyStore.Application.Common.Exceptions;

/// <summary>
/// Base exception for application-specific errors.
/// </summary>
public abstract class ApplicationException : Exception
{
    public string Code { get; }
    
    protected ApplicationException(string code, string message) 
        : base(message)
    {
        Code = code;
    }
    
    protected ApplicationException(string code, string message, Exception innerException) 
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : ApplicationException
{
    public string ResourceType { get; }
    public object? ResourceId { get; }
    
    public NotFoundException(string resourceType, object? resourceId = null)
        : base("NOT_FOUND", $"{resourceType} was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
    
    public static NotFoundException For<T>(object id) where T : class
    {
        return new NotFoundException(typeof(T).Name, id);
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : ApplicationException
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = errors;
    }
    
    public ValidationException(string field, string message)
        : base("VALIDATION_ERROR", message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : ApplicationException
{
    public BusinessRuleException(string message)
        : base("BUSINESS_RULE_VIOLATION", message)
    {
    }
    
    public BusinessRuleException(string code, string message)
        : base(code, message)
    {
    }
}

/// <summary>
/// Exception thrown when user is not authorized.
/// </summary>
public class UnauthorizedException : ApplicationException
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.")
        : base("UNAUTHORIZED", message)
    {
    }
}

/// <summary>
/// Exception thrown when access is forbidden.
/// </summary>
public class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message = "Access to this resource is forbidden.")
        : base("FORBIDDEN", message)
    {
    }
}

/// <summary>
/// Exception thrown when there's a conflict with existing data.
/// </summary>
public class ConflictException : ApplicationException
{
    public ConflictException(string message)
        : base("CONFLICT", message)
    {
    }
    
    public static ConflictException DuplicateEntry(string field, object value)
    {
        return new ConflictException($"A record with {field} '{value}' already exists.");
    }
}
