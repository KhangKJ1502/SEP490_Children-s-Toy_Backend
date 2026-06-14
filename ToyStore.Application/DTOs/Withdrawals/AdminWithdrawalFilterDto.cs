using System;

namespace ToyStore.Application.DTOs.Withdrawals;

/// <summary>
/// DTO chứa tham số lọc danh sách yêu cầu rút tiền cho admin.
/// </summary>
public class AdminWithdrawalFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
