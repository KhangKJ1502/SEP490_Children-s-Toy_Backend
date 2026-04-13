using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public string? EmployeeCode { get; set; }

    public string AccountName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = null!;

    public string? Image { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public string? Provider { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<Banner> Banners { get; set; } = new List<Banner>();

    public virtual ICollection<BlogPost> BlogPostAccounts { get; set; } = new List<BlogPost>();

    public virtual ICollection<BlogPost> BlogPostApprovedByNavigations { get; set; } = new List<BlogPost>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Order> OrderAccounts { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderAssignedToStaffs { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCancelledByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<OrderRefund> OrderRefundApprovedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundCustomers { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundRequestedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();

    public virtual ICollection<ReviewProductReply> ReviewProductReplies { get; set; } = new List<ReviewProductReply>();

    public virtual ICollection<ReviewProduct> ReviewProducts { get; set; } = new List<ReviewProduct>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserBlockHistory> UserBlockHistoryAccounts { get; set; } = new List<UserBlockHistory>();

    public virtual ICollection<UserBlockHistory> UserBlockHistoryBlockedByNavigations { get; set; } = new List<UserBlockHistory>();

    public virtual ICollection<UserBlockHistory> UserBlockHistoryUnblockedByNavigations { get; set; } = new List<UserBlockHistory>();

    public virtual ICollection<VoucherUsageLog> VoucherUsageLogs { get; set; } = new List<VoucherUsageLog>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
