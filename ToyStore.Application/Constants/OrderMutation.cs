namespace ToyStore.Application.Constants;

/// <summary>
/// Admin order operations subject to assignment checks for Staff/Merchandise.
/// </summary>
public enum OrderMutation
{
    Confirm,
    Process,
    Ship,
    Cancel
}
