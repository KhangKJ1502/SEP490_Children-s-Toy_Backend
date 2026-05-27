using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext : DbContext
{
    public SEP490ToyStoreContext(DbContextOptions<SEP490ToyStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<CustomerChild> CustomerChildren { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Age> Ages { get; set; }

    public virtual DbSet<BackgroundJob> BackgroundJobs { get; set; }

    public virtual DbSet<BlockReason> BlockReasons { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<BlogPostStat> BlogPostStats { get; set; }

    public virtual DbSet<BlogPostReaction> BlogPostReactions { get; set; }

    public virtual DbSet<BlogCommentBanReason> BlogCommentBanReasons { get; set; }

    public virtual DbSet<BlogCommentModerationLog> BlogCommentModerationLogs { get; set; }

    public virtual DbSet<BlogCommentViolationCount> BlogCommentViolationCounts { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CampaignStat> CampaignStats { get; set; }

    public virtual DbSet<CampaignTarget> CampaignTargets { get; set; }

    public virtual DbSet<CampaignApprovalLog> CampaignApprovalLogs { get; set; }

    public virtual DbSet<CampaignSchedule> CampaignSchedules { get; set; }

    public virtual DbSet<CampaignScheduleLog> CampaignScheduleLogs { get; set; }

    public virtual DbSet<CampaignReferenceSnapshot> CampaignReferenceSnapshots { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DeliveryAction> DeliveryActions { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<DomainEventOutbox> DomainEventOutboxes { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<ItemSimilarity> ItemSimilarities { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderRefund> OrderRefunds { get; set; }

    public virtual DbSet<RefundImage> RefundImages { get; set; }

    public virtual DbSet<OrderRefundReason> OrderRefundReasons { get; set; }

    public virtual DbSet<StatusRefund> StatusRefunds { get; set; }

    public virtual DbSet<RefundDetail> RefundDetails { get; set; }

    public virtual DbSet<RefundStatusHistory> RefundStatusHistories { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OrderVoucher> OrderVouchers { get; set; }

    public virtual DbSet<Origin> Origins { get; set; }

    public virtual DbSet<PaymentGatewayTransaction> PaymentGatewayTransactions { get; set; }

    public virtual DbSet<PaymentHistory> PaymentHistories { get; set; }

    public virtual DbSet<PriceRange> PriceRanges { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductDetail> ProductDetails { get; set; }

    public virtual DbSet<ProductFollower> ProductFollowers { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductPromotion> ProductPromotions { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionTimeSlot> PromotionTimeSlots { get; set; }

    public virtual DbSet<PromotionProductSlot> PromotionProductSlots { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<ReactionType> ReactionTypes { get; set; }

    public virtual DbSet<ReviewBlog> ReviewBlogs { get; set; }

    public virtual DbSet<ReviewBlogReaction> ReviewBlogReactions { get; set; }

    public virtual DbSet<ReviewBlogReply> ReviewBlogReplies { get; set; }

    public virtual DbSet<ReviewBlogReplyReaction> ReviewBlogReplyReactions { get; set; }

    public virtual DbSet<ReviewProduct> ReviewProducts { get; set; }

    public virtual DbSet<ReviewProductImage> ReviewProductImages { get; set; }

    public virtual DbSet<ReviewProductReaction> ReviewProductReactions { get; set; }

    public virtual DbSet<ReviewModerationLog> ReviewModerationLogs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sex> Sexes { get; set; }

    public virtual DbSet<ShippingProviderTransaction> ShippingProviderTransactions { get; set; }

    public virtual DbSet<ShippingStatusHistory> ShippingStatusHistories { get; set; }

    public virtual DbSet<StaffReviewProductReply> StaffReviewProductReplies { get; set; }

    public virtual DbSet<StatusOrder> StatusOrders { get; set; }

    public virtual DbSet<SuperCategory> SuperCategories { get; set; }

    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<TrendingProduct> TrendingProducts { get; set; }

    public virtual DbSet<UserBlockHistory> UserBlockHistories { get; set; }

    public virtual DbSet<UserProductScore> UserProductScores { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherUsageLog> VoucherUsageLogs { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletPin> WalletPins { get; set; }

    public virtual DbSet<WalletPinAttempt> WalletPinAttempts { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    public virtual DbSet<Widget> Widgets { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    // ── Luồng B — Shift Management & Auto Order Assignment ────────────────
    public virtual DbSet<ShiftTemplate> ShiftTemplates { get; set; }

    public virtual DbSet<WorkSchedule> WorkSchedules { get; set; }

    public virtual DbSet<StaffShiftCapacity> StaffShiftCapacities { get; set; }

    public virtual DbSet<OrderAssignment> OrderAssignments { get; set; }

    public virtual DbSet<OrderQueue> OrderQueues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts", tb => tb.HasTrigger("TR_Accounts_InitPreferences"));

            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA58684B564C1");

            entity.HasIndex(e => e.EmployeeCode, "IX_Accounts_EmployeeCode")
                .IsUnique()
                .HasFilter("([EmployeeCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.Email, e.IsActive, e.IsDeleted }, "IX_Accounts_Login");

            entity.HasIndex(e => e.Email, "UQ__Accounts__A9D105341DE5C280").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.SexId).HasColumnName("SexID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accounts_Roles");

            entity.HasOne(d => d.Sex).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.SexId)
                .HasConstraintName("FK_Accounts_Sexes");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__091C2A1B80DFE817");

            entity.HasIndex(e => e.AccountId, "IX_Addresses_OneDefaultPerUser")
                .IsUnique()
                .HasFilter("([IsDefault]=(1) AND [IsDeleted]=(0))");

            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AddressLine).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WardCode)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithOne(p => p.Address)
                .HasForeignKey<Address>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Addresses_Accounts");

            entity.HasOne(d => d.District).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.DistrictId)
                .HasConstraintName("FK_Addresses_Districts");

            entity.HasOne(d => d.Province).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.ProvinceId)
                .HasConstraintName("FK_Addresses_Provinces");

            entity.HasOne(d => d.WardCodeNavigation).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.WardCode)
                .HasConstraintName("FK_Addresses_Wards");
        });

        modelBuilder.Entity<Age>(entity =>
        {
            entity.HasKey(e => e.AgeId).HasName("PK__Ages__875454C22C2A4528");

            entity.HasIndex(e => e.AgeRange, "UQ__Ages__E0EBEE38F4878BA4").IsUnique();

            entity.Property(e => e.AgeId)
                .ValueGeneratedOnAdd()
                .HasColumnName("AgeID");
            entity.Property(e => e.AgeRange)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PK__Backgrou__056690E24B19863C");

            entity.ToTable("BackgroundJobs", "System");

            entity.HasIndex(e => e.JobName, "UQ__Backgrou__F1AC1A95DC882CD8").IsUnique();

            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.CronExpression)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.JobName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastRunStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LastRunTime).HasPrecision(0);
            entity.Property(e => e.NextRunTime).HasPrecision(0);
        });

        modelBuilder.Entity<BlockReason>(entity =>
        {
            entity.HasKey(e => e.BlockReasonId).HasName("PK__BlockRea__8F5DFA9632AB4BD3");

            entity.Property(e => e.BlockReasonId)
                .ValueGeneratedOnAdd()
                .HasColumnName("BlockReasonID");
            entity.Property(e => e.Content).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.BlogCategoryId).HasName("PK__BlogCate__6BD2DA61E9AA8F5E");

            entity.HasIndex(e => e.BlogCategoriesName, "UQ__BlogCate__CD921A00A50628DA").IsUnique();

            entity.Property(e => e.BlogCategoryId).HasColumnName("BlogCategoryID");
            entity.Property(e => e.BlogCategoriesName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.BlogPostId).HasName("PK__BlogPost__3217414947E21B0D");

            entity.ToTable("BlogPosts", tb =>
            {
                tb.HasTrigger("trg_BlogPost_InitStats");
                tb.HasTrigger("trg_BlogPosts_UpdateFeaturedByStatus");
            });

            entity.HasIndex(e => e.BlogCategoryId, "IX_BlogPosts_Category");

            entity.HasIndex(e => new { e.Status, e.IsDeleted, e.BlogAt }, "IX_BlogPosts_Status_Date").IsDescending(false, false, true);

            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlogAt).HasPrecision(0);
            entity.Property(e => e.BlogCategoryId).HasColumnName("BlogCategoryID");
            entity.Property(e => e.BlogContent).HasMaxLength(3000);
            entity.Property(e => e.BlogThumbnail)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.BlogTitle).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.BlogPostAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPosts_Accounts");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.BlogPostApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_BlogPosts_ApprovedBy");

            entity.HasOne(d => d.BlogCategory).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.BlogCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPosts_BlogCategories");
        });

        modelBuilder.Entity<BlogPostStat>(entity =>
        {
            entity.HasKey(e => e.BlogPostId).HasName("PK__BlogPost__32174149A2A95B6D");

            entity.ToTable("BlogPostStats", tb =>
            {
                tb.HasTrigger("trg_BlogPostStats_UpdateFeatured");
            });

            entity.HasIndex(e => new { e.LikeCount, e.CommentCount }, "IX_BlogPostStats_Score").IsDescending(true, true);

            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.CommentCount).HasDefaultValue(0);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.BlogPost).WithOne(p => p.BlogPostStat)
                .HasForeignKey<BlogPostStat>(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPostStats_BlogPosts");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Brands__DAD4F3BE26CDE783");

            entity.HasIndex(e => e.BrandName, "UQ__Brands__2206CE9B151E23F4").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId).HasName("PK_Campaigns");

            entity.ToTable("Campaigns", "Notification", t =>
            {
                t.HasCheckConstraint("CK_Campaigns_SourceType", "[SourceType] IN ('ADMIN', 'SYSTEM')");
                t.HasCheckConstraint("CK_Campaigns_TargetType", "[TargetType] IN ('ALL', 'INDIVIDUAL', 'ROLE')");
                t.HasCheckConstraint("CK_Campaigns_ReferenceType", "[ReferenceType] IN ('VOUCHER', 'PRODUCT', 'BLOG', 'SALE', 'OTHER')");
                t.HasCheckConstraint("CK_Campaigns_Status", "[Status] IN ('Draft', 'PendingApproval', 'Approved', 'Rejected', 'Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                t.HasCheckConstraint("CK_Campaigns_AdminRequiresCreator", "[SourceType] = 'SYSTEM' OR [CreatedByAccountID] IS NOT NULL");
                t.HasCheckConstraint("CK_Campaigns_MustHaveContent", "[TemplateCode] IS NOT NULL OR ([TitleOverride] IS NOT NULL AND [MessageOverride] IS NOT NULL)");
                t.HasCheckConstraint("CK_Campaigns_ReferenceConsistency", "([ReferenceType] IS NULL AND [ReferenceID] IS NULL) OR ([ReferenceType] IS NOT NULL AND [ReferenceID] IS NOT NULL)");
                t.HasCheckConstraint("CK_Campaigns_RejectedNeedsNote", "[Status] <> 'Rejected' OR [ReviewNote] IS NOT NULL");
                t.HasCheckConstraint("CK_Campaigns_ValidRange", "[ValidFrom] IS NULL OR [ValidTo] IS NULL OR [ValidFrom] < [ValidTo]");
                t.HasCheckConstraint("CK_Campaigns_ScheduledAtInRange", "[ScheduledAt] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] IS NULL OR ([ScheduledAt] >= [ValidFrom] AND [ScheduledAt] <= [ValidTo])");
                t.HasCheckConstraint("CK_Campaigns_ScheduledAtConsistency", "[ScheduledAt] IS NULL OR [Status] IN ('Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                t.HasCheckConstraint("CK_Campaigns_ApprovedExpireConsistency", "[ApprovedExpireAt] IS NULL OR [ReviewedAt] IS NULL OR [ApprovedExpireAt] > [ReviewedAt]");
                t.HasCheckConstraint("CK_Campaigns_ApprovedExpireOnlyAfterApprove", "[ApprovedExpireAt] IS NULL OR [Status] IN ('Approved', 'Scheduled', 'Sending', 'Sent', 'Cancelled', 'Failed')");
                t.HasCheckConstraint("CK_Campaigns_RescheduleNotExceedMax", "[RescheduleCount] <= [MaxRescheduleCount]");
            });

            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.ActionTarget).HasMaxLength(500);
            entity.Property(e => e.ActionType).HasMaxLength(20);
            entity.Property(e => e.CampaignName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.CreatedByAccountId).HasColumnName("CreatedByAccountID");
            entity.Property(e => e.EventKey)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.MessageOverride).HasMaxLength(500);
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.SubmittedByAccountId).HasColumnName("SubmittedByAccountID");
            entity.Property(e => e.SubmittedAt).HasPrecision(0);
            entity.Property(e => e.ReviewedByAccountId).HasColumnName("ReviewedByAccountID");
            entity.Property(e => e.ReviewedAt).HasPrecision(0);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.Property(e => e.SourceType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ADMIN");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.TargetType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ALL");
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TitleOverride).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.ValidFrom).HasPrecision(0);
            entity.Property(e => e.ValidTo).HasPrecision(0);
            entity.Property(e => e.ScheduledAt).HasPrecision(0);
            entity.Property(e => e.ApprovedExpireAt).HasPrecision(0);
            entity.Property(e => e.RescheduleCount).HasDefaultValue((byte)0);
            entity.Property(e => e.MaxRescheduleCount).HasDefaultValue((byte)3);

            entity.HasIndex(e => e.EventKey, "UQ_Campaigns_EventKey_Active")
                .IsUnique()
                .HasFilter("[EventKey] IS NOT NULL AND [IsDeleted] = 0");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Campaigns_Status")
                .IsDescending(false, true)
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(e => new { e.Status, e.SubmittedAt }, "IX_Campaigns_PendingApproval")
                .IncludeProperties(e => new { e.CampaignId, e.CampaignName, e.SubmittedByAccountId })
                .HasFilter("[Status] = 'PendingApproval' AND [IsDeleted] = 0");

            entity.HasIndex(e => new { e.Status, e.ApprovedExpireAt }, "IX_Campaigns_ApprovedExpired")
                .IncludeProperties(e => new { e.CampaignId, e.SubmittedByAccountId, e.CreatedByAccountId })
                .HasFilter("[Status] = 'Approved' AND [IsDeleted] = 0");

            entity.HasIndex(e => new { e.Status, e.ReferenceType, e.ScheduledAt }, "IX_Campaigns_ScheduledWithRef")
                .IncludeProperties(e => new { e.CampaignId, e.ReferenceId, e.ValidTo })
                .HasFilter("[Status] = 'Scheduled' AND [ReferenceType] IS NOT NULL AND [IsDeleted] = 0");

            entity.HasOne(d => d.CreatedByAccount).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.CreatedByAccountId)
                .HasConstraintName("FK_Campaigns_Accounts");

            entity.HasOne(d => d.SubmittedByAccount).WithMany(p => p.CampaignSubmittedByAccounts)
                .HasForeignKey(d => d.SubmittedByAccountId)
                .HasConstraintName("FK_Campaigns_SubmittedBy");

            entity.HasOne(d => d.ReviewedByAccount).WithMany(p => p.CampaignReviewedByAccounts)
                .HasForeignKey(d => d.ReviewedByAccountId)
                .HasConstraintName("FK_Campaigns_ReviewedBy");

            entity.HasOne(d => d.TemplateCodeNavigation).WithMany(p => p.Campaigns)
                .HasPrincipalKey(p => p.TemplateCode)
                .HasForeignKey(d => d.TemplateCode)
                .HasConstraintName("FK_Campaigns_Templates");
        });

        modelBuilder.Entity<CampaignApprovalLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("CampaignApprovalLogs", "Notification", t =>
            {
                t.HasCheckConstraint("CK_CAL_Action", "[Action] IN ('Submitted', 'Approved', 'Rejected', 'Recalled', 'Scheduled', 'Rescheduled', 'Cancelled', 'Overridden')");
                t.HasCheckConstraint("CK_CAL_RejectedNeedsNote", "[Action] <> 'Rejected' OR [Note] IS NOT NULL");
            });

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ActorId).HasColumnName("ActorID");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => new { e.CampaignId, e.CreatedAt }, "IX_CAL_Campaign")
                .IsDescending(false, true)
                .IncludeProperties(e => new { e.Action, e.ActorId });

            entity.HasIndex(e => new { e.Action, e.CreatedAt }, "IX_CAL_PendingSubmissions")
                .IncludeProperties(e => new { e.CampaignId, e.ActorId })
                .HasFilter("[Action] = 'Submitted'");

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignApprovalLogs)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CAL_Campaigns");

            entity.HasOne(d => d.Actor).WithMany(p => p.CampaignApprovalLogs)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CAL_Actor");
        });

        modelBuilder.Entity<CampaignSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.ToTable("CampaignSchedules", "Notification", t =>
            {
                t.HasCheckConstraint("CK_CS_ExecutionStatus", "[ExecutionStatus] IN ('Waiting', 'Dispatched', 'Done', 'Failed', 'Cancelled')");
                t.HasCheckConstraint("CK_CS_LockConsistency", "([LockedByJobID] IS NULL) = ([LockedAt] IS NULL)");
                t.HasCheckConstraint("CK_CS_ExecutedAtConsistency", "[ExecutionStatus] IN ('Done', 'Failed') OR [ExecutedAt] IS NULL");
                t.HasCheckConstraint("CK_CS_AttemptNotExceedMax", "[AttemptCount] <= [MaxAttemptCount]");
            });

            entity.HasIndex(e => e.CampaignId, "UQ_CampaignSchedules_CampaignID").IsUnique();

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.ScheduledBy).HasColumnName("ScheduledBy");
            entity.Property(e => e.ScheduledAt).HasPrecision(0);
            entity.Property(e => e.LockedByJobId).HasColumnName("LockedByJobID");
            entity.Property(e => e.LockedAt).HasPrecision(0);
            entity.Property(e => e.ExecutionStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Waiting");
            entity.Property(e => e.AttemptCount).HasDefaultValue((byte)0);
            entity.Property(e => e.MaxAttemptCount).HasDefaultValue((byte)3);
            entity.Property(e => e.LastError).HasMaxLength(500);
            entity.Property(e => e.ExecutedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasIndex(e => new { e.ExecutionStatus, e.ScheduledAt }, "IX_CS_Waiting")
                .IncludeProperties(e => new { e.CampaignId, e.AttemptCount, e.MaxAttemptCount })
                .HasFilter("[ExecutionStatus] = 'Waiting'");

            entity.HasIndex(e => new { e.ExecutionStatus, e.LockedAt }, "IX_CS_StaleLock")
                .IncludeProperties(e => new { e.CampaignId, e.LockedByJobId })
                .HasFilter("[ExecutionStatus] = 'Dispatched'");

            entity.HasOne(d => d.Campaign).WithOne(p => p.CampaignSchedule)
                .HasForeignKey<CampaignSchedule>(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CS_Campaigns");

            entity.HasOne(d => d.ScheduledByNavigation).WithMany(p => p.CampaignSchedules)
                .HasForeignKey(d => d.ScheduledBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CS_ScheduledBy");

            entity.HasOne(d => d.LockedByJob).WithMany(p => p.CampaignSchedules)
                .HasForeignKey(d => d.LockedByJobId)
                .HasConstraintName("FK_CS_Jobs");
        });

        modelBuilder.Entity<CampaignScheduleLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("CampaignScheduleLogs", "Notification", t =>
            {
                t.HasCheckConstraint("CK_CSL_Action", "[Action] IN ('Scheduled', 'Rescheduled')");
                t.HasCheckConstraint("CK_CSL_ActionConsistency", "([Action] = 'Scheduled' AND [PreviousScheduledAt] IS NULL) OR ([Action] = 'Rescheduled' AND [PreviousScheduledAt] IS NOT NULL)");
            });

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.ActorId).HasColumnName("ActorID");
            entity.Property(e => e.Action).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.PreviousScheduledAt).HasPrecision(0);
            entity.Property(e => e.NewScheduledAt).HasPrecision(0);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => new { e.CampaignId, e.CreatedAt }, "IX_CSL_Campaign")
                .IsDescending(false, true)
                .IncludeProperties(e => new { e.ActorId, e.Action, e.NewScheduledAt, e.PreviousScheduledAt });

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignScheduleLogs)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CSL_Campaigns");

            entity.HasOne(d => d.Actor).WithMany(p => p.CampaignScheduleLogs)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CSL_Actor");
        });

        modelBuilder.Entity<CampaignReferenceSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId);

            entity.ToTable("CampaignReferenceSnapshots", "Notification", t =>
            {
                t.HasCheckConstraint("CK_CRS_ReferenceType", "[ReferenceType] IN ('VOUCHER', 'PRODUCT', 'BLOG', 'SALE', 'OTHER')");
                t.HasCheckConstraint("CK_CRS_DateRange", "[EntityStartDate] IS NULL OR [EntityEndDate] > [EntityStartDate]");
                t.HasCheckConstraint("CK_CRS_StaleConsistency", "[IsStale] = 0 OR ([StaleReason] IS NOT NULL AND [StaleDetectedAt] IS NOT NULL)");
            });

            entity.Property(e => e.SnapshotId).HasColumnName("SnapshotID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.ReferenceType).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.EntityStatus).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.EntityStartDate).HasPrecision(0);
            entity.Property(e => e.EntityEndDate).HasPrecision(0);
            entity.Property(e => e.IsStale).HasDefaultValue(false);
            entity.Property(e => e.StaleReason).HasMaxLength(200);
            entity.Property(e => e.StaleDetectedAt).HasPrecision(0);
            entity.Property(e => e.SnapshotAt).HasPrecision(0).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.CampaignId, "UQ_CRS_OneLiveSnapshotPerCampaign")
                .IsUnique()
                .HasFilter("[IsStale] = 0");

            entity.HasIndex(e => new { e.IsStale, e.EntityEndDate }, "IX_CRS_RevalidationJob")
                .IncludeProperties(e => new { e.CampaignId, e.ReferenceType, e.ReferenceId, e.EntityStatus })
                .HasFilter("[IsStale] = 0");

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignReferenceSnapshots)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CRS_Campaigns");
        });


        modelBuilder.Entity<CampaignStat>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__Campaign__3A162D1EA29FD2D5");

            entity.ToTable("CampaignStats", "Notification");

            entity.HasIndex(e => e.CampaignId, "UQ_CampaignStats_CampaignID").IsUnique();

            entity.Property(e => e.StatId).HasColumnName("StatID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Campaign).WithOne(p => p.CampaignStat)
                .HasForeignKey<CampaignStat>(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CampaignStats_Campaigns");
        });

        modelBuilder.Entity<CampaignTarget>(entity =>
        {
            entity.HasKey(e => e.CampaignTargetId).HasName("PK__Campaign__C1C43FA75E1078F4");

            entity.ToTable("CampaignTargets", "Notification");

            entity.Property(e => e.CampaignTargetId).HasColumnName("CampaignTargetID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACCOUNT_ID");
            entity.Property(e => e.TargetValue)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignTargets)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CampaignTargets_Campaigns");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD797AD6E6049");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.AccountId, "UQ__Cart__349DA58776E0F0FB").IsUnique();

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cart_Accounts");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B2A728313E0");

            entity.HasIndex(e => new { e.CartId, e.RemovedAt }, "IX_CartItems_ActiveCart").HasFilter("([RemovedAt] IS NULL)");

            entity.HasIndex(e => new { e.CartId, e.ProductId }, "UQ_CartItems_CartProduct").IsUnique();

            entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
            entity.Property(e => e.AddedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.IsSelected).HasDefaultValue(true);
            entity.Property(e => e.PriceAtThatTime).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.RemovedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Products");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B8978D605");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E01B631F3E").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(25);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SuperCategoryId).HasColumnName("SuperCategoryID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.SuperCategory).WithMany(p => p.Categories)
                .HasForeignKey(d => d.SuperCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categories_SuperCategories");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__626D8FEE3F04E48B");

            entity.ToTable("Deliveries", "Notification");

            entity.HasIndex(e => new { e.AccountId, e.NotificationType, e.Status }, "IX_Deliveries_NotificationType").HasFilter("([Status]<>'Deleted')");

            entity.HasIndex(e => new { e.RecipientType, e.Status, e.CreatedAt }, "IX_NotificationDeliveries_Admin")
                .IsDescending(false, false, true)
                .HasFilter("([RecipientType]<>'CUSTOMER')");

            entity.HasIndex(e => new { e.AccountId, e.RecipientType, e.Status }, "IX_NotificationDeliveries_User").HasFilter("([Status]='Unread' AND [RecipientType]='CUSTOMER')");

            entity.Property(e => e.DeliveryId).HasColumnName("DeliveryID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActionTarget).HasMaxLength(500);
            entity.Property(e => e.ActionType).HasMaxLength(20);
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedByJobId).HasColumnName("CreatedByJobID");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("SYSTEM");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("WEB_BELL");
            entity.Property(e => e.Payload)
                .HasMaxLength(2000)
                .HasDefaultValue("{}");
            entity.Property(e => e.ReadAt).HasPrecision(0);
            entity.Property(e => e.RecipientType)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("CUSTOMER");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Unread");
            entity.Property(e => e.EmailStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.PushStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.IdempotencyKey)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.HasIndex(e => e.IdempotencyKey, "UQ_Deliveries_IdempotencyKey")
                .IsUnique()
                .HasFilter("([IdempotencyKey] IS NOT NULL)");

            entity.HasOne(d => d.Account).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deliveries_Accounts");

            entity.HasOne(d => d.Campaign).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.CampaignId)
                .HasConstraintName("FK_Deliveries_Campaigns");

            entity.HasOne(d => d.CreatedByJob).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.CreatedByJobId)
                .HasConstraintName("FK_Deliveries_Jobs");

            entity.HasOne(d => d.TemplateCodeNavigation).WithMany(p => p.Deliveries)
                .HasPrincipalKey(p => p.TemplateCode)
                .HasForeignKey(d => d.TemplateCode)
                .HasConstraintName("FK_Deliveries_Templates");
        });

        modelBuilder.Entity<DeliveryAction>(entity =>
        {
            entity.HasKey(e => e.ActionId).HasName("PK__Delivery__FFE3F4B9CCE7F330");

            entity.ToTable("DeliveryActions", "Notification");

            entity.Property(e => e.ActionId).HasColumnName("ActionID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActionTarget).HasMaxLength(500);
            entity.Property(e => e.ActionType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryId).HasColumnName("DeliveryID");
            entity.Property(e => e.OccurredAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Account).WithMany(p => p.DeliveryActions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeliveryActions_Accounts");

            entity.HasOne(d => d.Delivery).WithMany(p => p.DeliveryActions)
                .HasForeignKey(d => d.DeliveryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeliveryActions_Deliveries");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.DistrictId).HasName("PK__District__85FDA4C63161ED48");

            entity.HasIndex(e => e.ProvinceId, "IX_Districts_ProvinceId");

            entity.Property(e => e.DistrictId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DistrictName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Province).WithMany(p => p.Districts)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Districts_Provinces");
        });

        modelBuilder.Entity<DomainEventOutbox>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__DomainEv__7944C87080D07E3D");

            entity.ToTable("DomainEventOutbox", "System");

            entity.HasIndex(e => e.OccurredOn, "IX_DomainEventOutbox_Pending").HasFilter("([ProcessedOn] IS NULL)");

            entity.Property(e => e.EventId)
                .ValueGeneratedNever()
                .HasColumnName("EventID");
            entity.Property(e => e.AggregateId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.AggregateType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OccurredOn).HasPrecision(0);
            entity.Property(e => e.Payload)
                .HasMaxLength(1000)
                .HasDefaultValue("{}");
            entity.Property(e => e.ProcessedOn).HasPrecision(0);
            entity.Property(e => e.ProcessingAt).HasPrecision(0);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C870E12C15C1");

            entity.ToTable("Events", "Interaction");

            entity.HasIndex(e => new { e.AccountId, e.EventType, e.CreatedAt }, "IX_InteractionEvents_UserBehavior").IsDescending(false, false, true);

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ClickPosition)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EntityID");
            entity.Property(e => e.EntityType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.EventType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Metadata).HasMaxLength(500);
            entity.Property(e => e.Referrer)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SessionID");
            entity.Property(e => e.Source)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithMany(p => p.Events)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Events_Accounts");
        });

        modelBuilder.Entity<ItemSimilarity>(entity =>
        {
            entity.HasKey(e => e.SimilarityId).HasName("PK__ItemSimi__64D0C10E684FF281");

            entity.ToTable("ItemSimilarities", "Recommendation");

            entity.HasIndex(e => new { e.SourceProductId, e.SimilarityScore }, "IX_ItemSimilarities_Score").IsDescending(false, true);

            entity.HasIndex(e => e.SourceProductId, "IX_ItemSimilarities_Source");

            entity.Property(e => e.SimilarityId).HasColumnName("SimilarityID");
            entity.Property(e => e.AlgorithmType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("cf");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SimilarProductId).HasColumnName("SimilarProductID");
            entity.Property(e => e.SimilarityScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.SourceProductId).HasColumnName("SourceProductID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.SimilarProduct).WithMany(p => p.ItemSimilaritySimilarProducts)
                .HasForeignKey(d => d.SimilarProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ItemSimilarities_SimilarProduct");

            entity.HasOne(d => d.SourceProduct).WithMany(p => p.ItemSimilaritySourceProducts)
                .HasForeignKey(d => d.SourceProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ItemSimilarities_SourceProduct");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C5061317E87CD884");

            entity.HasIndex(e => e.MaterialName, "UQ__Material__9C87053C5302146A").IsUnique();

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MaterialName).HasMaxLength(25);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF5F588397");

            entity.HasIndex(e => new { e.OrderDate, e.PaymentStatus }, "IX_Orders_ReportByDate");

            entity.HasIndex(e => new { e.OrderCode, e.PaymentStatus, e.StatusId }, "IX_Orders_StatusTracking");

            entity.HasIndex(e => new { e.AccountId, e.OrderDate }, "IX_Orders_UserHistory").IsDescending(false, true);

            entity.HasIndex(e => e.PaymentCode, "IX_Orders_PaymentCode");

            entity.HasIndex(e => e.OrderCode, "UQ__Orders__999B52290487CF36").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActualShippingFee).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.AssignedToStaffId).HasColumnName("AssignedToStaffID");
            entity.Property(e => e.CancelReason).HasMaxLength(500);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DeliveredAt).HasPrecision(0);
            entity.Property(e => e.EstimatedShippingFee).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.OrderCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OrderDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("SHIP_COD");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.ShippedAt).HasPrecision(0);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.ShippingDistrictName).HasMaxLength(100);
            entity.Property(e => e.ShippingName).HasMaxLength(100);
            entity.Property(e => e.ShippingOrderCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShippingPhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ShippingProvinceName).HasMaxLength(100);
            entity.Property(e => e.ShippingWardCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ShippingWardName).HasMaxLength(100);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.VoucherDiscountAmount).HasColumnType("decimal(12, 0)");

            entity.HasOne(d => d.Account).WithMany(p => p.OrderAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Accounts");

            entity.HasOne(d => d.AssignedToStaff).WithMany(p => p.OrderAssignedToStaffs)
                .HasForeignKey(d => d.AssignedToStaffId)
                .HasConstraintName("FK_Orders_AssignedStaff");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.OrderCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK_Orders_CancelledBy");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_StatusOrders");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C69BA0900");

            entity.HasIndex(e => e.ProductId, "IX_OrderDetails_ProductSales");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "UQ_OrderDetails_OrderProduct").IsUnique();

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("(case when ([Quantity]*[UnitPrice]-[DiscountAmount])<(0) then (0) else [Quantity]*[UnitPrice]-[DiscountAmount] end)", true)
                .HasColumnType("decimal(19, 0)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductImage)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.SlotProductId).HasColumnName("SlotProductID");

            entity.HasOne(d => d.Promotion).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_OrderDetails_Promotions");

            entity.HasOne(d => d.SlotProduct).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.SlotProductId)
                .HasConstraintName("FK_OrderDetails_PromotionProductSlots");
        });

        modelBuilder.Entity<OrderRefund>(entity =>
        {
            entity.ToTable("OrderRefunds", tb => tb.HasTrigger("TR_OrderRefunds_ValidateAmount"));

            entity.HasKey(e => e.RefundId).HasName("PK__OrderRef__725AB9001E9FBB52");

            entity.HasIndex(e => e.OrderId, "IX_OrderRefunds_Order");

            entity.HasIndex(e => e.ShippingOrderCode, "IX_OrderRefunds_ShippingOrderCode")
                .HasFilter("([ShippingOrderCode] IS NOT NULL)");

            entity.HasIndex(e => e.RefundCode, "UQ_OrderRefunds_RefundCode")
                .IsUnique();

            entity.Property(e => e.RefundId).HasColumnName("RefundID");
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ReasonDetails).HasMaxLength(500);
            entity.Property(e => e.RefundReasonId).HasColumnName("RefundReasonID");
            entity.Property(e => e.RefundCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ShippingOrderCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.AdminNote).HasMaxLength(1000);
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.RejectedAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.OrderRefundApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_OrderRefunds_ApprovedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.OrderRefundCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderRefunds_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderRefunds_Orders");

            entity.HasOne(d => d.RefundReason).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.RefundReasonId)
                .HasConstraintName("FK_OrderRefunds_RefundReasons");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.OrderRefundRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("FK_OrderRefunds_RequestedBy");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_OrderRefunds_WalletTransactions");

            entity.HasOne(d => d.Status).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderRefunds_StatusRefunds");
        });

        modelBuilder.Entity<RefundImage>(entity =>
        {
            entity.ToTable("RefundImages", tb => tb.HasTrigger("TR_RefundImages_MaxImages"));

            entity.HasKey(e => e.RefundImageId).HasName("PK__RefundIm__1234567890ABCDEF");

            entity.HasIndex(e => e.RefundId, "IX_RefundImages_RefundID").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.RefundImageId).HasColumnName("RefundImageID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.RefundId).HasColumnName("RefundID");

            entity.HasOne(d => d.Refund).WithMany(p => p.RefundImages)
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefundImages_OrderRefunds");
        });

        modelBuilder.Entity<StatusRefund>(entity =>
        {
            entity.ToTable("StatusRefunds");

            entity.HasKey(e => e.StatusId);

            entity.Property(e => e.StatusId)
                .HasColumnName("StatusID")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<RefundDetail>(entity =>
        {
            entity.ToTable("RefundDetails");

            entity.HasKey(e => e.RefundDetailId);

            entity.Property(e => e.RefundDetailId).HasColumnName("RefundDetailID");
            entity.Property(e => e.RefundId).HasColumnName("RefundID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Refund).WithMany(p => p.RefundDetails)
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefundDetails_OrderRefunds");

            entity.HasOne(d => d.Product).WithMany(p => p.RefundDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefundDetails_Products");
        });

        modelBuilder.Entity<RefundStatusHistory>(entity =>
        {
            entity.ToTable("RefundStatusHistory");

            entity.HasKey(e => e.HistoryId);

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.RefundId).HasColumnName("RefundID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.ChangedBy).HasColumnName("ChangedBy");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Refund).WithMany(p => p.RefundStatusHistories)
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefundStatusHistory_OrderRefunds");

            entity.HasOne(d => d.Status).WithMany(p => p.RefundStatusHistories)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefundStatusHistory_StatusRefunds");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.RefundStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK_RefundStatusHistory_ChangedBy");
        });

        modelBuilder.Entity<OrderRefundReason>(entity =>
        {
            entity.HasKey(e => e.RefundReasonId).HasName("PK__OrderRef__9A229525BB82DCC0");

            entity.Property(e => e.RefundReasonId)
                .ValueGeneratedOnAdd()
                .HasColumnName("RefundReasonID");
            entity.Property(e => e.Content).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderSta__4D7B4ADDB5579E5E");

            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK_OrderStatusHistory_ChangedBy");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderStatusHistory_Orders");

            entity.HasOne(d => d.Status).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderStatusHistory_Status");
        });

        modelBuilder.Entity<OrderVoucher>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.VoucherId });

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.DiscountAmountApplied).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.VoucherTarget)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderVouchers_Orders");

            entity.HasOne(d => d.Voucher).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderVouchers_Vouchers");
        });

        modelBuilder.Entity<Origin>(entity =>
        {
            entity.HasKey(e => e.OriginId).HasName("PK__Origins__171FA2C65CE9E745");

            entity.HasIndex(e => e.OriginName, "UQ__Origins__636F5CFDD97DCEC5").IsUnique();

            entity.Property(e => e.OriginId)
                .ValueGeneratedOnAdd()
                .HasColumnName("OriginID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OriginName).HasMaxLength(100);
        });

        modelBuilder.Entity<PaymentGatewayTransaction>(entity =>
        {
            entity.HasKey(e => e.PaymentGatewayTxnId);

            entity.HasIndex(e => e.OrderId, "IX_PayGwTxn_OrderID");

            entity.HasIndex(e => e.PaymentHistoryId, "IX_PayGwTxn_PaymentHistoryID").HasFilter("([PaymentHistoryID] IS NOT NULL)");

            entity.HasIndex(e => new { e.Provider, e.Status }, "IX_PayGwTxn_Provider_Status");

            entity.HasIndex(e => e.RequestId, "UQ__PaymentG__33A8519B13DA0886").IsUnique();

            entity.Property(e => e.PaymentGatewayTxnId).HasColumnName("PaymentGatewayTxnID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentHistoryId).HasColumnName("PaymentHistoryID");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RequestId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("RequestID");
            entity.Property(e => e.ResponseCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ResponseMessage).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TransactionNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentGatewayTransactions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayGwTxn_Orders");

            entity.HasOne(d => d.PaymentHistory).WithMany(p => p.PaymentGatewayTransactions)
                .HasForeignKey(d => d.PaymentHistoryId)
                .HasConstraintName("FK_PayGwTxn_PaymentHistory");
        });

        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.HasKey(e => e.PaymentHistoryId).HasName("PK__PaymentH__F3B93391666E61EF");

            entity.ToTable("PaymentHistory");

            entity.HasIndex(e => e.OrderId, "IX_PaymentHistory_Order");

            entity.Property(e => e.PaymentHistoryId).HasColumnName("PaymentHistoryID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");

            entity.HasOne(d => d.Account).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentHistory_Accounts");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentHistory_Orders");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.PaymentHistories)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_PaymentHistory_WalletTransactions");
        });

        modelBuilder.Entity<PriceRange>(entity =>
        {
            entity.HasKey(e => e.PriceRangeId).HasName("PK__PriceRan__B8A301FFFA15956C");

            entity.Property(e => e.PriceRangeId)
                .ValueGeneratedOnAdd()
                .HasColumnName("PriceRangeID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PriceRangeMax).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PriceRangeMin).HasColumnType("decimal(12, 0)");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED1F57C0B1");

            entity.HasIndex(e => new { e.BrandId, e.ProductStatus }, "IX_Products_Brand_Status").HasFilter("([IsDeleted]=(0) AND [BrandID] IS NOT NULL)");

            entity.HasIndex(e => new { e.CategoryId, e.ProductStatus }, "IX_Products_Category_Status").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.ProductStatus, e.LaunchDate }, "IX_Products_ComingSoon_Launch").HasFilter("([ProductStatus]='ComingSoon' AND [IsDeleted]=(0))");

            entity.HasIndex(e => new { e.CategoryId, e.BrandId, e.Price, e.IsDeleted }, "IX_Products_FilterSort");

            entity.HasIndex(e => new { e.ProductStatus, e.IsDeleted }, "IX_Products_LowStock_V2").HasFilter("([Quantity]<=(10) AND [IsDeleted]=(0) AND [ProductStatus]='Active')");

            entity.HasIndex(e => new { e.PriceRangeId, e.ProductStatus }, "IX_Products_PriceRange_Status").HasFilter("([IsDeleted]=(0) AND [ProductStatus]='Active')");

            entity.HasIndex(e => new { e.ProductName, e.CategoryId, e.BrandId }, "IX_Products_Search");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastLowStockNotifiedAt).HasPrecision(0);
            entity.Property(e => e.LaunchDate).HasPrecision(0);
            entity.Property(e => e.LowStockNotificationEnabled).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PriceRangeId).HasColumnName("PriceRangeID");
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StockThreshold).HasDefaultValue((short)10);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK_Products_Brands");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.PriceRange).WithMany(p => p.Products)
                .HasForeignKey(d => d.PriceRangeId)
                .HasConstraintName("FK_Products_PriceRanges");
        });

        modelBuilder.Entity<ProductDetail>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__ProductD__B40CC6EDB04D8C10");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnName("ProductID");
            entity.Property(e => e.AgeId).HasColumnName("AgeID");
            entity.Property(e => e.Description).HasMaxLength(1500);
            entity.Property(e => e.HeightCm).HasColumnName("HeightCm");
            entity.Property(e => e.LengthCm).HasColumnName("LengthCm");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.OriginId).HasColumnName("OriginID");
            entity.Property(e => e.SexId).HasColumnName("SexID");
            entity.Property(e => e.WeightGram).HasColumnName("WeightGram");
            entity.Property(e => e.WidthCm).HasColumnName("WidthCm");

            entity.HasOne(d => d.Age).WithMany(p => p.ProductDetails)
                .HasForeignKey(d => d.AgeId)
                .HasConstraintName("FK_ProductDetails_Ages");

            entity.HasOne(d => d.Material).WithMany(p => p.ProductDetails)
                .HasForeignKey(d => d.MaterialId)
                .HasConstraintName("FK_ProductDetails_Materials");

            entity.HasOne(d => d.Origin).WithMany(p => p.ProductDetails)
                .HasForeignKey(d => d.OriginId)
                .HasConstraintName("FK_ProductDetails_Origins");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductDetail)
                .HasForeignKey<ProductDetail>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductDetails_Products");

            entity.HasOne(d => d.Sex).WithMany(p => p.ProductDetails)
                .HasForeignKey(d => d.SexId)
                .HasConstraintName("FK_ProductDetails_Sexes");
        });

        modelBuilder.Entity<ProductFollower>(entity =>
        {
            entity.HasKey(e => e.FollowerId).HasName("PK__ProductF__E85940F983D3C9F5");

            entity.HasIndex(e => e.AccountId, "IX_ProductFollowers_Account");

            entity.HasIndex(e => new { e.ProductId, e.NotifiedAt }, "IX_ProductFollowers_Pending").HasFilter("([NotifiedAt] IS NULL)");

            entity.HasIndex(e => new { e.ProductId, e.AccountId }, "UQ_ProductFollowers_ProductAccount").IsUnique();

            entity.Property(e => e.FollowerId).HasColumnName("FollowerID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NotifiedAt).HasPrecision(0);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.ProductFollowers)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductFollowers_Accounts");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductFollowers)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductFollowers_Products");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4EC6261C86C");

            entity.HasIndex(e => e.ProductId, "UQ_ProductImages_OneMain")
                .IsUnique()
                .HasFilter("([IsMain]=(1))");

            entity.HasQueryFilter(e => e.IsMain); // Fix duplicate issue in Include

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Product).WithOne(p => p.ProductImage)
                .HasForeignKey<ProductImage>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<ProductPromotion>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.PromotionId });

            entity.HasIndex(e => e.ProductId, "IX_ProductPromotions_ProductID_Active");

            entity.HasIndex(e => e.PromotionId, "IX_ProductPromotions_PromotionID");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductPromotions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductPromotions_Products");

            entity.HasOne(d => d.Promotion).WithMany(p => p.ProductPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductPromotions_Promotions");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42F2F0105A9F7");

            entity.HasIndex(e => e.Priority, "IX_Promotions_Priority").IsDescending();

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Promotions_Status_Time");

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Promotions_Worker");

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.PromotionName).HasMaxLength(200);
            entity.Property(e => e.PromotionType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Scheduled");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_Accounts");

            entity.HasMany(d => d.OrderDetails).WithOne(p => p.Promotion)
                .HasForeignKey(p => p.PromotionId)
                .HasConstraintName("FK_OrderDetails_Promotions");
        });

        modelBuilder.Entity<PromotionTimeSlot>(entity =>
        {
            entity.HasKey(e => e.TimeSlotId).HasName("PK__Promotio__41CC1F52F08BECC5");

            // Tìm slot đang active nhanh theo khoảng thời gian UTC
            entity.HasIndex(e => new { e.Status, e.StartAt, e.EndAt }, "IX_PromotionTimeSlots_Active");

            // Tra cứu theo promotion + status
            entity.HasIndex(e => new { e.PromotionId, e.Status }, "IX_PromotionTimeSlots_Promotion");

            // Ràng buộc unique: mỗi promotion không có 2 slot trùng thời gian
            entity.HasIndex(e => new { e.PromotionId, e.StartAt, e.EndAt }, "UQ_PromotionTimeSlots_UniqueSlot").IsUnique();

            entity.Property(e => e.TimeSlotId).HasColumnName("TimeSlotID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.StartAt)
                .HasPrecision(0)
                .HasColumnName("StartAt");
            entity.Property(e => e.EndAt)
                .HasPrecision(0)
                .HasColumnName("EndAt");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Scheduled");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionTimeSlots)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PromotionTimeSlots_Promotions");
        });

        // PromotionProductSlots — chỉ dùng cho FLASH_SALE
        // Gắn sản phẩm + giá/số lượng riêng cho từng time slot
        modelBuilder.Entity<PromotionProductSlot>(entity =>
        {
            entity.HasKey(e => e.SlotProductId).HasName("PK_PromotionProductSlots");

            entity.HasIndex(e => new { e.TimeSlotId, e.ProductId }, "UQ_PromotionProductSlots_SlotProduct").IsUnique();

            // Tra cứu sản phẩm flash-sale đang active theo slot
            entity.HasIndex(e => e.TimeSlotId, "IX_PromotionProductSlots_Slot_Active");

            // Tra cứu ngược: sản phẩm đang tham gia slot nào
            entity.HasIndex(e => e.ProductId, "IX_PromotionProductSlots_Product");

            entity.Property(e => e.SlotProductId).HasColumnName("SlotProductID");
            entity.Property(e => e.TimeSlotId).HasColumnName("TimeSlotID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SalePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);
            entity.Property(e => e.ReservedQuantity).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.PromotionProductSlots)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PromotionProductSlots_TimeSlot");

            entity.HasOne(d => d.Product).WithMany(p => p.PromotionProductSlots)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromotionProductSlots_Products");

            entity.HasMany(d => d.OrderDetails).WithOne(p => p.SlotProduct)
                .HasForeignKey(p => p.SlotProductId)
                .HasConstraintName("FK_OrderDetails_PromotionProductSlots");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.ProvinceId).HasName("PK__Province__FD0A6F83F155A109");

            entity.Property(e => e.ProvinceId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ProvinceCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ProvinceName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<ReactionType>(entity =>
        {
            entity.HasKey(e => e.ReactionTypeId).HasName("PK__Reaction__01E625C0806F96F1");

            entity.HasIndex(e => e.Code, "UQ__Reaction__A25C5AA7C65C7840").IsUnique();

            entity.Property(e => e.ReactionTypeId).HasColumnName("ReactionTypeID");
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DisplayName).HasMaxLength(50);
        });

        modelBuilder.Entity<ReviewBlog>(entity =>
        {
            entity.ToTable("ReviewBlogs", tb => tb.HasTrigger("trg_BlogComment_UpdateCommentCount"));

            entity.HasKey(e => e.ReviewBlogId).HasName("PK__ReviewBl__A19536C07ECC639B");

            entity.HasIndex(e => new { e.BlogPostId, e.ModerationStatus, e.CreatedAt }, "IX_ReviewBlogs_BlogPost_Status");

            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt }, "IX_ReviewBlogs_Failed")
                .HasFilter("([ModerationStatus]='Failed' OR [ModerationStatus]='Pending')");

            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.HiddenAt).HasPrecision(0);
            entity.Property(e => e.HiddenBy).HasColumnName("HiddenBy");
            entity.Property(e => e.IsHidden).HasDefaultValue(false);
            entity.Property(e => e.LastRetryAt).HasPrecision(0);
            entity.Property(e => e.ManualReviewDeadline).HasPrecision(0);
            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.RetryCount).HasDefaultValue((byte)0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogs)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogs_Accounts");

            entity.HasOne(d => d.BlogPost).WithMany(p => p.ReviewBlogs)
                .HasForeignKey(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogs_BlogPosts");

            entity.HasOne(d => d.HiddenByNavigation).WithMany(p => p.ReviewBlogHiddenByNavigations)
                .HasForeignKey(d => d.HiddenBy)
                .HasConstraintName("FK_ReviewBlogs_HiddenBy");
        });

        modelBuilder.Entity<ReviewBlogReaction>(entity =>
        {
            entity.ToTable("ReviewBlogReactions", tb => tb.HasTrigger("trg_BlogReaction_UpdateLikeCount"));

            entity.HasKey(e => e.ReactionBlogId).HasName("PK__ReviewBl__6A8A0D2701E53414");

            entity.HasIndex(e => new { e.ReviewBlogId, e.ReactionTypeId }, "IX_ReviewBlogReactions_Stats");

            entity.HasIndex(e => new { e.AccountId, e.ReviewBlogId }, "UQ_ReviewBlogReactions_AccountReview").IsUnique();

            entity.Property(e => e.ReactionBlogId).HasColumnName("ReactionBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReactionTypeId).HasColumnName("ReactionTypeID");
            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReactions_Accounts");

            entity.HasOne(d => d.ReactionType).WithMany(p => p.ReviewBlogReactions)
                .HasForeignKey(d => d.ReactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReactions_ReactionTypes");

            entity.HasOne(d => d.ReviewBlog).WithMany(p => p.ReviewBlogReactions)
                .HasForeignKey(d => d.ReviewBlogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReactions_ReviewBlogs");
        });

        modelBuilder.Entity<ReviewBlogReply>(entity =>
        {
            entity.ToTable("ReviewBlogReplies", tb => tb.HasTrigger("trg_ReviewBlogReply_UpdateCommentCount"));

            entity.HasKey(e => e.ReplyBlogId).HasName("PK__ReviewBl__5996364139D7A15C");

            entity.HasIndex(e => new { e.ReviewBlogId, e.ModerationStatus, e.CreatedAt }, "IX_ReviewBlogReplies_Comment_Status");

            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt }, "IX_ReviewBlogReplies_Failed")
                .HasFilter("([ModerationStatus]='Failed' OR [ModerationStatus]='Pending')");

            entity.Property(e => e.ReplyBlogId).HasColumnName("ReplyBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.HiddenAt).HasPrecision(0);
            entity.Property(e => e.HiddenBy).HasColumnName("HiddenBy");
            entity.Property(e => e.IsHidden).HasDefaultValue(false);
            entity.Property(e => e.LastRetryAt).HasPrecision(0);
            entity.Property(e => e.ManualReviewDeadline).HasPrecision(0);
            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.RetryCount).HasDefaultValue((byte)0);
            entity.Property(e => e.ParentReplyId).HasColumnName("ParentReplyID");
            entity.Property(e => e.ReplyToAccountId).HasColumnName("ReplyToAccountID");
            entity.Property(e => e.ReviewBlogId).HasColumnName("ReviewBlogID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogReplyAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplies_Accounts");

            entity.HasOne(d => d.HiddenByNavigation).WithMany(p => p.ReviewBlogReplyHiddenByNavigations)
                .HasForeignKey(d => d.HiddenBy)
                .HasConstraintName("FK_ReviewBlogReplies_HiddenBy");

            entity.HasOne(d => d.ParentReply).WithMany(p => p.InverseParentReply)
                .HasForeignKey(d => d.ParentReplyId)
                .HasConstraintName("FK_ReviewBlogReplies_Parent");

            entity.HasOne(d => d.ReplyToAccount).WithMany(p => p.ReviewBlogReplyReplyToAccounts)
                .HasForeignKey(d => d.ReplyToAccountId)
                .HasConstraintName("FK_ReviewBlogReplies_ReplyTo");

            entity.HasOne(d => d.ReviewBlog).WithMany(p => p.ReviewBlogReplies)
                .HasForeignKey(d => d.ReviewBlogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplies_ReviewBlogs");
        });

        modelBuilder.Entity<BlogPostReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionPostId).HasName("PK_BlogPostReactions");
            entity.ToTable("BlogPostReactions", tb => tb.HasTrigger("TR_BlogPostReactions"));

            entity.HasIndex(e => new { e.BlogPostId, e.ReactionTypeId }, "IX_BlogPostReactions_Stats");

            entity.HasIndex(e => new { e.AccountId, e.BlogPostId }, "UQ_BlogPostReactions_AccountPost").IsUnique();

            entity.Property(e => e.ReactionPostId).HasColumnName("ReactionPostID");
            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ReactionTypeId).HasColumnName("ReactionTypeID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Account).WithMany(p => p.BlogPostReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPostReactions_Accounts");

            entity.HasOne(d => d.BlogPost).WithMany(p => p.BlogPostReactions)
                .HasForeignKey(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPostReactions_BlogPosts");

            entity.HasOne(d => d.ReactionType).WithMany(p => p.BlogPostReactions)
                .HasForeignKey(d => d.ReactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPostReactions_ReactionTypes");
        });

        modelBuilder.Entity<ReviewBlogReplyReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionReplyBlogId).HasName("PK_ReviewBlogReplyReactions");

            entity.HasIndex(e => new { e.ReplyBlogId, e.ReactionTypeId }, "IX_ReviewBlogReplyReactions_Stats");

            entity.HasIndex(e => new { e.AccountId, e.ReplyBlogId }, "UQ_ReviewBlogReplyReactions_AccountReply").IsUnique();

            entity.Property(e => e.ReactionReplyBlogId).HasColumnName("ReactionReplyBlogID");
            entity.Property(e => e.ReplyBlogId).HasColumnName("ReplyBlogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ReactionTypeId).HasColumnName("ReactionTypeID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewBlogReplyReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplyReactions_Accounts");

            entity.HasOne(d => d.ReplyBlog).WithMany(p => p.ReviewBlogReplyReactions)
                .HasForeignKey(d => d.ReplyBlogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplyReactions_ReviewBlogReplies");

            entity.HasOne(d => d.ReactionType).WithMany(p => p.ReviewBlogReplyReactions)
                .HasForeignKey(d => d.ReactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewBlogReplyReactions_ReactionTypes");
        });

        modelBuilder.Entity<BlogCommentBanReason>(entity =>
        {
            entity.HasKey(e => e.BanReasonId).HasName("PK_BlogCommentBanReasons");

            entity.Property(e => e.BanReasonId).HasColumnName("BanReasonID");
            entity.Property(e => e.Content).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<BlogCommentModerationLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK_BlogCommentModerationLogs");

            entity.HasIndex(e => new { e.CommentId, e.CreatedAt }, "IX_BCML_Comment")
                .HasFilter("([CommentID] IS NOT NULL)");

            entity.HasIndex(e => new { e.ReplyId, e.CreatedAt }, "IX_BCML_Reply")
                .HasFilter("([ReplyID] IS NOT NULL)");

            entity.HasIndex(e => new { e.Action, e.CreatedAt }, "IX_BCML_ManualReview")
                .HasFilter("([Action]='ManualReview' OR [Action]='Failed')");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BanReasonId).HasColumnName("BanReasonID");
            entity.Property(e => e.CommentId).HasColumnName("CommentID");
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModeratedBy).HasColumnName("ModeratedBy");
            entity.Property(e => e.ModerationResult).HasColumnName("ModerationResult");
            entity.Property(e => e.ModeratorType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ReplyId).HasColumnName("ReplyID");
            entity.Property(e => e.TargetType)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.BanReason).WithMany(p => p.BlogCommentModerationLogs)
                .HasForeignKey(d => d.BanReasonId)
                .HasConstraintName("FK_BCML_BanReasons");

            entity.HasOne(d => d.Comment).WithMany(p => p.BlogCommentModerationLogs)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("FK_BCML_ReviewBlogs");

            entity.HasOne(d => d.ModeratedByNavigation).WithMany(p => p.BlogCommentModerationLogs)
                .HasForeignKey(d => d.ModeratedBy)
                .HasConstraintName("FK_BCML_Accounts");

            entity.HasOne(d => d.Reply).WithMany(p => p.BlogCommentModerationLogs)
                .HasForeignKey(d => d.ReplyId)
                .HasConstraintName("FK_BCML_ReviewBlogReplies");
        });

        modelBuilder.Entity<BlogCommentViolationCount>(entity =>
        {
            entity.ToTable("BlogCommentViolationCount");

            entity.HasKey(e => e.AccountId).HasName("PK_BlogCommentViolationCount");

            entity.HasIndex(e => new { e.IsCommentBanned, e.BanExpiresAt }, "IX_BCVC_Banned")
                .HasFilter("([IsCommentBanned]=(1))");

            entity.Property(e => e.AccountId)
                .ValueGeneratedNever()
                .HasColumnName("AccountID");
            entity.Property(e => e.BanExpiresAt).HasPrecision(0);
            entity.Property(e => e.BannedAt).HasPrecision(0);
            entity.Property(e => e.IsCommentBanned).HasDefaultValue(false);
            entity.Property(e => e.LastViolatedAt).HasPrecision(0);
            entity.Property(e => e.RateWindowAt).HasPrecision(0);
            entity.Property(e => e.UnbannedAt).HasPrecision(0);
            entity.Property(e => e.UnbannedBy).HasColumnName("UnbannedBy");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.ViolationCount).HasDefaultValue((byte)0);
            entity.Property(e => e.RateCount).HasDefaultValue((byte)0);

            entity.HasOne(d => d.Account).WithOne(p => p.BlogCommentViolationCountAccount)
                .HasForeignKey<BlogCommentViolationCount>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BCVC_Accounts");

            entity.HasOne(d => d.UnbannedByNavigation).WithMany(p => p.BlogCommentViolationCountUnbannedByNavigations)
                .HasForeignKey(d => d.UnbannedBy)
                .HasConstraintName("FK_BCVC_UnbannedBy");
        });

        modelBuilder.Entity<ReviewProduct>(entity =>
        {
            entity.ToTable("ReviewProducts", tb => tb.HasTrigger("TR_ReviewProducts_ValidateOrderDetail"));

            entity.HasKey(e => e.ReviewId).HasName("PK__ReviewPr__74BC79AE912C0FED");

            entity.HasIndex(e => new { e.ProductId, e.IsDeleted }, "IX_ReviewProducts_Product");

            entity.HasIndex(e => new { e.AccountId, e.OrderId, e.ProductId }, "UQ_Review_Account_Order_Product").IsUnique();

            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt }, "IX_ReviewProducts_ModerationPending")
                .HasFilter("([ModerationStatus]='Pending' AND [IsDeleted]=(0))");

            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt }, "IX_ReviewProducts_ManualReview")
                .HasFilter("([ModerationStatus]='ManualReview' AND [IsDeleted]=(0))");

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsEdited).HasDefaultValue(false);

            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProducts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Accounts");

            entity.HasOne(d => d.Order).WithMany(p => p.ReviewProducts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.ReviewProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Products");
        });

        modelBuilder.Entity<ReviewProductImage>(entity =>
        {
            entity.ToTable("ReviewProductImages", tb => tb.HasTrigger("TR_ReviewProductImages_MaxImages"));

            entity.HasKey(e => e.ReviewProductImageId).HasName("PK__ReviewPr__013E0F1E09405F43");

            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt }, "IX_ReviewProductImages_ModerationPending")
                .HasFilter("([ModerationStatus]='Pending' AND [IsDeleted]=(0))");

            entity.Property(e => e.ReviewProductImageId).HasColumnName("ReviewProductImageID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ImageURL");

            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductImages)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductImages_ReviewProducts");
        });

        modelBuilder.Entity<ReviewProductReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionProductId).HasName("PK__ReviewPr__B56FCAF15CBA344B");

            entity.HasIndex(e => new { e.AccountId, e.ReviewProductId }, "UQ_ReviewProductReactions_AccountReview").IsUnique();

            entity.Property(e => e.ReactionProductId).HasColumnName("ReactionProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReactionTypeId).HasColumnName("ReactionTypeID");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReactions_Accounts");

            entity.HasOne(d => d.ReactionTypeNavigation).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.ReactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReactions_ReactionTypes");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReactions_ReviewProducts");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3ACBF2EEFE");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61608A369932").IsUnique();

            entity.Property(e => e.RoleId)
                .ValueGeneratedOnAdd()
                .HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Sex>(entity =>
        {
            entity.HasKey(e => e.SexId).HasName("PK__Sexes__75622DB6312F4F3E");

            entity.HasIndex(e => e.SexName, "UQ__Sexes__BA354290E01BBEF1").IsUnique();

            entity.Property(e => e.SexId)
                .ValueGeneratedOnAdd()
                .HasColumnName("SexID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SexName).HasMaxLength(4);
        });

        modelBuilder.Entity<ShippingProviderTransaction>(entity =>
        {
            entity.HasKey(e => e.ShippingTransactionId).HasName("PK__Shipping__F215F69363919D74");

            entity.HasIndex(e => new { e.Provider, e.Status, e.OrderId }, "IX_ShippingProviderTransactions_Polling").HasFilter("([Status]<>'delivered' AND [Status]<>'returned' AND [Status]<>'return_fail' AND [Status]<>'exception' AND [Status]<>'damage' AND [Status]<>'lost' AND [Status]<>'cancel')");

            entity.HasIndex(e => e.OrderId, "IX_ShippingTxn_OrderID");

            entity.HasIndex(e => new { e.Provider, e.Status }, "IX_ShippingTxn_Provider_Status");

            entity.Property(e => e.ShippingTransactionId).HasColumnName("ShippingTransactionID");
            entity.Property(e => e.ActualDelivery).HasPrecision(0);
            entity.Property(e => e.CodAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EstimatedDelivery).HasPrecision(0);
            entity.Property(e => e.LastErrorMessage).HasMaxLength(500);
            entity.Property(e => e.LastPolledAt).HasPrecision(0);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProviderOrderCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ServiceType).HasMaxLength(100);
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Order).WithMany(p => p.ShippingProviderTransactions)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShippingTxn_Orders");
        });

        modelBuilder.Entity<ShippingStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Shipping__4D7B4ABDF6123545");

            entity.HasIndex(e => e.ShippingTxId, "IX_ShippingStatusHistories_ShippingTxId");

            entity.Property(e => e.NewStatus).HasMaxLength(50);
            entity.Property(e => e.PreviousStatus).HasMaxLength(50);
            entity.Property(e => e.ProcessedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Source).HasMaxLength(20);

            entity.HasOne(d => d.Order).WithMany(p => p.ShippingStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShippingS__Order__1C5231C2");

            entity.HasOne(d => d.ShippingTx).WithMany(p => p.ShippingStatusHistories)
                .HasForeignKey(d => d.ShippingTxId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShippingS__Shipp__1B5E0D89");
        });

        modelBuilder.Entity<StaffReviewProductReply>(entity =>
        {
            entity.HasKey(e => e.ReplyProductId).HasName("PK__StaffRev__2DCE233CE3243EE5");

            entity.Property(e => e.ReplyProductId).HasColumnName("ReplyProductID");
            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.StaffReviewProductReplies)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReplies_ReviewProducts");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffReviewProductReplies)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReplies_Accounts");
        });

        modelBuilder.Entity<StatusOrder>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__StatusOr__C8EE20436DF30AC3");

            entity.HasIndex(e => e.StatusName, "UQ__StatusOr__05E7698A8B998D32").IsUnique();

            entity.Property(e => e.StatusId)
                .ValueGeneratedOnAdd()
                .HasColumnName("StatusID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<SuperCategory>(entity =>
        {
            entity.HasKey(e => e.SuperCategoryId).HasName("PK__SuperCat__CEB990D38D215BB2");

            entity.HasIndex(e => e.SuperCategoryName, "UQ__SuperCat__3FA779DF65E3CBC6").IsUnique();

            entity.Property(e => e.SuperCategoryId).HasColumnName("SuperCategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SuperCategoryName).HasMaxLength(25);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Template__F87ADD07CB394BD6");

            entity.ToTable("Templates", "Notification");

            entity.HasIndex(e => e.TemplateCode, "UQ__Template__0FDB5081E141D516").IsUnique();

            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MessageTemplate).HasMaxLength(500);
            entity.Property(e => e.UsageScope)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ADMIN");
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TitleTemplate).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<TrendingProduct>(entity =>
        {
            entity.HasKey(e => e.TrendingId).HasName("PK__Trending__8938F5282894286C");

            entity.ToTable("TrendingProducts", "Recommendation");

            entity.HasIndex(e => new { e.Scope, e.WindowHours, e.Rank }, "IX_Trending_Scope_Rank");

            entity.HasIndex(e => new { e.ProductId, e.Scope, e.WindowHours }, "UQ_Trending_Product_Scope_Window").IsUnique();

            entity.Property(e => e.TrendingId).HasColumnName("TrendingID");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Scope)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("global");
            entity.Property(e => e.Score).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.WindowHours).HasDefaultValue((byte)24);

            entity.HasOne(d => d.Product).WithMany(p => p.TrendingProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trending_Products");
        });

        modelBuilder.Entity<UserBlockHistory>(entity =>
        {
            entity.HasKey(e => e.BlockId).HasName("PK__UserBloc__144215117FE0AB63");

            entity.ToTable("UserBlockHistory");

            entity.HasIndex(e => new { e.BlockedUntil, e.AccountId }, "IX_UserBlockHistory_PendingUnblock").HasFilter("([UnblockedAt] IS NULL)");

            entity.Property(e => e.BlockId).HasColumnName("BlockID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlockReasonId).HasColumnName("BlockReasonID");
            entity.Property(e => e.BlockedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BlockedUntil).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.UnblockedAt).HasPrecision(0);
            entity.Property(e => e.UnblockedByJobId).HasColumnName("UnblockedByJobID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.UserBlockHistoryAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBlockHistory_Account");

            entity.HasOne(d => d.BlockReason).WithMany(p => p.UserBlockHistories)
                .HasForeignKey(d => d.BlockReasonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBlockHistory_BlockReasons");

            entity.HasOne(d => d.BlockedByNavigation).WithMany(p => p.UserBlockHistoryBlockedByNavigations)
                .HasForeignKey(d => d.BlockedBy)
                .HasConstraintName("FK_UserBlockHistory_BlockedBy");

            entity.HasOne(d => d.UnblockedByNavigation).WithMany(p => p.UserBlockHistoryUnblockedByNavigations)
                .HasForeignKey(d => d.UnblockedBy)
                .HasConstraintName("FK_UserBlockHistory_UnblockedBy");

            entity.HasOne(d => d.UnblockedByJob).WithMany(p => p.UserBlockHistories)
                .HasForeignKey(d => d.UnblockedByJobId)
                .HasConstraintName("FK_UserBlockHistory_BackgroundJobs");
        });

        modelBuilder.Entity<UserProductScore>(entity =>
        {
            entity.HasKey(e => e.ScoreId).HasName("PK__UserProd__7DD229F1BA602B69");

            entity.ToTable("UserProductScores", "Recommendation");

            entity.HasIndex(e => new { e.AccountId, e.Score }, "IX_UPS_User").IsDescending(false, true);

            entity.HasIndex(e => new { e.AccountId, e.ProductId }, "UQ_UserProductScores").IsUnique();

            entity.Property(e => e.ScoreId).HasColumnName("ScoreID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ComputedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastInteractedAt).HasPrecision(0);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Score).HasColumnType("decimal(8, 4)");

            entity.HasOne(d => d.Account).WithMany(p => p.UserProductScores)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UPS_Accounts");

            entity.HasOne(d => d.Product).WithMany(p => p.UserProductScores)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UPS_Products");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE79C105B9C507");

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Vouchers_Worker");

            entity.HasIndex(e => e.VoucherCode, "UQ_Vouchers_Code_Active")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountTarget)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DiscountType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.MaxDiscountCap).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.MaxUsagePerUser).HasDefaultValue((short)1);
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VoucherDescription).HasMaxLength(255);
            entity.Property(e => e.VoucherName).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Vouchers_Accounts");
        });

        modelBuilder.Entity<VoucherUsageLog>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__VoucherU__29B197C0D357122E");

            entity.HasIndex(e => new { e.VoucherId, e.UsedAt }, "IX_VoucherUsageLogs_Analytics");

            entity.Property(e => e.UsageId).HasColumnName("UsageID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.UsedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");

            entity.HasOne(d => d.Account).WithMany(p => p.VoucherUsageLogs)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherUsageLogs_Accounts");

            entity.HasOne(d => d.Order).WithMany(p => p.VoucherUsageLogs)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherUsageLogs_Orders");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherUsageLogs)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherUsageLogs_Vouchers");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92E5D7899BD");

            entity.HasIndex(e => e.AccountId, "UQ__Wallets__349DA587207D60C0").IsUnique();

            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Balance).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .IsFixedLength();
            entity.Property(e => e.LastTransactionAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wallets_Accounts");
        });

        modelBuilder.Entity<WalletPin>(entity =>
        {
            entity.HasKey(e => e.WalletPinId);

            entity.HasIndex(e => e.WalletId, "UQ_WalletPins_WalletID")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.WalletPinId).HasColumnName("WalletPinID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FailedAttempts).HasDefaultValue((byte)0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LockedUntil).HasPrecision(0);
            entity.Property(e => e.PinHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TotalFailedAttempts).HasDefaultValue((byte)0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletPins)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletPins_Wallets");
        });

        modelBuilder.Entity<WalletPinAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId);

            entity.HasIndex(e => new { e.WalletId, e.CreatedAt }, "IX_WalletPinAttempts_Wallet").IsDescending(false, true);

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActionType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Account).WithMany(p => p.WalletPinAttempts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletPinAttempts_Accounts");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletPinAttempts)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletPinAttempts_Wallets");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId).HasName("PK__WalletTr__7184AECF832C4B4D");

            entity.HasIndex(e => new { e.AccountId, e.CreatedAt }, "IX_WalletTransactions_Account").IsDescending(false, true);

            entity.HasIndex(e => e.RelatedOrderId, "IX_WalletTransactions_Order").HasFilter("([RelatedOrderID] IS NOT NULL)");

            entity.HasIndex(e => new { e.WalletId, e.CreatedAt }, "IX_WalletTransactions_Wallet").IsDescending(false, true);

            entity.HasIndex(e => e.IdempotencyKey, "UQ_WalletTransactions_IdempotencyKey")
                .IsUnique()
                .HasFilter("([IdempotencyKey] IS NOT NULL)");

            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.BalanceBefore).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Direction)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ExternalRef)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdempotencyKey)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Metadata).HasMaxLength(1000);
            entity.Property(e => e.Method)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.RelatedOrderId).HasColumnName("RelatedOrderID");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TxnType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Account).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTransactions_Accounts");

            entity.HasOne(d => d.RelatedOrder).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.RelatedOrderId)
                .HasConstraintName("FK_WalletTransactions_Orders");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTransactions_Wallets");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.WardCode).HasName("PK__Wards__1A7FBFF15512F9D4");

            entity.HasIndex(e => e.DistrictId, "IX_Wards_DistrictId");

            entity.Property(e => e.WardCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.WardName).HasMaxLength(100);

            entity.HasOne(d => d.District).WithMany(p => p.Wards)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wards_Districts");
        });

        modelBuilder.Entity<Widget>(entity =>
        {
            entity.HasKey(e => e.WidgetId).HasName("PK__Widgets__ADFD3072E41ABF05");

            entity.ToTable("Widgets", "Recommendation");

            entity.HasIndex(e => e.WidgetCode, "UQ__Widgets__C77DBD58FBAAB47D").IsUnique();

            entity.Property(e => e.WidgetId)
                .ValueGeneratedOnAdd()
                .HasColumnName("WidgetID");
            entity.Property(e => e.Algorithm)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Config).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FallbackAlgo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxItems).HasDefaultValue((byte)10);
            entity.Property(e => e.WidgetCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WidgetName).HasMaxLength(100);
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__233189CBFF107F0A");

            entity.HasIndex(e => e.AccountId, "IX_Wishlists_User");

            entity.HasIndex(e => new { e.AccountId, e.ProductId }, "UQ_Wishlists_AccountProduct").IsUnique();

            entity.Property(e => e.WishlistId).HasColumnName("WishlistID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlists_Accounts");

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlists_Products");
        });

        modelBuilder.Entity<CustomerChild>(entity =>
        {
            entity.HasKey(e => e.ChildId).HasName("PK__CustomerChildren__1234");

            entity.HasIndex(e => e.Dob, "IX_CustomerChildren_DOB").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.SexId).HasColumnName("SexID");
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.NickName).HasMaxLength(50);
            entity.Property(e => e.BirthdayNotifiedYear).HasColumnName("BirthdayNotifiedYear");
            entity.Property(e => e.EditCount)
                .HasColumnName("EditCount")
                .HasDefaultValue(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.CustomerChildren)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerChildren_Accounts");

            entity.HasOne(d => d.Sex).WithMany(p => p.CustomerChildren)
                .HasForeignKey(d => d.SexId)
                .HasConstraintName("FK_CustomerChildren_Sexes");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.PreferenceId).HasName("PK__UserPreferences__1234");

            entity.ToTable("UserPreferences", "Notification");

            entity.HasIndex(e => e.AccountId, "UQ_UserPreferences_AccountID").IsUnique();

            entity.Property(e => e.PreferenceId).HasColumnName("PreferenceID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.EmailOptIn).HasDefaultValue(true);
            entity.Property(e => e.WebPushOptIn).HasDefaultValue(false);
            entity.Property(e => e.OrderUpdates).HasDefaultValue(true);
            entity.Property(e => e.Promotions).HasDefaultValue(true);
            entity.Property(e => e.StockAlerts).HasDefaultValue(true);
            entity.Property(e => e.BlogAlerts).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithOne(p => p.UserPreference)
                .HasForeignKey<UserPreference>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPreferences_Accounts");
        });

        modelBuilder.Entity<ReviewModerationLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__ReviewMo__5E5499A8");

            entity.HasIndex(e => new { e.ReviewId, e.CreatedAt }, "IX_ModerationLogs_Review").IsDescending(false, true);

            entity.HasIndex(e => new { e.Action, e.CreatedAt }, "IX_ModerationLogs_ManualReview")
                .HasFilter("([Action]='ManualReview')");

            entity.HasIndex(e => new { e.ModeratorType, e.AiModelVersion, e.Action }, "IX_ModerationLogs_AIPerformance")
                .HasFilter("([ModeratorType]='AI')");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AiModelVersion)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("AIModelVersion");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.ModeratorType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.TargetType)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Image).WithMany(p => p.ReviewModerationLogs)
                .HasForeignKey(d => d.ImageId)
                .HasConstraintName("FK_ModerationLogs_ReviewProductImages");

            entity.HasOne(d => d.ModeratedByNavigation).WithMany(p => p.ReviewModerationLogs)
                .HasForeignKey(d => d.ModeratedBy)
                .HasConstraintName("FK_ModerationLogs_Accounts");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewModerationLogs)
                .HasForeignKey(d => d.ReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ModerationLogs_ReviewProducts");
        });

        // ── Luồng B entity configurations ────────────────────────────────────
        modelBuilder.Entity<ShiftTemplate>(entity =>
        {
            entity.HasKey(e => e.ShiftTemplateId).HasName("PK_ShiftTemplates");

            entity.ToTable("ShiftTemplates");

            entity.HasIndex(e => e.ShiftName, "UQ_ShiftTemplates_ShiftName").IsUnique();

            entity.Property(e => e.ShiftTemplateId)
                .ValueGeneratedOnAdd()
                .HasColumnName("ShiftTemplateID");
            entity.Property(e => e.ShiftName).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.MaxOrdersPerShift).HasDefaultValue((short)20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK_WorkSchedules");

            entity.ToTable("WorkSchedules", tb => tb.HasTrigger("TR_WorkSchedules_CreateCapacity"));

            entity.HasIndex(
                e => new { e.AccountId, e.WorkDate, e.ShiftTemplateId },
                "UQ_WorkSchedules_StaffShiftDay").IsUnique();

            entity.HasIndex(
                e => new { e.WorkDate, e.Status },
                "IX_WorkSchedules_Date_Status");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ShiftTemplateId).HasColumnName("ShiftTemplateID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Scheduled");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkSchedules_Accounts");

            entity.HasOne(d => d.ShiftTemplate)
                .WithMany(p => p.WorkSchedules)
                .HasForeignKey(d => d.ShiftTemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkSchedules_ShiftTemplates");

            entity.HasOne(d => d.CreatedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkSchedules_CreatedBy");
        });

        modelBuilder.Entity<StaffShiftCapacity>(entity =>
        {
            entity.HasKey(e => e.CapacityId).HasName("PK_StaffShiftCapacity");

            entity.ToTable("StaffShiftCapacity");

            entity.HasIndex(e => e.ScheduleId, "UQ_SSC_ScheduleID").IsUnique();

            entity.Property(e => e.CapacityId).HasColumnName("CapacityID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CurrentLoad).HasDefaultValue((short)0);
            entity.Property(e => e.MaxLoad).HasDefaultValue((short)20);
            entity.Property(e => e.ShiftFullNotifiedAt).HasPrecision(3);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.WorkSchedule)
                .WithOne(p => p.StaffShiftCapacity)
                .HasForeignKey<StaffShiftCapacity>(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SSC_Schedules");

            entity.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SSC_Accounts");
        });

        modelBuilder.Entity<OrderAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK_OrderAssignments");

            entity.ToTable("OrderAssignments");

            entity.HasIndex(
                e => new { e.OrderId, e.RoleId },
                "UQ_OA_ActiveOrderRole")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.OrderId, "IX_OA_OrderID");
            entity.HasIndex(e => new { e.AccountId, e.AssignedAt }, "IX_OA_AccountID_Date").IsDescending(false, true);
            entity.HasIndex(e => e.ScheduleId, "IX_OA_ScheduleID");

            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AssignedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OA_Orders");

            entity.HasOne(d => d.WorkSchedule)
                .WithMany(p => p.OrderAssignments)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OA_Schedules");

            entity.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OA_Accounts");

            entity.HasOne(d => d.AssignedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_OA_AssignedBy");

            entity.HasOne(d => d.Role)
                .WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OA_Roles");
        });

        modelBuilder.Entity<OrderQueue>(entity =>
        {
            entity.HasKey(e => e.QueueId).HasName("PK_OrderQueue");

            entity.ToTable("OrderQueue");

            entity.HasIndex(e => e.OrderId, "UQ_OQ_OrderID").IsUnique();

            entity.HasIndex(
                e => new { e.IsResolved, e.QueuedAt },
                "IX_OQ_Unresolved")
                .HasFilter("([IsResolved]=(0))");

            entity.Property(e => e.QueueId).HasColumnName("QueueID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.QueuedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Reason)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ResolvedAt).HasPrecision(0);
            entity.Property(e => e.IsResolved).HasDefaultValue(false);

            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OQ_Orders");

            entity.HasOne(d => d.AssignedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_OQ_AssignedBy");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
