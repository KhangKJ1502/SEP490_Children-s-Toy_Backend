using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext
{
    public virtual DbSet<AiPromptTemplate> AiPromptTemplates { get; set; }

    public virtual DbSet<AiBlogQueue> AiBlogQueues { get; set; }

    public virtual DbSet<AiBlogGenerationHistory> AiBlogGenerationHistories { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<AiBlogGenerationHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);

            entity.ToTable("AIBlogGenerationHistory");

            entity.HasIndex(e => new { e.BlogPostId, e.RequestedAt }, "IX_AIBlogGenerationHistory_BlogPost_RequestedAt");
            entity.HasIndex(e => new { e.Status, e.RequestedAt }, "IX_AIBlogGenerationHistory_Status_RequestedAt");
            entity.HasIndex(e => new { e.StaffId, e.RequestedAt }, "IX_AIBlogGenerationHistory_Staff_RequestedAt");
            entity.HasIndex(e => e.CorrelationId, "IX_AIBlogGenerationHistory_CorrelationId").HasFilter("([CorrelationId] IS NOT NULL)");
            entity.HasIndex(e => e.IdempotencyKey, "UQ_AIBlogGenerationHistory_IdempotencyKey")
                .IsUnique()
                .HasFilter("([IdempotencyKey] IS NOT NULL)");
            entity.HasIndex(e => new { e.BlogPostId, e.IsAppliedToBlog, e.AppliedAt }, "IX_AIBlogGenerationHistory_Applied");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.BlogPostId).HasColumnName("BlogPostID");
            entity.Property(e => e.QueueId).HasColumnName("QueueID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.PromptData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Temperature).HasColumnType("decimal(4, 2)");
            entity.Property(e => e.Language).HasMaxLength(20);
            entity.Property(e => e.Tone).HasMaxLength(50);
            entity.Property(e => e.GeneratedContent).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.CorrelationId)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.IdempotencyKey)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsAppliedToBlog).HasDefaultValue(false);
            entity.Property(e => e.AppliedAt).HasPrecision(0);
            entity.Property(e => e.RequestedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProcessedAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.BlogPost).WithMany()
                .HasForeignKey(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIBlogGenerationHistory_BlogPosts");

            entity.HasOne(d => d.Queue).WithMany(p => p.AiBlogGenerationHistories)
                .HasForeignKey(d => d.QueueId)
                .HasConstraintName("FK_AIBlogGenerationHistory_AIBlogQueue");

            entity.HasOne(d => d.Staff).WithMany()
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIBlogGenerationHistory_Staff");

            entity.HasOne(d => d.Template).WithMany(p => p.AiBlogGenerationHistories)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_AIBlogGenerationHistory_Template");
        });
    }
}
