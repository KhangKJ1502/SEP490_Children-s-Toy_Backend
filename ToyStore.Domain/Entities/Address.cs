using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Address
{
    public int AddressId { get; set; }

    public int AccountId { get; set; }

    public string? RecipientName { get; set; }

    public string? PhoneNumber { get; set; }

    public string AddressLine { get; set; } = null!;

    public string? WardCode { get; set; }

    public int? DistrictId { get; set; }

    public int? ProvinceId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual District? District { get; set; }

    public virtual Province? Province { get; set; }

    public virtual Ward? WardCodeNavigation { get; set; }
}
