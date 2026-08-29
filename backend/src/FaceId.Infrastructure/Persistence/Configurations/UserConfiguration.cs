using FaceId.Domain.Companies;
using FaceId.Domain.Users;
using FaceId.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaceId.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.CompanyId)
            .IsRequired();

        builder.Property(user => user.Cpf)
            .HasConversion<CpfConverter>()
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasConversion<EmailAddressConverter>()
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.MustChangePassword).IsRequired();
        builder.Property(user => user.FailedLoginAttempts).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(user => user.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // The same person may work for two companies on the same instance, so
        // the credential is unique per tenant rather than globally.
        builder.HasIndex(user => new { user.CompanyId, user.Cpf })
            .IsUnique()
            .HasDatabaseName("ix_users_company_cpf");

        builder.HasIndex(user => new { user.CompanyId, user.Email })
            .IsUnique()
            .HasDatabaseName("ix_users_company_email");
    }
}
