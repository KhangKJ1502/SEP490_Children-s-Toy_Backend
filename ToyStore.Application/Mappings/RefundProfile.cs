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
            .ForMember(dest => dest.RefundSource, opt => opt.MapFrom(src => src.RefundSource ?? "Customer"));

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
    /// Thu thập và sắp xếp toàn bộ lịch sử hành trình giao nhận / hoàn trả vận chuyển từ các giao dịch GHN liên quan.
    /// </summary>
    private static System.Collections.Generic.List<ShippingStatusHistory> MapRefundShippingHistory(OrderRefund refund)
    {
        if (refund == null || refund.Order == null)
            return new System.Collections.Generic.List<ShippingStatusHistory>();

        var historyList = new System.Collections.Generic.List<ShippingStatusHistory>();

        // Lấy lịch sử theo mã vận đơn chuyển hàng ban đầu
        if (!string.IsNullOrWhiteSpace(refund.ShippingOrderCode))
        {
            var tx = refund.Order.ShippingProviderTransactions
                .FirstOrDefault(t => t.ProviderOrderCode == refund.ShippingOrderCode);
            if (tx != null)
            {
                historyList.AddRange(tx.ShippingStatusHistories);
            }
        }

        // Lấy lịch sử theo mã vận đơn trả hàng về kho
        if (!string.IsNullOrWhiteSpace(refund.ReturnShippingOrderCode))
        {
            var tx = refund.Order.ShippingProviderTransactions
                .FirstOrDefault(t => t.ProviderOrderCode == refund.ReturnShippingOrderCode);
            if (tx != null)
            {
                historyList.AddRange(tx.ShippingStatusHistories);
            }
        }

        return historyList.OrderByDescending(h => h.ProcessedAt).ThenByDescending(h => h.HistoryId).ToList();
    }
}
