namespace ToyStore.Domain.Enums;

/// <summary>
/// Age ranges for toy recommendations.
/// </summary>
public enum AgeRange
{
    /// <summary>0-12 months</summary>
    Infant = 0,
    
    /// <summary>1-3 years</summary>
    Toddler = 1,
    
    /// <summary>3-5 years</summary>
    Preschool = 2,
    
    /// <summary>6-8 years</summary>
    EarlyChildhood = 3,
    
    /// <summary>9-12 years</summary>
    Kids = 4,
    
    /// <summary>13+ years</summary>
    Teens = 5
}
