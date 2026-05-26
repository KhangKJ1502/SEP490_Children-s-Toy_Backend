using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ProductDetail
{
    public int ProductId { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? SexId { get; set; }

    public byte? OriginId { get; set; }

    public int WeightGram { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }

    public virtual Age? Age { get; set; }

    public virtual Material? Material { get; set; }

    public virtual Origin? Origin { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sex? Sex { get; set; }
}
