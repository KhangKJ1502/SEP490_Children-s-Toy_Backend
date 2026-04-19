using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class OrderNote
{
    public int NoteId { get; set; }

    public int OrderId { get; set; }

    public int StaffId { get; set; }

    public string Note { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Account Staff { get; set; } = null!;
}
