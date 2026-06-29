using System;

namespace ToyStore.Application.DTOs.Refunds;

public class RefundReasonDto
{
    public byte RefundReasonId { get; set; }
    public string Content { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// Bên chịu trách nhiệm mặc định: "Store" hoặc "Customer".
    /// FE dùng để hiển thị warning phí ship hoàn trả khi khách chọn lý do.
    /// </summary>
    public string ResponsibleParty { get; set; } = "Store";
}
