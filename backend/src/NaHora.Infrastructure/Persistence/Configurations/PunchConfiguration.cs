using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Attendance;
using NaHora.Domain.Employees;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class PunchConfiguration : IEntityTypeConfiguration<Punch>
{
    public void Configure(EntityTypeBuilder<Punch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("punches");

        builder.HasKey(punch => punch.Id);

        builder.Property(punch => punch.CompanyId).IsRequired();
        builder.Property(punch => punch.EmployeeId).IsRequired();

        builder.Property(punch => punch.ClientId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(punch => punch.Nsr).IsRequired();

        builder.Property(punch => punch.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(punch => punch.Origin)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(punch => punch.Trust)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(punch => punch.OccurredAt).IsRequired();
        builder.Property(punch => punch.RecordedAt).IsRequired();
        builder.Property(punch => punch.LocalDate).IsRequired();
        builder.Property(punch => punch.CompanyOffset).IsRequired();

        // Owned rather than related: neither value object has identity, and
        // neither is ever loaded without the punch it belongs to.
        builder.ComplexProperty(punch => punch.Location, location =>
        {
            location.Property(value => value.Latitude).HasColumnName("latitude").IsRequired();
            location.Property(value => value.Longitude).HasColumnName("longitude").IsRequired();
            location.Property(value => value.AccuracyMeters).HasColumnName("accuracy_meters").IsRequired();
            location.Property(value => value.CapturedAt).HasColumnName("location_captured_at").IsRequired();
        });

        builder.ComplexProperty(punch => punch.Integrity, integrity =>
        {
            integrity.Property(value => value.DeviceId).HasColumnName("device_id").HasMaxLength(128).IsRequired();
            integrity.Property(value => value.LocationWasMocked).HasColumnName("location_was_mocked").IsRequired();
            integrity.Property(value => value.DeviceClockAt).HasColumnName("device_clock_at").IsRequired();
            integrity.Property(value => value.ClockSkewSeconds).HasColumnName("clock_skew_seconds").IsRequired();
        });

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(punch => punch.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // The NSR is the AFD's spine: unique within a company, and the database
        // enforces it rather than trusting the counter to have been read under
        // a lock every time.
        builder.HasIndex(punch => new { punch.CompanyId, punch.Nsr })
            .IsUnique()
            .HasDatabaseName("ix_punches_company_nsr");

        // What makes a retry idempotent. A second arrival of the same client
        // identifier hits this index instead of creating a duplicate punch,
        // even if two requests race past the lookup.
        builder.HasIndex(punch => new { punch.CompanyId, punch.ClientId })
            .IsUnique()
            .HasDatabaseName("ix_punches_company_client_id");

        // The query behind the daily sheet and the history screen.
        builder.HasIndex(punch => new { punch.CompanyId, punch.EmployeeId, punch.LocalDate })
            .HasDatabaseName("ix_punches_company_employee_local_date");

        // Managers filter for what needs a human. Partial, because trusted
        // punches are the overwhelming majority and indexing them here would
        // pay for rows the query never wants.
        builder.HasIndex(punch => new { punch.CompanyId, punch.Trust })
            .HasFilter("trust <> 'Trusted'")
            .HasDatabaseName("ix_punches_company_trust_pending");
    }
}
