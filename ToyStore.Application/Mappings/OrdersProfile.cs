using AutoMapper;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Application.Services;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Enums;

namespace ToyStore.Application.Mappings;

public class OrdersProfile : Profile
{
    public OrdersProfile()
    {
        // Danh sach don hang (admin)
        CreateMap<Order, AdminOrderListItemDto>()
            .ForMember(d => d.StatusId, opt => opt.MapFrom(s => s.StatusId))
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.FulfillmentLabel, opt => opt.MapFrom(s => AdminOrderFulfillmentMapper.GetFulfillmentLabel(s)))
            .ForMember(d => d.GhnShippingStatus, opt => opt.MapFrom(s => AdminOrderFulfillmentMapper.GetLatestGhnStatus(s)))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.ShippingName))
            .ForMember(d => d.CustomerPhone, opt => opt.MapFrom(s => s.ShippingPhone))
            .ForMember(d => d.AssignedToStaffName, opt => opt.MapFrom(s =>
                s.AssignedToStaff != null ? s.AssignedToStaff.AccountName : null))
            .ForMember(d => d.AssignedToMerchId, opt => opt.MapFrom(s => s.AssignedToMerchId))
            .ForMember(d => d.AssignedToMerchName, opt => opt.MapFrom(s =>
                s.AssignedToMerch != null ? s.AssignedToMerch.AccountName : null));

        // Chi tiet don hang (admin)
        CreateMap<Order, AdminOrderDetailDto>()
            .ForMember(d => d.StatusId, opt => opt.MapFrom(s => s.StatusId))
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.FulfillmentLabel, opt => opt.MapFrom(s => AdminOrderFulfillmentMapper.GetFulfillmentLabel(s)))
            .ForMember(d => d.GhnShippingStatus, opt => opt.MapFrom(s => AdminOrderFulfillmentMapper.GetLatestGhnStatus(s)))
            .ForMember(d => d.CancelledByName, opt => opt.MapFrom(s =>
                s.CancelledByNavigation != null ? s.CancelledByNavigation.AccountName : null))
            .ForMember(d => d.AssignedToStaffName, opt => opt.MapFrom(s =>
                s.AssignedToStaff != null ? s.AssignedToStaff.AccountName : null))
            .ForMember(d => d.AssignedToMerchId, opt => opt.MapFrom(s => s.AssignedToMerchId))
            .ForMember(d => d.AssignedToMerchName, opt => opt.MapFrom(s =>
                s.AssignedToMerch != null ? s.AssignedToMerch.AccountName : null))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderDetails))
            .ForMember(d => d.StatusHistory, opt => opt.MapFrom(s => s.OrderStatusHistories))
            .ForMember(d => d.Shipping, opt => opt.MapFrom(s =>
                s.ShippingProviderTransactions.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).FirstOrDefault()))
            .ForMember(d => d.ShippingHistory, opt => opt.MapFrom(s => MapAdminShippingHistory(s)));

        CreateMap<ShippingStatusHistory, AdminShippingStatusHistoryDto>();

        // Danh sach don hang (customer)
        CreateMap<Order, CustomerOrderListItemDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => CustomerOrderDisplayStatusMapper.MapOrderListStatus(s)))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderDetails))
            .ForMember(d => d.TotalItems, opt => opt.MapFrom(s => s.OrderDetails.Count))
            .ForMember(d => d.HasActiveRefund, opt => opt.MapFrom(s => CustomerOrderDisplayStatusMapper.HasActiveRefund(s)))
            .AfterMap((s, d) => CustomerOrderDisplayStatusMapper.ApplyCustomerOrderContract(s, d));

        // Chi tiet don hang (customer)
        CreateMap<Order, CustomerOrderDetailDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => CustomerOrderDisplayStatusMapper.MapOrderListStatus(s)))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderDetails))
            .ForMember(d => d.StatusHistory, opt => opt.MapFrom(s => s.OrderStatusHistories))
            .ForMember(d => d.Shipping, opt => opt.MapFrom(s =>
                s.ShippingProviderTransactions.FirstOrDefault()))
            .ForMember(d => d.HasActiveRefund, opt => opt.MapFrom(s => CustomerOrderDisplayStatusMapper.HasActiveRefund(s)))
            .AfterMap((s, d) => CustomerOrderDisplayStatusMapper.ApplyCustomerOrderContract(s, d));

        // Chi tiet san pham trong don
        CreateMap<OrderDetail, AdminOrderDetailItemDto>();

        CreateMap<OrderDetail, CustomerOrderListItemProductDto>()
            .ForMember(d => d.Variant, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s =>
                s.Product != null && s.Product.Category != null
                    ? s.Product.Category.CategoryName
                    : null));

        CreateMap<OrderDetail, CustomerOrderDetailItemDto>()
            .ForMember(d => d.Variant, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s =>
                s.Product != null && s.Product.Category != null
                    ? s.Product.Category.CategoryName
                    : null));

        // Lich su trang thai
        CreateMap<OrderStatusHistory, OrderStatusHistoryDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.ChangedByName, opt => opt.MapFrom(s =>
                s.ChangedByNavigation != null ? s.ChangedByNavigation.AccountName : null));

        CreateMap<OrderStatusHistory, CustomerOrderStatusHistoryDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s =>
                CustomerOrderDisplayStatusMapper.ToCustomerHistoryDisplayStatus(s.Status.StatusName, null)))
            .ForMember(d => d.ChangedByName, opt => opt.MapFrom(s =>
                s.ChangedByNavigation != null ? s.ChangedByNavigation.AccountName : null));

        // Thong tin giao hang
        CreateMap<ShippingProviderTransaction, ShippingTransactionDto>();

        CreateMap<ShippingProviderTransaction, CustomerShippingTransactionDto>();
    }

    private static List<ShippingStatusHistory> MapAdminShippingHistory(Order order)
    {
        var tx = order.ShippingProviderTransactions
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefault();
        return tx?.ShippingStatusHistories.OrderByDescending(h => h.ProcessedAt).ToList() ?? [];
    }
}
