namespace ToyStore.Application.DTOs.Refunds;

public class UpdateRefundStatusDto
{
    public string Status { get; set; } = null!;
    public string? RejectReason { get; set; }
}
