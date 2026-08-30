using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Companies;
using NaHora.Infrastructure.Persistence.ValueConverters;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Cnpj)
            .HasConversion<CnpjConverter>()
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(company => company.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.TradeName)
            .HasMaxLength(200);

        builder.Property(company => company.Email)
            .HasConversion<EmailAddressConverter>()
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(company => company.TimeZoneId)
            .HasMaxLength(64)
            .IsRequired();

        // Stored as text rather than an integer: a migration that reorders the
        // enum would silently reassign every existing row.
        builder.Property(company => company.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(company => company.RegistryVerified)
            .IsRequired();

        builder.Property(company => company.CreatedAt)
            .IsRequired();

        // A CNPJ identifies one legal entity, so uniqueness is system-wide and
        // not per tenant.
        builder.HasIndex(company => company.Cnpj)
            .IsUnique()
            .HasDatabaseName("ix_companies_cnpj");

        // Supports the job that expires sign-ups left unconfirmed.
        builder.HasIndex(company => company.Status)
            .HasDatabaseName("ix_companies_status");
    }
}
