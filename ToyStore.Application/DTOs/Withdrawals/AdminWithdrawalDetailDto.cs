using System;
using System.Collections.Generic;

namespace ToyStore.Application.DTOs.Withdrawals;

/// <summary>
/// DTO chứa chi tiết đầy đủ yêu cầu rút tiền cho Admin xem chi tiết.
/// </summary>
public class AdminWithdrawalDetailDto
{
    public int WithdrawalId { get; set; }
    public int WalletId { get; set; }
    public int AccountId { get; set; }
    public int? WalletTransactionId { get; set; }
    public string ReferenceId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string ToBankBin { get; set; } = null!;
    public string ToBankName { get; set; } = null!;
    public string ToAccountNumber { get; set; } = null!;
    public string ToAccountName { get; set; } = null!;
    public string? PayosPayoutId { get; set; }
    public string? PayosTransactionId { get; set; }
    public string Status { get; set; } = null!;
    public string? FailReason { get; set; }
    public byte RetryCount { get; set; }
    public DateTime? ProcessingAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Thông tin chi tiết khách hàng
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? CustomerPhone { get; set; }

    // Lịch sử các bước xử lý
    public List<WithdrawalHistoryStepDto> StatusHistory { get; set; } = new();
}

/// <summary>
/// Chi tiết lịch sử thay đổi trạng thái rút tiền.
/// </summary>
public class WithdrawalHistoryStepDto
{
    public int HistoryId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
