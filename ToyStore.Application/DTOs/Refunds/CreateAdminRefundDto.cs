using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Refunds;

public class CreateAdminRefundDto
{
    public int OrderId { get; set; }
    public byte RefundReasonId { get; set; }
    public string? ReasonDetails { get; set; }
    public decimal? OverrideAmount { get; set; }
    public List<CreateRefundItemDto> Items { get; set; } = new List<CreateRefundItemDto>();
}
