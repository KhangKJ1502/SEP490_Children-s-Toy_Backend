using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Sex
{
    public byte SexId { get; set; }

    public string SexName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<CustomerChild> CustomerChildren { get; set; } = new List<CustomerChild>();
}
