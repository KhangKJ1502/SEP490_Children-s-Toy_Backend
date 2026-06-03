using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Account
{
    public int AccountId { get; set; }

    public byte RoleId { get; set; }

    public byte? SexId { get; set; }

    public DateTime? Dob { get; set; }

    public string? EmployeeCode { get; set; }

    public string AccountName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public string? Provider { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<BlogPost> BlogPostAccounts { get; set; } = new List<BlogPost>();

    public virtual ICollection<BlogPost> BlogPostApprovedByNavigations { get; set; } = new List<BlogPost>();

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<Campaign> CampaignSubmittedByAccounts { get; set; } = new List<Campaign>();

    public virtual ICollection<Campaign> CampaignReviewedByAccounts { get; set; } = new List<Campaign>();

    public virtual ICollection<CampaignApprovalLog> CampaignApprovalLogs { get; set; } = new List<CampaignApprovalLog>();

    public virtual ICollection<CampaignScheduleLog> CampaignScheduleLogs { get; set; } = new List<CampaignScheduleLog>();

    public virtual ICollection<CampaignSchedule> CampaignSchedules { get; set; } = new List<CampaignSchedule>();

    public virtual ICollection<BlogPostReaction> BlogPostReactions { get; set; } = new List<BlogPostReaction>();

    public virtual ICollection<BlogCommentModerationLog> BlogCommentModerationLogs { get; set; } = new List<BlogCommentModerationLog>();

    public virtual BlogCommentViolationCount? BlogCommentViolationCountAccount { get; set; }

    public virtual ICollection<BlogCommentViolationCount> BlogCommentViolationCountUnbannedByNavigations { get; set; } = new List<BlogCommentViolationCount>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<DeliveryAction> DeliveryActions { get; set; } = new List<DeliveryAction>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Order> OrderAccounts { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderAssignedToStaffs { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCancelledByNavigations { get; set; } = new List<Order>();

    public virtual ICollection<OrderRefund> OrderRefundApprovedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundCustomers { get; set; } = new List<OrderRefund>();

    public virtual ICollection<OrderRefund> OrderRefundRequestedByNavigations { get; set; } = new List<OrderRefund>();

    public virtual ICollection<RefundStatusHistory> RefundStatusHistories { get; set; } = new List<RefundStatusHistory>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

    public virtual ICollection<ProductFollower> ProductFollowers { get; set; } = new List<ProductFollower>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<ReviewBlogReaction> ReviewBlogReactions { get; set; } = new List<ReviewBlogReaction>();

    public virtual ICollection<ReviewBlogReplyReaction> ReviewBlogReplyReactions { get; set; } = new List<ReviewBlogReplyReaction>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplyAccounts { get; set; } = new List<ReviewBlogReply>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplyHiddenByNavigations { get; set; } = new List<ReviewBlogReply>();

    public virtual ICollection<ReviewBlogReply> ReviewBlogReplyReplyToAccounts { get; set; } = new List<ReviewBlogReply>();

    public virtual ICollection<ReviewBlog> ReviewBlogs { get; set; } = new List<ReviewBlog>();

    public virtual ICollection<ReviewBlog> ReviewBlogHiddenByNavigations { get; set; } = new List<ReviewBlog>();

    public virtual ICollection<ReviewProductReaction> ReviewProductReactions { get; set; } = new List<ReviewProductReaction>();

    public virtual ICollection<ReviewProduct> ReviewProducts { get; set; } = new List<ReviewProduct>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<StaffReviewProductReply> StaffReviewProductReplies { get; set; } = new List<StaffReviewProductReply>();

    public virtual ICollection<UserProductScore> UserProductScores { get; set; } = new List<UserProductScore>();

    public virtual ICollection<VoucherUsageLog> VoucherUsageLogs { get; set; } = new List<VoucherUsageLog>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<Wallet> WalletUnbannedByNavigations { get; set; } = new List<Wallet>();

    public virtual ICollection<WalletPinAttempt> WalletPinAttempts { get; set; } = new List<WalletPinAttempt>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual Sex? Sex { get; set; }

    public virtual ICollection<CustomerChild> CustomerChildren { get; set; } = new List<CustomerChild>();

    public virtual UserPreference? UserPreference { get; set; }

    public virtual ICollection<ReviewModerationLog> ReviewModerationLogs { get; set; } = new List<ReviewModerationLog>();
}
