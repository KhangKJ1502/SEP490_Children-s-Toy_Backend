using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ProductDetail
{
    public int ProductId { get; set; }

    public string? Description { get; set; }

    public short? MaterialId { get; set; }

    public byte? AgeId { get; set; }

    public byte? SexId { get; set; }

    public short? OriginId { get; set; }

    public virtual Age? Age { get; set; }

    public virtual Material? Material { get; set; }

    public virtual Origin? Origin { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sex? Sex { get; set; }
}
