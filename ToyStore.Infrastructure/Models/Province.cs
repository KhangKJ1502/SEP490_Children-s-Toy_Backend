using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Province
{
    public int ProvinceId { get; set; }

    public string ProvinceName { get; set; } = null!;

    public string? ProvinceCode { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<District> Districts { get; set; } = new List<District>();
}
