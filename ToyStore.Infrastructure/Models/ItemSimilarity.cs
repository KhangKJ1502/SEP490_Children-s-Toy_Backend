using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ItemSimilarity
{
    public int SimilarityId { get; set; }

    public int SourceProductId { get; set; }

    public int SimilarProductId { get; set; }

    public decimal SimilarityScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product SimilarProduct { get; set; } = null!;

    public virtual Product SourceProduct { get; set; } = null!;
}
