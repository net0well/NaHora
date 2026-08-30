using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;
using NaHora.Domain.Employees;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class PunchAdjustmentConfiguration : IEntityTypeConfiguration<PunchAdjustment>
{
    public void Configure(EntityTypeBuilder<PunchAdjustment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("punch_adjustments");

        builder.HasKey(adjustment => adjustment.Id);

        builder.Property(adjustment => adjustment.CompanyId).IsRequired();
        builder.Property(adjustment => adjustment.EmployeeId).IsRequired();

        // Null on an inclusion: there is no punch to point at.
        builder.Property(adjustment => adjustment.PunchId);

        // Stored as text rather than an integer: a migration that reorders the
        // enum would silently reassign every existing row.
        builder.Property(adjustment => adjustment.Kind)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(adjustment => adjustment.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(adjustment => adjustment.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(adjustment => adjustment.OriginalOccurredAt);
        builder.Property(adjustment => adjustment.AdjustedOccurredAt).IsRequired();

        builder.Property(adjustment => adjustment.JustificationId).IsRequired();
        builder.Property(adjustment => adjustment.Notes).HasMaxLength(1000);
        builder.Property(adjustment => adjustment.DecisionNotes).HasMaxLength(1000);

        builder.Property(adjustment => adjustment.AttachmentKey).HasMaxLength(300);

        builder.Property(adjustment => adjustment.RequestedByUserId).IsRequired();
        builder.Property(adjustment => adjustment.RequestedAt).IsRequired();
        builder.Property(adjustment => adjustment.DecidedByUserId);
        builder.Property(adjustment => adjustment.DecidedAt);

        // Restrict, not cascade: a punch cannot be deleted at all, and a rule
        // that quietly removes its treatments would be describing a deletion
        // this product does not perform.
        builder.HasOne<Punch>()
            .WithMany()
            .HasForeignKey(adjustment => adjustment.PunchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(adjustment => adjustment.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AdjustmentJustification>()
            .WithMany()
            .HasForeignKey(adjustment => adjustment.JustificationId)
            .OnDelete(DeleteBehavior.Restrict);

        // The manager's queue: everything still waiting, per company.
        builder.HasIndex(adjustment => new { adjustment.CompanyId, adjustment.Status })
            .HasDatabaseName("ix_punch_adjustments_company_status");

        // The employee's own history, and the lookup that decides which
        // treatment governs a punch.
        builder.HasIndex(adjustment => new { adjustment.EmployeeId, adjustment.RequestedAt })
            .HasDatabaseName("ix_punch_adjustments_employee_requested_at");
    }
}
