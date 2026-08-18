using AutoMapper;
using System.Linq;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình ánh xạ AutoMapper giữa các thực thể Domain của nghiệp vụ Hoàn tiền (OrderRefund, RefundDetail, RefundStatusHistory, OrderRefundReason) và các DTOs tương ứng.
/// </summary>
public class RefundProfile : Profile
{
    /// <summary>
    /// Khởi tạo cấu hình ánh xạ AutoMapper cho toàn bộ phân hệ Refund.
    /// </summary>
    public RefundProfile()
    {
        // Ánh xạ OrderRefund -> RefundDto (Chi tiết đầy đủ)
        CreateMap<OrderRefund, RefundDto>()
            .ForMember(dest => dest.ReasonDetails, opt => opt.MapFrom(src => CleanReasonDetails(src.ReasonDetails)))
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.Order.Status.StatusName))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Order.PaymentStatus))
            .ForMember(dest => dest.RefundReasonContent, opt => opt.MapFrom(src => src.RefundReason != null ? src.RefundReason.Content : null))
            .ForMember(dest => dest.RefundReasonResponsibleParty, opt => opt.MapFrom(src => src.RefundReason != null ? src.RefundReason.ResponsibleParty : null))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.AccountName))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Order.ShippingPhone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerAddress, opt => opt.MapFrom(src => 
                src.Customer.Address != null 
                    ? (src.Customer.Address.AddressLine + 
                       (src.Customer.Address.WardCodeNavigation != null ? ", " + src.Customer.Address.WardCodeNavigation.WardName : "") + 
                       (src.Customer.Address.District != null ? ", " + src.Customer.Address.District.DistrictName : "") + 
                       (src.Customer.Address.Province != null ? ", " + src.Customer.Address.Province.ProvinceName : ""))
                    : (src.Order.ShippingAddress + ", " + src.Order.ShippingWardName + ", " + src.Order.ShippingDistrictName + ", " + src.Order.ShippingProvinceName)
            ))
            .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src => src.RequestedByNavigation != null ? src.RequestedByNavigation.AccountName : null))
            .ForMember(dest => dest.RefundStatus, opt => opt.MapFrom(src => src.Status != null ? src.Status.StatusName : null))
            .ForMember(dest => dest.RefundSource, opt => opt.MapFrom(src => src.RefundSource ?? "Customer"))
            .ForMember(dest => dest.RefundType, opt => opt.MapFrom(src => src.RefundType ?? "ReturnAndRefund"))
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.RefundDetails))
            .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.RefundStatusHistories))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.RefundImages.Where(i => !i.IsDeleted).Select(i => i.ImageUrl).ToList()))
            .ForMember(dest => dest.ShippingOrderCode, opt => opt.MapFrom(src => src.ShippingOrderCode))
            .ForMember(dest => dest.ShippingHistory, opt => opt.MapFrom(src => MapRefundShippingHistory(src)))
            .ForMember(dest => dest.VoucherDiscountAmount, opt => opt.MapFrom(src => src.Order != null ? src.Order.VoucherDiscountAmount : 0m))
            .ForMember(dest => dest.CustomerShippingPaid, opt => opt.MapFrom(src => src.CustomerShippingPaid))
            .ForMember(dest => dest.IncludeShippingInRefund, opt => opt.MapFrom(src => src.IncludeShippingInRefund));

        // Ánh xạ OrderRefund -> RefundListDto (Danh sách tóm tắt)
        CreateMap<OrderRefund, RefundListDto>()
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.Order.Status.StatusName))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Order.PaymentStatus))
            .ForMember(dest => dest.RefundReasonContent, opt => opt.MapFrom(src => src.RefundReason != null ? src.RefundReason.Content : null))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.AccountName))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Order.ShippingPhone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src => src.RequestedByNavigation != null ? src.RequestedByNavigation.AccountName : null))
            .ForMember(dest => dest.RefundStatus, opt => opt.MapFrom(src => src.Status != null ? src.Status.StatusName : null))
            .ForMember(dest => dest.RefundSource, opt => opt.MapFrom(src => src.RefundSource ?? "Customer"))
            .ForMember(dest => dest.RefundType, opt => opt.MapFrom(src => src.RefundType ?? "ReturnAndRefund"));

        // Ánh xạ OrderRefundReason -> RefundReasonDto
        CreateMap<OrderRefundReason, RefundReasonDto>();

        // Ánh xạ RefundDetail -> RefundDetailDto
        CreateMap<RefundDetail, RefundDetailDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => src.Product.ProductImage != null ? src.Product.ProductImage.ImageUrl : null))
            .ForMember(dest => dest.RestorableQuantity, opt => opt.MapFrom(src => src.RestorableQuantity));

        // Ánh xạ RefundStatusHistory -> RefundStatusHistoryDto
        CreateMap<RefundStatusHistory, RefundStatusHistoryDto>()
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(dest => dest.ChangedByName, opt => opt.MapFrom(src => src.ChangedByNavigation != null ? src.ChangedByNavigation.AccountName : null));
    }

    /// <summary>
    /// Xóa bỏ phần thông tin từ chối (Reject Reason) nếu bị nối vào ReasonDetails của khách hàng để hiển thị sạch đẹp.
    /// </summary>
    private static string? CleanReasonDetails(string? reasonDetails)
    {
        if (string.IsNullOrWhiteSpace(reasonDetails)) return null;

        if (reasonDetails.StartsWith("Reject Reason:", System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var index = reasonDetails.IndexOf(" | Reject Reason:", System.StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var cleaned = reasonDetails.Substring(0, index).Trim();
            return string.IsNullOrEmpty(cleaned) ? null : cleaned;
        }

        return reasonDetails;
    }

    /// <summary>
    /// Thu thập và sắp xếp toàn bộ lịch sử hành trình giao nhận / hoàn trả vận chuyển từ các giao dịch GHN liên quan ĐẾN REFUND NÀY.
    /// Loại trừ 100% giao dịch giao hàng gốc của đơn hàng chính (RefundId == null).
    /// </summary>
    private static System.Collections.Generic.List<ShippingStatusHistory> MapRefundShippingHistory(OrderRefund refund)
    {
        if (refund == null || refund.Order == null || refund.Order.ShippingProviderTransactions == null)
            return new System.Collections.Generic.List<ShippingStatusHistory>();

        var historyList = new System.Collections.Generic.List<ShippingStatusHistory>();

        var mainOrderShippingCode = refund.Order?.ShippingOrderCode;

        var refundTxns = refund.Order.ShippingProviderTransactions
            .Where(t =>
            {
                // Loại trừ 100% giao dịch giao hàng gốc của đơn hàng chính (kể cả dữ liệu cũ đã lỡ bị gán RefundId)
                bool isOriginalOrderTx = !string.IsNullOrWhiteSpace(mainOrderShippingCode) &&
                    string.Equals(t.ProviderOrderCode, mainOrderShippingCode, StringComparison.OrdinalIgnoreCase) &&
                    !t.ProviderOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                    !t.ProviderOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) &&
                    !t.ProviderOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase);

                if (isOriginalOrderTx) return false;

                // 1. Giao dịch được gán trực tiếp cho Refund này VÀ có tiền tố refund hoặc mã refund
                if (t.RefundId.HasValue && t.RefundId.Value == refund.RefundId) return true;

                // 2. Giao dịch thuộc luồng refund (có tiền tố R-, R2-, REF- hoặc khớp ReturnShippingOrderCode/RefundCode)
                if (!string.IsNullOrWhiteSpace(t.ProviderOrderCode))
                {
                    if (t.ProviderOrderCode.StartsWith("R-", StringComparison.OrdinalIgnoreCase) ||
                        t.ProviderOrderCode.StartsWith("R2-", StringComparison.OrdinalIgnoreCase) ||
                        t.ProviderOrderCode.StartsWith("REF-", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (!string.IsNullOrWhiteSpace(refund.ReturnShippingOrderCode) &&
                        string.Equals(t.ProviderOrderCode, refund.ReturnShippingOrderCode, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            })
            .ToList();

        foreach (var tx in refundTxns)
        {
            if (tx.ShippingStatusHistories != null)
            {
                historyList.AddRange(tx.ShippingStatusHistories);
            }
        }

        return historyList
            .OrderByDescending(h => h.ProcessedAt)
            .ThenByDescending(h => h.HistoryId)
            .DistinctBy(h => h.HistoryId)
            .ToList();
    }
}
