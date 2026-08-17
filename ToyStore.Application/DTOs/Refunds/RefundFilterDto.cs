using System;
using ToyStore.Application.Common.Models;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) chứa các tham số lọc danh sách yêu cầu hoàn tiền dành cho Khách hàng (Customer).
/// </summary>
public class RefundFilterDto
{
    /// <summary>
    /// Trang hiện tại (bắt đầu từ 1, mặc định 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên một trang (mặc định 10).
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Lọc theo tên trạng thái hoàn tiền (ví dụ: "Pending", "Approved", "Returning", "Completed", "Cancelled",...).
    /// </summary>
    public string? RefundStatus { get; set; }

    /// <summary>
    /// Lọc theo mã ID đơn hàng.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Lọc các yêu cầu tạo từ ngày (FromDate).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Lọc các yêu cầu tạo đến ngày (ToDate).
    /// </summary>
    public DateTime? ToDate { get; set; }
}
