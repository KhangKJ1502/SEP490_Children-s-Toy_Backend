namespace ToyStore.Application.DTOs.Withdrawals;

public class WithdrawalDto
{
    public int WithdrawalId { get; set; }
    public string ReferenceId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string ToBankBin { get; set; } = null!;
    public string ToBankName { get; set; } = null!;
    public string ToAccountNumber { get; set; } = null!;
    public string ToAccountName { get; set; } = null!;
    public string? PayosPayoutId { get; set; }
    public string Status { get; set; } = null!;
    public string? FailReason { get; set; }
    public DateTime? ProcessingAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
