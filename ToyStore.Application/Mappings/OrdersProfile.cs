using AutoMapper;
using ToyStore.Application.DTOs.Orders;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class OrdersProfile : Profile
{
    public OrdersProfile()
    {
        // Danh sach don hang (admin)
        CreateMap<Order, AdminOrderListItemDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.ShippingName))
            .ForMember(d => d.CustomerPhone, opt => opt.MapFrom(s => s.ShippingPhone))
            .ForMember(d => d.AssignedToStaffName, opt => opt.MapFrom(s =>
                s.AssignedToStaff != null ? s.AssignedToStaff.AccountName : null));

        // Chi tiet don hang (admin)
        CreateMap<Order, AdminOrderDetailDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.AssignedToStaffName, opt => opt.MapFrom(s =>
                s.AssignedToStaff != null ? s.AssignedToStaff.AccountName : null))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderDetails))
            .ForMember(d => d.StatusHistory, opt => opt.MapFrom(s => s.OrderStatusHistories))
            .ForMember(d => d.Shipping, opt => opt.MapFrom(s =>
                s.ShippingProviderTransactions.FirstOrDefault()));

        // Danh sach don hang (customer)
        CreateMap<Order, CustomerOrderListItemDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.Item, opt => opt.MapFrom(s =>
                s.OrderDetails.OrderBy(d => d.OrderDetailId).FirstOrDefault()))
            .ForMember(d => d.TotalItems, opt => opt.MapFrom(s => s.OrderDetails.Count));

        // Chi tiet don hang (customer)
        CreateMap<Order, CustomerOrderDetailDto>()
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderDetails))
            .ForMember(d => d.StatusHistory, opt => opt.MapFrom(s => s.OrderStatusHistories))
            .ForMember(d => d.Shipping, opt => opt.MapFrom(s =>
                s.ShippingProviderTransactions.FirstOrDefault()));

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
            .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.StatusName))
            .ForMember(d => d.ChangedByName, opt => opt.MapFrom(s =>
                s.ChangedByNavigation != null ? s.ChangedByNavigation.AccountName : null));

        // Thong tin giao hang
        CreateMap<ShippingProviderTransaction, ShippingTransactionDto>();

        CreateMap<ShippingProviderTransaction, CustomerShippingTransactionDto>();
    }
}
