using System;

namespace ToyStore.Application.DTOs.Withdrawals;

/// <summary>
/// DTO thể hiện dòng thông tin rút tiền trong danh sách hiển thị của Admin.
/// </summary>
public class AdminWithdrawalListDto
{
    public int WithdrawalId { get; set; }
    public string ReferenceId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string ToBankBin { get; set; } = null!;
    public string ToBankName { get; set; } = null!;
    public string ToAccountNumber { get; set; } = null!;
    public string ToAccountName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Thông tin khách hàng cơ bản
    public int AccountId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? CustomerPhone { get; set; }
}
