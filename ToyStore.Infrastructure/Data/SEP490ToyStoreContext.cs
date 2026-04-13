using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Models;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext : DbContext
{
    public SEP490ToyStoreContext(DbContextOptions<SEP490ToyStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Age> Ages { get; set; }

    public virtual DbSet<BackgroundJob> BackgroundJobs { get; set; }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<BlockReason> BlockReasons { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ChatConversation> ChatConversations { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DomainEventOutbox> DomainEventOutboxes { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<ItemSimilarity> ItemSimilarities { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderRefund> OrderRefunds { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OrderVoucher> OrderVouchers { get; set; }

    public virtual DbSet<Origin> Origins { get; set; }

    public virtual DbSet<PaymentHistory> PaymentHistories { get; set; }

    public virtual DbSet<PriceRange> PriceRanges { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductDetail> ProductDetails { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<ReviewProduct> ReviewProducts { get; set; }

    public virtual DbSet<ReviewProductImage> ReviewProductImages { get; set; }

    public virtual DbSet<ReviewProductReaction> ReviewProductReactions { get; set; }

    public virtual DbSet<ReviewProductReply> ReviewProductReplies { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sex> Sexes { get; set; }

    public virtual DbSet<StatusOrder> StatusOrders { get; set; }

    public virtual DbSet<SuperCategory> SuperCategories { get; set; }

    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<UserBlockHistory> UserBlockHistories { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherType> VoucherTypes { get; set; }

    public virtual DbSet<VoucherUsageLog> VoucherUsageLogs { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA5863627A0DC");

            entity.HasIndex(e => e.EmployeeCode, "IX_Accounts_EmployeeCode")
                .IsUnique()
                .HasFilter("([EmployeeCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.Email, e.IsActive, e.IsDeleted }, "IX_Accounts_Login");

            entity.HasIndex(e => e.Email, "UQ__Accounts__A9D10534E30722DF").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AccountName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Image)
                .HasMaxLength(500)
                .IsUnicode(false);
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
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__091C2A1BCB21757F");

            entity.HasIndex(e => e.AccountId, "IX_Addresses_OneDefaultPerUser")
                .IsUnique()
                .HasFilter("([IsDefault]=(1) AND [IsDeleted]=(0))");

            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AddressLine).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.RecipientPhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Ward).HasMaxLength(100);

            entity.HasOne(d => d.Account).WithOne(p => p.Address)
                .HasForeignKey<Address>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Addresses_Accounts");
        });

        modelBuilder.Entity<Age>(entity =>
        {
            entity.HasKey(e => e.AgeId).HasName("PK__Ages__875454C2D6735AB3");

            entity.HasIndex(e => e.AgeRange, "UQ__Ages__E0EBEE385911F773").IsUnique();

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
            entity.HasKey(e => e.JobId).HasName("PK__Backgrou__056690E29F3B2565");

            entity.ToTable("BackgroundJobs", "System");

            entity.HasIndex(e => e.JobName, "UQ__Backgrou__F1AC1A95CF711544").IsUnique();

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

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK__Banners__32E86A31D6C2DD35");

            entity.Property(e => e.BannerId).HasColumnName("BannerID");
            entity.Property(e => e.BannerName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LinkUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Position)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("HomePage");
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Banners)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Banners_Accounts");
        });

        modelBuilder.Entity<BlockReason>(entity =>
        {
            entity.HasKey(e => e.ReasonId).HasName("PK__BlockRea__A4F8C0C7B3BFA276");

            entity.Property(e => e.ReasonId).HasColumnName("ReasonID");
            entity.Property(e => e.Content).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ReasonCode)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.BlogCategoryId).HasName("PK__BlogCate__6BD2DA61842D2D07");

            entity.HasIndex(e => e.BlogCategoriesName, "UQ__BlogCate__CD921A00A85607DE").IsUnique();

            entity.Property(e => e.BlogCategoryId).HasColumnName("BlogCategoryID");
            entity.Property(e => e.BlogCategoriesName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.BlogPostId).HasName("PK__BlogPost__321741498654D05B");

            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlogAt).HasPrecision(0);
            entity.Property(e => e.BlogThumbnail)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.BlogTitle).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Account).WithMany(p => p.BlogPostAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPosts_Accounts");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.BlogPostApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_BlogPosts_ApprovedBy");

            entity.HasMany(d => d.BlogCategories).WithMany(p => p.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    r => r.HasOne<BlogCategory>().WithMany()
                        .HasForeignKey("BlogCategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_BlogPostCategories_BlogCategories"),
                    l => l.HasOne<BlogPost>().WithMany()
                        .HasForeignKey("BlogPostId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_BlogPostCategories_BlogPosts"),
                    j =>
                    {
                        j.HasKey("BlogPostId", "BlogCategoryId");
                        j.ToTable("BlogPostCategories");
                        j.IndexerProperty<int>("BlogPostId").HasColumnName("BlogPostID");
                        j.IndexerProperty<short>("BlogCategoryId").HasColumnName("BlogCategoryID");
                    });
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Brands__DAD4F3BE4EA66BF7");

            entity.HasIndex(e => e.BrandName, "UQ__Brands__2206CE9BDA5F0959").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD797F2C8258E");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.AccountId, "UQ__Cart__349DA5874853AEF2").IsUnique();

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
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B2AB6C5E03F");

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
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2BFA7E33C9");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E0E7230115").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
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

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__ChatConv__C050D897A6CE4B6E");

            entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SessionID");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("BotActive");

            entity.HasOne(d => d.Account).WithMany(p => p.ChatConversations)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_ChatConversations_Accounts");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__ChatMess__C87C037CD2FD0229");

            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "IX_ChatMessages_Conversation").IsDescending(false, true);

            entity.Property(e => e.MessageId).HasColumnName("MessageID");
            entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SenderType)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Conversation).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessages_ChatConversations");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__626D8FEE55944138");

            entity.ToTable("Deliveries", "Notification");

            entity.HasIndex(e => new { e.AccountId, e.Status }, "IX_NotificationDeliveries_User").HasFilter("([Status]='Unread')");

            entity.Property(e => e.DeliveryId).HasColumnName("DeliveryID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedByJobId).HasColumnName("CreatedByJobID");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Unread");
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Account).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationDeliveries_Accounts");

            entity.HasOne(d => d.CreatedByJob).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.CreatedByJobId)
                .HasConstraintName("FK_NotificationDeliveries_BackgroundJobs");

            entity.HasOne(d => d.TemplateCodeNavigation).WithMany(p => p.Deliveries)
                .HasPrincipalKey(p => p.TemplateCode)
                .HasForeignKey(d => d.TemplateCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationDeliveries_Templates");
        });

        modelBuilder.Entity<DomainEventOutbox>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__DomainEv__7944C870DCC51FB4");

            entity.ToTable("DomainEventOutbox", "System");

            entity.HasIndex(e => e.ProcessedOn, "IX_DomainEventOutbox_Pending").HasFilter("([ProcessedOn] IS NULL)");

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
            entity.Property(e => e.ProcessedOn).HasPrecision(0);
            entity.Property(e => e.ProcessingAt).HasPrecision(0);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C870BE59B760");

            entity.ToTable("Events", "Interaction");

            entity.HasIndex(e => new { e.AccountId, e.EventType, e.CreatedAt }, "IX_InteractionEvents_UserBehavior").IsDescending(false, false, true);

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
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
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SessionID");

            entity.HasOne(d => d.Account).WithMany(p => p.Events)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_Events_Accounts");
        });

        modelBuilder.Entity<ItemSimilarity>(entity =>
        {
            entity.HasKey(e => e.SimilarityId).HasName("PK__ItemSimi__64D0C10E6D329C4A");

            entity.ToTable("ItemSimilarities", "Recommendation");

            entity.HasIndex(e => e.SourceProductId, "IX_ItemSimilarities_Source");

            entity.Property(e => e.SimilarityId).HasColumnName("SimilarityID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SimilarProductId).HasColumnName("SimilarProductID");
            entity.Property(e => e.SimilarityScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.SourceProductId).HasColumnName("SourceProductID");

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
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C506131702FFABE8");

            entity.HasIndex(e => e.MaterialName, "UQ__Material__9C87053C96C6FDA0").IsUnique();

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MaterialName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF9DBF8053");

            entity.ToTable(tb => tb.HasTrigger("TR_Orders_CalculateTotalAmount"));

            entity.HasIndex(e => new { e.OrderDate, e.PaymentStatus }, "IX_Orders_ReportByDate");

            entity.HasIndex(e => new { e.OrderCode, e.PaymentStatus, e.StatusId }, "IX_Orders_StatusTracking");

            entity.HasIndex(e => new { e.AccountId, e.OrderDate }, "IX_Orders_UserHistory").IsDescending(false, true);

            entity.HasIndex(e => e.OrderCode, "UQ__Orders__999B522960DC57EB").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActualShippingFee).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.AssignedToStaffId).HasColumnName("AssignedToStaffID");
            entity.Property(e => e.CancelReason).HasMaxLength(500);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.CreateAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DeliveredAt).HasPrecision(0);
            entity.Property(e => e.EstimatedShippingFee).HasColumnType("decimal(10, 0)");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OrderDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BANK_TRANSFER");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.ShippedAt).HasPrecision(0);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.ShippingCity).HasMaxLength(100);
            entity.Property(e => e.ShippingDistrict).HasMaxLength(100);
            entity.Property(e => e.ShippingName).HasMaxLength(100);
            entity.Property(e => e.ShippingPhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ShippingWard).HasMaxLength(100);
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
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30CBFEEDE99");

            entity.ToTable(tb => tb.HasTrigger("TR_OrderDetails_SyncSubTotal"));

            entity.HasIndex(e => e.ProductId, "IX_OrderDetails_ProductSales");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "UQ_OrderDetails_OrderProduct").IsUnique();

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("([Quantity]*[UnitPrice]-[DiscountAmount])", true)
                .HasColumnType("decimal(19, 0)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductImage)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 0)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        modelBuilder.Entity<OrderRefund>(entity =>
        {
            entity.HasKey(e => e.RefundId).HasName("PK__OrderRef__725AB900034D7C10");

            entity.HasIndex(e => e.OrderId, "IX_OrderRefunds_Order");

            entity.Property(e => e.RefundId).HasColumnName("RefundID");
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ImgUrl)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("ImgURL");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RefundStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Requested");
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

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.OrderRefundRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("FK_OrderRefunds_RequestedBy");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.OrderRefunds)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_OrderRefunds_WalletTransactions");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderSta__4D7B4ADDA2013875");

            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreateAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

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

            entity.ToTable(tb => tb.HasTrigger("TR_OrderVouchers_SyncDiscount"));

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.DiscountAmountApplied).HasColumnType("decimal(12, 0)");

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
            entity.HasKey(e => e.OriginId).HasName("PK__Origins__171FA2C64C7A6CC4");

            entity.HasIndex(e => e.OriginName, "UQ__Origins__636F5CFD23F6A913").IsUnique();

            entity.Property(e => e.OriginId).HasColumnName("OriginID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.OriginName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.HasKey(e => e.PaymentHistoryId).HasName("PK__PaymentH__F3B93391186346A9");

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
            entity.HasKey(e => e.PriceRangeId).HasName("PK__PriceRan__B8A301FF50E1656A");

            entity.Property(e => e.PriceRangeId)
                .ValueGeneratedOnAdd()
                .HasColumnName("PriceRangeID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PriceRangeMax).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PriceRangeMin).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED8D000CAD");

            entity.ToTable(tb => tb.HasTrigger("TR_Products_SyncStatusWithQuantity"));

            entity.HasIndex(e => new { e.CategoryId, e.BrandId, e.Price, e.IsDeleted }, "IX_Products_FilterSort");

            entity.HasIndex(e => new { e.ProductStatus, e.IsDeleted }, "IX_Products_LowStock_V2").HasFilter("([Quantity]<=(10) AND [IsDeleted]=(0) AND [ProductStatus]='Active')");

            entity.HasIndex(e => new { e.ProductName, e.CategoryId, e.BrandId }, "IX_Products_Search");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastLowStockNotifiedAt).HasPrecision(0);
            entity.Property(e => e.LowStockNotificationEnabled).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PriceRangeId).HasColumnName("PriceRangeID");
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
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

            entity.HasOne(d => d.Promotion).WithMany(p => p.Products)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_Products_Promotions");
        });

        modelBuilder.Entity<ProductDetail>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__ProductD__B40CC6ED8B2376B8");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnName("ProductID");
            entity.Property(e => e.AgeId).HasColumnName("AgeID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.OriginId).HasColumnName("OriginID");
            entity.Property(e => e.SexId).HasColumnName("SexID");

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

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4EC70C37209");

            entity.HasIndex(e => e.ProductId, "IX_ProductImages_OneMainPerProduct")
                .IsUnique()
                .HasFilter("([IsMain]=(1))");

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

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42F2FE1E929A5");

            entity.ToTable(tb => tb.HasTrigger("TR_Promotions_SyncStatusWithDates"));

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Promotions_Worker");

            entity.HasIndex(e => e.PromotionCode, "UQ__Promotio__A617E4B68B3FFD27").IsUnique();

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.PromotionCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PromotionName).HasMaxLength(200);
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_Accounts");
        });

        modelBuilder.Entity<ReviewProduct>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ReviewPr__74BC79AE25B1FB94");

            entity.ToTable(tb => tb.HasTrigger("TR_ReviewProducts_ValidateProductInOrder"));

            entity.HasIndex(e => new { e.ProductId, e.IsDeleted }, "IX_ReviewProducts_Product");

            entity.HasIndex(e => new { e.AccountId, e.OrderId, e.ProductId }, "UQ_Review_Account_Order_Product").IsUnique();

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
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
            entity.HasKey(e => e.ReviewProductImageId).HasName("PK__ReviewPr__013E0F1E0E27DBDE");

            entity.Property(e => e.ReviewProductImageId).HasColumnName("ReviewProductImageID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductImages)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductImages_ReviewProducts");
        });

        modelBuilder.Entity<ReviewProductReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionProductId).HasName("PK__ReviewPr__B56FCAF190C1F86F");

            entity.HasIndex(e => new { e.AccountId, e.ReviewProductId }, "UQ_ReviewProductReactions_AccountReview").IsUnique();

            entity.Property(e => e.ReactionProductId).HasColumnName("ReactionProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReactionType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReactions_Accounts");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductReactions)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReactions_ReviewProducts");
        });

        modelBuilder.Entity<ReviewProductReply>(entity =>
        {
            entity.HasKey(e => e.ReplyProductId).HasName("PK__ReviewPr__2DCE233CEED80D41");

            entity.Property(e => e.ReplyProductId).HasColumnName("ReplyProductID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReviewProductId).HasColumnName("ReviewProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.ReviewProductReplies)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReplies_Accounts");

            entity.HasOne(d => d.ReviewProduct).WithMany(p => p.ReviewProductReplies)
                .HasForeignKey(d => d.ReviewProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReviewProductReplies_ReviewProducts");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A1442BD49");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616052A46602").IsUnique();

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
            entity.HasKey(e => e.SexId).HasName("PK__Sexes__75622DB655E93547");

            entity.HasIndex(e => e.SexName, "UQ__Sexes__BA3542903BC33368").IsUnique();

            entity.Property(e => e.SexId)
                .ValueGeneratedOnAdd()
                .HasColumnName("SexID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SexName).HasMaxLength(20);
        });

        modelBuilder.Entity<StatusOrder>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__StatusOr__C8EE20433C304E00");

            entity.HasIndex(e => e.StatusName, "UQ__StatusOr__05E7698ABA40E3B0").IsUnique();

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
            entity.HasKey(e => e.SuperCategoryId).HasName("PK__SuperCat__CEB990D3F22A7A8D");

            entity.HasIndex(e => e.SuperCategoryName, "UQ__SuperCat__3FA779DF415358E8").IsUnique();

            entity.Property(e => e.SuperCategoryId).HasColumnName("SuperCategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SuperCategoryName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__Template__F87ADD0782B38662");

            entity.ToTable("Templates", "Notification");

            entity.HasIndex(e => e.TemplateCode, "UQ__Template__0FDB5081688B87DC").IsUnique();

            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MessageTemplate).HasMaxLength(1000);
            entity.Property(e => e.TemplateCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TitleTemplate).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<UserBlockHistory>(entity =>
        {
            entity.HasKey(e => e.BlockId).HasName("PK__UserBloc__144215110F5C9550");

            entity.ToTable("UserBlockHistory");

            entity.Property(e => e.BlockId).HasColumnName("BlockID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BlockedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BlockedUntil).HasPrecision(0);
            entity.Property(e => e.ReasonId).HasColumnName("ReasonID");
            entity.Property(e => e.UnblockedAt).HasPrecision(0);
            entity.Property(e => e.UnblockedByJobId).HasColumnName("UnblockedByJobID");

            entity.HasOne(d => d.Account).WithMany(p => p.UserBlockHistoryAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBlockHistory_Account");

            entity.HasOne(d => d.BlockedByNavigation).WithMany(p => p.UserBlockHistoryBlockedByNavigations)
                .HasForeignKey(d => d.BlockedBy)
                .HasConstraintName("FK_UserBlockHistory_BlockedBy");

            entity.HasOne(d => d.Reason).WithMany(p => p.UserBlockHistories)
                .HasForeignKey(d => d.ReasonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBlockHistory_BlockReasons");

            entity.HasOne(d => d.UnblockedByNavigation).WithMany(p => p.UserBlockHistoryUnblockedByNavigations)
                .HasForeignKey(d => d.UnblockedBy)
                .HasConstraintName("FK_UserBlockHistory_UnblockedBy");

            entity.HasOne(d => d.UnblockedByJob).WithMany(p => p.UserBlockHistories)
                .HasForeignKey(d => d.UnblockedByJobId)
                .HasConstraintName("FK_UserBlockHistory_BackgroundJobs");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE79C1712142E5");

            entity.ToTable(tb => tb.HasTrigger("TR_Vouchers_SyncStatusWithDates"));

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Vouchers_Worker");

            entity.HasIndex(e => e.VoucherCode, "UQ__Vouchers__7F0ABCA97E565216").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.MaxUsagePerUser).HasDefaultValue((short)1);
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VoucherName).HasMaxLength(255);
            entity.Property(e => e.VoucherScope)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.VoucherTypeId).HasColumnName("VoucherTypeID");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Vouchers_Accounts");

            entity.HasOne(d => d.VoucherType).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.VoucherTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vouchers_VoucherTypes");
        });

        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.HasKey(e => e.VoucherTypeId).HasName("PK__VoucherT__6541283D943F6216");

            entity.HasIndex(e => e.VoucherTypeName, "UQ__VoucherT__6F01963349F34485").IsUnique();

            entity.Property(e => e.VoucherTypeId)
                .ValueGeneratedOnAdd()
                .HasColumnName("VoucherTypeID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.VoucherTypeName).HasMaxLength(100);
        });

        modelBuilder.Entity<VoucherUsageLog>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__VoucherU__29B197C00D2EF61A");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("TR_VoucherUsageLogs_DecreaseQuantity");
                    tb.HasTrigger("TR_VoucherUsageLogs_ValidateMaxUsage");
                });

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
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92E8A4B8EF4");

            entity.HasIndex(e => e.AccountId, "UQ__Wallets__349DA58795DC08F8").IsUnique();

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

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId).HasName("PK__WalletTr__7184AECF44CE2A07");

            entity.HasIndex(e => new { e.AccountId, e.CreatedAt }, "IX_WalletTransactions_Account").IsDescending(false, true);

            entity.HasIndex(e => e.RelatedOrderId, "IX_WalletTransactions_Order").HasFilter("([RelatedOrderID] IS NOT NULL)");

            entity.HasIndex(e => new { e.WalletId, e.CreatedAt }, "IX_WalletTransactions_Wallet").IsDescending(false, true);

            entity.HasIndex(e => e.IdempotencyKey, "UQ_WalletTransactions_IdempotencyKey").IsUnique();

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
            entity.Property(e => e.Method)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RelatedOrderId).HasColumnName("RelatedOrderID");
            entity.Property(e => e.RelatedPaymentHistoryId).HasColumnName("RelatedPaymentHistoryID");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TxnType)
                .HasMaxLength(10)
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

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__233189CBB4FCD8F1");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
