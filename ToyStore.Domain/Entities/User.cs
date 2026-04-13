namespace ToyStore.Domain.Entities;

/// <summary>
/// Represents a user/customer in the system.
/// </summary>
public class User : AuditableEntity
{
    /// <summary>
    /// User's email address (unique).
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// User's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Hashed password (managed by identity system).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// User's avatar/profile image URL.
    /// </summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// User's date of birth.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// Default shipping address.
    /// </summary>
    public string? ShippingAddress { get; set; }
    
    /// <summary>
    /// Default billing address.
    /// </summary>
    public string? BillingAddress { get; set; }
    
    /// <summary>
    /// Whether the user's email is verified.
    /// </summary>
    public bool IsEmailVerified { get; set; }
    
    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Last login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// User's preferred categories for recommendations (JSON array of ToyCategory).
    /// </summary>
    public string? PreferredCategories { get; set; }
    
    /// <summary>
    /// Children's ages for personalized recommendations (comma-separated).
    /// </summary>
    public string? ChildrenAges { get; set; }
    
    /// <summary>
    /// User's orders.
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    
    /// <summary>
    /// User's behavior history.
    /// </summary>
    public virtual ICollection<UserBehavior> Behaviors { get; set; } = new List<UserBehavior>();
    
    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
