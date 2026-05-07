using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class CustomerChild
{
    public int ChildId { get; set; }

    public int AccountId { get; set; }

    public string FullName { get; set; } = null!;

    public string? NickName { get; set; }

    public DateTime Dob { get; set; }

    public byte? SexId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Sex? Sex { get; set; }
}
