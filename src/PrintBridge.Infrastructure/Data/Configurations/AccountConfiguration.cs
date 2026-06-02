using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Enums;

namespace PrintBridge.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("NVARCHAR(50)");

        builder.Property(a => a.PasswordHash)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("NVARCHAR(255)");

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR(100)");

        builder.Property(a => a.FullName)
            .HasMaxLength(200)
            .HasColumnType("NVARCHAR(200)")
            .IsRequired(false);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.UpdatedAt)
            .IsRequired(false);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Store enum as int in DB
        builder.Property(a => a.Role)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AccountRole.User);

        builder.HasIndex(a => a.Username)
            .IsUnique()
            .HasDatabaseName("IX_Account_Username");

        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("IX_Account_Email");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Account_IsActive");

        builder.HasIndex(a => a.IsDeleted)
            .HasDatabaseName("IX_Account_IsDeleted");


    }
}