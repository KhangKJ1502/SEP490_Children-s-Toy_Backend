using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Ward
{
    public string WardCode { get; set; } = null!;

    public int DistrictId { get; set; }

    public string WardName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual District District { get; set; } = null!;
}
