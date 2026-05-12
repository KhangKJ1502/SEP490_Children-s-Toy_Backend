using System;
using ToyStore.Application.Common.Models;

namespace ToyStore.Application.DTOs.Refunds;

public class AdminRefundFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? RefundStatus { get; set; }
    public int? OrderId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
    public byte? RefundReasonId { get; set; }
    public string? SortBy { get; set; } // "createdAt" or "approvedAmount"
    public string? SortDir { get; set; } // "asc" or "desc"
}
