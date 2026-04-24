using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class District
{
    public int DistrictId { get; set; }

    public int ProvinceId { get; set; }

    public string DistrictName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Province Province { get; set; } = null!;

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
