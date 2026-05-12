using System;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundReasonDto
{
    public byte RefundReasonId { get; set; }
    public string Content { get; set; } = null!;
    public string? Description { get; set; }
}
