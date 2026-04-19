using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Widget
{
    public byte WidgetId { get; set; }

    public string WidgetCode { get; set; } = null!;

    public string WidgetName { get; set; } = null!;

    public string Algorithm { get; set; } = null!;

    public byte MaxItems { get; set; }

    public string? FallbackAlgo { get; set; }

    public bool IsActive { get; set; }

    public string? Config { get; set; }

    public DateTime CreatedAt { get; set; }
}
