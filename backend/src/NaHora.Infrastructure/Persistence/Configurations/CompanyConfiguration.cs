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

        // Owned rather than a separate table: an address has no identity of
        // its own and is never queried without the company it belongs to.
        builder.OwnsOne(company => company.Address, address => address.ConfigureAddress());

        builder.Navigation(company => company.Address).IsRequired();

        builder.Property(company => company.Phone)
            .HasConversion<NullablePhoneNumberConverter>()
            .HasMaxLength(11);

        // Free text: the format differs by state, and "ISENTO" is a legitimate
        // entry in several of them.
        builder.Property(company => company.StateRegistration)
            .HasMaxLength(32);

        builder.Property(company => company.MunicipalRegistration)
            .HasMaxLength(32);

        builder.Property(company => company.FoundedOn);

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
