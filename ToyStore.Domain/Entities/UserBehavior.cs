using ToyStore.Domain.Enums;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Tracks user behavior for recommendation system.
/// </summary>
public class UserBehavior : Entity
{
    /// <summary>
    /// User who performed the action.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// User navigation property.
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// Product the action was performed on.
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Product navigation property.
    /// </summary>
    public virtual Product Product { get; set; } = null!;
    
    /// <summary>
    /// Type of behavior/action.
    /// </summary>
    public BehaviorType BehaviorType { get; set; }
    
    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// Additional data about the behavior (e.g., rating value, search query).
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Duration for view behaviors (in seconds).
    /// </summary>
    public int? DurationSeconds { get; set; }
    
    /// <summary>
    /// Session ID for grouping behaviors.
    /// </summary>
    public string? SessionId { get; set; }
    
    public UserBehavior()
    {
        OccurredAt = DateTime.UtcNow;
    }
}
