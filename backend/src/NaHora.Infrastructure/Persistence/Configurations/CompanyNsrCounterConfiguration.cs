using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Attendance;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class CompanyNsrCounterConfiguration : IEntityTypeConfiguration<CompanyNsrCounter>
{
    public void Configure(EntityTypeBuilder<CompanyNsrCounter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("nsr_counters");

        builder.HasKey(counter => counter.Id);

        builder.Property(counter => counter.CompanyId).IsRequired();
        builder.Property(counter => counter.LastNsr).IsRequired();

        // One counter per company, enforced here rather than assumed. Two rows
        // for the same company would let two punches take the same number and
        // put a duplicate NSR in the AFD.
        builder.HasIndex(counter => counter.CompanyId)
            .IsUnique()
            .HasDatabaseName("ix_nsr_counters_company");
    }
}
