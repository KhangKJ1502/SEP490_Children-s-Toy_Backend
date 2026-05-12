using System;
using ToyStore.Application.Common.Models;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? RefundStatus { get; set; }
    public int? OrderId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
