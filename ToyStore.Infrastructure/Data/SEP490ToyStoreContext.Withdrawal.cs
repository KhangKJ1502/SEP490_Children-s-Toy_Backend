using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class SEP490ToyStoreContext
{
    public virtual DbSet<SavedBankAccount> SavedBankAccounts { get; set; }

    public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    public virtual DbSet<WithdrawalStatusHistory> WithdrawalStatusHistories { get; set; }

    private static void ConfigureWithdrawalFlow(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedBankAccount>(entity =>
        {
            entity.HasKey(e => e.SavedBankAccountId).HasName("PK_SavedBankAccounts");

            entity.ToTable("SavedBankAccounts");

            entity.HasIndex(e => new { e.AccountId, e.BankBin, e.AccountNumber }, "UQ_SavedBankAccounts_AccountBin").IsUnique();

            entity.HasIndex(e => e.AccountId, "UQ_SavedBankAccounts_OneDefault")
                .IsUnique()
                .HasFilter("([IsDefault]=(1) AND [IsDeleted]=(0))");

            entity.Property(e => e.SavedBankAccountId).HasColumnName("SavedBankAccountID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.BankBin)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BankShortName)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BankCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccountName).HasMaxLength(200);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LastUsedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Account).WithMany(p => p.SavedBankAccounts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SavedBankAccounts_Accounts");
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.HasKey(e => e.WithdrawalId).HasName("PK_WithdrawalRequests");

            entity.ToTable("WithdrawalRequests");

            entity.HasIndex(e => e.ReferenceId, "UQ_WithdrawalRequests_ReferenceId").IsUnique();

            entity.HasIndex(e => new { e.AccountId, e.CreatedAt }, "IX_WithdrawalRequests_Account").IsDescending(false, true);

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_WithdrawalRequests_Pending")
                .HasFilter("([Status]='PENDING' OR [Status]='PROCESSING')");

            entity.HasIndex(e => e.PayosPayoutId, "IX_WithdrawalRequests_PayosPayoutId")
                .HasFilter("([PayosPayoutId] IS NOT NULL)");

            entity.Property(e => e.WithdrawalId).HasColumnName("WithdrawalID");
            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.WalletTransactionId).HasColumnName("WalletTransactionID");
            entity.Property(e => e.ReferenceId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.ToBankBin)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ToBankName).HasMaxLength(100);
            entity.Property(e => e.ToAccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ToAccountName).HasMaxLength(200);
            entity.Property(e => e.PayosPayoutId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PayosTransactionId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FailReason).HasMaxLength(500);
            entity.Property(e => e.RetryCount).HasDefaultValue((byte)0);
            entity.Property(e => e.ProcessingAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Account).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WithdrawalRequests_Accounts");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WithdrawalRequests_Wallets");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_WithdrawalRequests_WalletTransactions");
        });

        modelBuilder.Entity<WithdrawalStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK_WithdrawalStatusHistory");

            entity.ToTable("WithdrawalStatusHistory");

            entity.HasIndex(e => new { e.WithdrawalId, e.CreatedAt }, "IX_WithdrawalStatusHistory_Withdrawal");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.WithdrawalId).HasColumnName("WithdrawalID");
            entity.Property(e => e.FromStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ToStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Source)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Withdrawal).WithMany(p => p.WithdrawalStatusHistories)
                .HasForeignKey(d => d.WithdrawalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WithdrawalStatusHistory_WithdrawalRequests");
        });
    }
}
