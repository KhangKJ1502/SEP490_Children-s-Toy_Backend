namespace ToyStore.Domain.Entities;

/// <summary>
/// Base entity with audit fields for tracking creation and modification.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Identifier of the user who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Date and time when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Identifier of the user who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Indicates whether the entity is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Date and time when the entity was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
}
