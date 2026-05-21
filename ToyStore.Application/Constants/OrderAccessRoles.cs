namespace ToyStore.Application.Constants;

/// <summary>
/// Role IDs from JWT / Roles table and OrderAssignments.RoleID for Staff/Merch.
/// </summary>
public static class OrderAccessRoles
{
    public const byte Customer = 1;
    public const byte Admin = 2;
    public const byte Staff = 3;
    public const byte Merchandise = 4;

    /// <summary>OrderAssignments.RoleID for sales staff.</summary>
    public const byte AssignmentStaff = 3;

    /// <summary>OrderAssignments.RoleID for warehouse/merch.</summary>
    public const byte AssignmentMerchandise = 4;
}
