using System;
using ToyStore.Application.Common.Models;

namespace ToyStore.Application.DTOs.Refunds;

/// <summary>
/// Data Transfer Object (DTO) chứa các tiêu chí lọc, tìm kiếm và phân trang danh sách yêu cầu hoàn tiền trong giao diện Quản trị viên/Nhân viên.
/// </summary>
public class AdminRefundFilterDto
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
    /// Lọc theo tên trạng thái hoàn tiền (ví dụ: "Pending", "Approved", "Rejected", "Returning", "Received", "Refunded",...).
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

    /// <summary>
    /// Lọc theo mã ID khách hàng gửi yêu cầu.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Lọc theo mã ID lý do hoàn tiền.
    /// </summary>
    public byte? RefundReasonId { get; set; }

    /// <summary>
    /// Cột sắp xếp (ví dụ: "createdAt", "approvedAmount").
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Hướng sắp xếp ("asc" tăng dần, "desc" giảm dần).
    /// </summary>
    public string? SortDir { get; set; }

    /// <summary>
    /// Cờ lọc chỉ lấy các yêu cầu hoàn tiền được phân công cho chính tài khoản đang đăng nhập (true: chỉ của tôi).
    /// </summary>
    public bool AssignedToMe { get; set; } = false;

    /// <summary>
    /// Mã ID tài khoản nhân viên được phân công.
    /// </summary>
    public int? AssignedAccountId { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm (mã đơn hàng, mã hoàn tiền, tên khách hàng,...).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Phạm vi phân công (ví dụ: "ALL", "MINE", "UNASSIGNED").
    /// </summary>
    public string? AssignmentScope { get; set; }
}

