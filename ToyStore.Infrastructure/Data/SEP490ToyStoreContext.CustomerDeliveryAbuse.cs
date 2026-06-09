using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext
{
    public virtual DbSet<CustomerDeliveryAbuseCase> CustomerDeliveryAbuseCases { get; set; }

    private static void ConfigureCustomerDeliveryAbuseCases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerDeliveryAbuseCase>(entity =>
        {
            entity.HasKey(e => e.CaseId);
            entity.ToTable("CustomerDeliveryAbuseCases");

            entity.HasIndex(e => e.AccountId, "UQ_CustomerDeliveryAbuseCases_Account").IsUnique();
            entity.HasIndex(e => new { e.Status, e.StrictPeriodUntil }, "IX_CustomerDeliveryAbuseCases_Status_StrictUntil");

            entity.Property(e => e.CaseId).HasColumnName("CaseID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Status).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.LastGHNFailCode).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.AppealDecision).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.PermanentBlockReason).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.Account)
                .WithOne()
                .HasForeignKey<CustomerDeliveryAbuseCase>(e => e.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerDeliveryAbuseCases_Accounts");
        });
    }
}
