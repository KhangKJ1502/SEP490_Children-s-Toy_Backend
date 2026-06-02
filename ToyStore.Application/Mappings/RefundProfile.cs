using AutoMapper;
using System.Linq;
using ToyStore.Application.DTOs.Refunds;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class RefundProfile : Profile
{
    public RefundProfile()
    {
        CreateMap<OrderRefund, RefundDto>()
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.Order.Status.StatusName))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Order.PaymentStatus))
            .ForMember(dest => dest.RefundReasonContent, opt => opt.MapFrom(src => src.RefundReason != null ? src.RefundReason.Content : null))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.AccountName))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.PhoneNumber))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src => src.RequestedByNavigation != null ? src.RequestedByNavigation.AccountName : null))
            .ForMember(dest => dest.RefundStatus, opt => opt.MapFrom(src => src.Status != null ? src.Status.StatusName : null))
            .ForMember(dest => dest.RefundSource, opt => opt.MapFrom(src => src.RefundSource ?? "Customer"))
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.RefundDetails))
            .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.RefundStatusHistories))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.RefundImages.Where(i => !i.IsDeleted).Select(i => i.ImageUrl).ToList()))
            .ForMember(dest => dest.ShippingOrderCode, opt => opt.MapFrom(src => src.ShippingOrderCode))
            .ForMember(dest => dest.ShippingHistory, opt => opt.MapFrom(src => MapRefundShippingHistory(src)));

        CreateMap<OrderRefund, RefundListDto>()
            .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order.OrderCode))
            .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.Order.Status.StatusName))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Order.PaymentStatus))
            .ForMember(dest => dest.RefundReasonContent, opt => opt.MapFrom(src => src.RefundReason != null ? src.RefundReason.Content : null))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.AccountName))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.PhoneNumber))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src => src.RequestedByNavigation != null ? src.RequestedByNavigation.AccountName : null))
            .ForMember(dest => dest.RefundStatus, opt => opt.MapFrom(src => src.Status != null ? src.Status.StatusName : null))
            .ForMember(dest => dest.RefundSource, opt => opt.MapFrom(src => src.RefundSource ?? "Customer"));

        CreateMap<OrderRefundReason, RefundReasonDto>();

        CreateMap<RefundDetail, RefundDetailDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
            .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => src.Product.ProductImage != null ? src.Product.ProductImage.ImageUrl : null));

        CreateMap<RefundStatusHistory, RefundStatusHistoryDto>()
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(dest => dest.ChangedByName, opt => opt.MapFrom(src => src.ChangedByNavigation != null ? src.ChangedByNavigation.AccountName : null));
    }

    private static System.Collections.Generic.List<ShippingStatusHistory> MapRefundShippingHistory(OrderRefund refund)
    {
        if (refund == null || string.IsNullOrWhiteSpace(refund.ShippingOrderCode) || refund.Order == null)
            return new System.Collections.Generic.List<ShippingStatusHistory>();

        var tx = refund.Order.ShippingProviderTransactions
            .FirstOrDefault(t => t.ProviderOrderCode == refund.ShippingOrderCode);

        return tx?.ShippingStatusHistories.OrderByDescending(h => h.ProcessedAt).ToList() 
            ?? new System.Collections.Generic.List<ShippingStatusHistory>();
    }
}
