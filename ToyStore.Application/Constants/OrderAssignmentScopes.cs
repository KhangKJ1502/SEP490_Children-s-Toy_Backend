namespace ToyStore.Application.Constants;

/// <summary>
/// Query assignmentScope cho GET /api/admin/orders (Staff/Merchandise).
/// </summary>
public static class OrderAssignmentScopes
{
    public const string InProgress = "inProgress";
    public const string Completed = "completed";

    public static string Normalize(string? scope) =>
        string.Equals(scope, Completed, StringComparison.OrdinalIgnoreCase)
            ? Completed
            : InProgress;
}
