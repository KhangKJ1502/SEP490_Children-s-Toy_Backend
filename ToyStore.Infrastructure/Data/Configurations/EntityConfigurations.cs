using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.HasIndex(p => p.Slug).IsUnique();
        
        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.HasIndex(p => p.SKU).IsUnique();
        
        builder.Property(p => p.Price)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.SalePrice)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.CostPrice)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.Weight)
            .HasPrecision(18, 2);
            
        builder.Property(p => p.AverageRating)
            .HasPrecision(3, 2);
            
        builder.Property(p => p.Description)
            .HasMaxLength(4000);
            
        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);
            
        builder.Property(p => p.Brand)
            .HasMaxLength(128);
            
        builder.Property(p => p.ImageUrl)
            .HasMaxLength(512);
            
        builder.Property(p => p.Tags)
            .HasMaxLength(1000);
            
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.ToyCategory);
        builder.HasIndex(p => p.AgeRange);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(128);
            
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(128);
            
        builder.HasIndex(c => c.Slug).IsUnique();
        
        builder.Property(c => c.Description)
            .HasMaxLength(500);
            
        builder.Property(c => c.ImageUrl)
            .HasMaxLength(512);
            
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.HasIndex(u => u.Email).IsUnique();
        
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);
            
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);
            
        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(512);
            
        builder.Property(u => u.ShippingAddress)
            .HasMaxLength(500);
            
        builder.Property(u => u.BillingAddress)
            .HasMaxLength(500);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(32);
            
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        
        builder.Property(o => o.SubTotal)
            .HasPrecision(18, 2);
            
        builder.Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
            
        builder.Property(o => o.ShippingCost)
            .HasPrecision(18, 2);
            
        builder.Property(o => o.TaxAmount)
            .HasPrecision(18, 2);
            
        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);
            
        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(10);
            
        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(o => o.BillingAddress)
            .HasMaxLength(500);
            
        builder.Property(o => o.RecipientName)
            .IsRequired()
            .HasMaxLength(128);
            
        builder.Property(o => o.RecipientPhone)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasMaxLength(32);
            
        builder.Property(o => o.PaymentStatus)
            .IsRequired()
            .HasMaxLength(32);
            
        builder.Property(o => o.PaymentTransactionId)
            .HasMaxLength(128);
            
        builder.Property(o => o.TrackingNumber)
            .HasMaxLength(64);
            
        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.OrderDate);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        
        builder.HasKey(oi => oi.Id);
        
        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(oi => oi.ProductSKU)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(oi => oi.ProductImageUrl)
            .HasMaxLength(512);
            
        builder.Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);
            
        builder.Property(oi => oi.DiscountAmount)
            .HasPrecision(18, 2);
            
        builder.Property(oi => oi.TotalPrice)
            .HasPrecision(18, 2);
            
        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserBehaviorConfiguration : IEntityTypeConfiguration<UserBehavior>
{
    public void Configure(EntityTypeBuilder<UserBehavior> builder)
    {
        builder.ToTable("UserBehaviors");
        
        builder.HasKey(ub => ub.Id);
        
        builder.Property(ub => ub.Metadata)
            .HasMaxLength(1000);
            
        builder.Property(ub => ub.SessionId)
            .HasMaxLength(64);
            
        builder.HasOne(ub => ub.User)
            .WithMany(u => u.Behaviors)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(ub => ub.Product)
            .WithMany(p => p.UserBehaviors)
            .HasForeignKey(ub => ub.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(ub => ub.UserId);
        builder.HasIndex(ub => ub.ProductId);
        builder.HasIndex(ub => ub.BehaviorType);
        builder.HasIndex(ub => ub.OccurredAt);
    }
}

public class PaymentWebhookConfiguration : IEntityTypeConfiguration<PaymentWebhook>
{
    public void Configure(EntityTypeBuilder<PaymentWebhook> builder)
    {
        builder.ToTable("PaymentWebhooks");
        
        builder.HasKey(pw => pw.Id);
        
        builder.Property(pw => pw.Provider)
            .IsRequired()
            .HasMaxLength(32);
            
        builder.Property(pw => pw.TransactionId)
            .IsRequired()
            .HasMaxLength(128);
            
        builder.Property(pw => pw.EventType)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(pw => pw.Signature)
            .HasMaxLength(512);
            
        builder.Property(pw => pw.ProcessingResult)
            .HasMaxLength(1000);
            
        builder.Property(pw => pw.SourceIP)
            .HasMaxLength(45);
            
        builder.HasOne(pw => pw.Order)
            .WithMany()
            .HasForeignKey(pw => pw.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasIndex(pw => pw.TransactionId);
        builder.HasIndex(pw => pw.ReceivedAt);
    }
}
