using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Banner
{
    public int BannerId { get; set; }

    public string BannerName { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string? LinkUrl { get; set; }

    public string Position { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public byte DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account CreatedByNavigation { get; set; } = null!;
}
