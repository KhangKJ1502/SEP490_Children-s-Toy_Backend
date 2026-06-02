using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext
{
    public virtual DbSet<AiPromptTemplate> AiPromptTemplates { get; set; }

    public virtual DbSet<AiBlogQueue> AiBlogQueues { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>().Ignore(e => e.IsSelected);

        modelBuilder.Entity<AiPromptTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.HasIndex(e => e.TemplateName).IsUnique();

            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.TemplateName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PromptStructure).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DefaultTone).HasMaxLength(50);
            entity.Property(e => e.DefaultCategoryId).HasColumnName("DefaultCategoryID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.DefaultCategory).WithMany()
                .HasForeignKey(d => d.DefaultCategoryId)
                .HasConstraintName("FK_AIPromptTemplates_BlogCategories");
        });

        modelBuilder.Entity<AiBlogQueue>(entity =>
        {
            entity.HasKey(e => e.QueueId);

            entity.ToTable("AIBlogQueue", tb => tb.HasTrigger("trg_AIBlogQueue_UpdateTime"));

            entity.HasIndex(e => new { e.Status, e.Priority, e.RequestedAt }, "IX_AIBlogQueue_Status");

            entity.HasIndex(e => new { e.BlogPostId, e.RequestedAt, e.QueueId }, "IX_AIBlogQueue_BlogPost_RequestedAt");

            entity.Property(e => e.QueueId).HasColumnName("QueueID");
            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.PromptData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.GeneratedContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.RequestedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProcessedAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.BlogPost).WithMany()
                .HasForeignKey(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIBlogQueue_BlogPosts");

            entity.HasOne(d => d.Staff).WithMany()
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIBlogQueue_Staff");

            entity.HasOne(d => d.Template).WithMany(p => p.AiBlogQueues)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_AIBlogQueue_Template");
        });
    }
}
