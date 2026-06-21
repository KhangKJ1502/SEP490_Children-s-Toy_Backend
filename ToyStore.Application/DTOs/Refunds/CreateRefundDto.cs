using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

public class CreateRefundDto
{
    public int OrderId { get; set; }
    public byte RefundReasonId { get; set; }
    public string? ReasonDetails { get; set; }
    public string RefundType { get; set; } = "ReturnAndRefund";
    public List<string> Images { get; set; } = new List<string>();
    public List<CreateRefundItemDto> Items { get; set; } = new List<CreateRefundItemDto>();
}
