using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Scheduling;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeWorkScheduleConfiguration : IEntityTypeConfiguration<EmployeeWorkSchedule>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employee_work_schedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.CompanyId).IsRequired();
        builder.Property(schedule => schedule.EmployeeId).IsRequired();
        builder.Property(schedule => schedule.EffectiveFrom).IsRequired();
        builder.Property(schedule => schedule.EffectiveTo);
        builder.Property(schedule => schedule.CreatedAt).IsRequired();

        // Answers "which version governs this day" without scanning: the
        // lookup is by employee, ordered by when the period opened.
        builder.HasIndex(schedule => new { schedule.EmployeeId, schedule.EffectiveFrom })
            .HasDatabaseName("ix_employee_work_schedules_employee_effective_from");

        builder.OwnsMany(schedule => schedule.Days, day => day.ConfigureWorkDays(
            "employee_work_schedule_days",
            "employee_work_schedule_id"));

        builder.Navigation(schedule => schedule.Days).AutoInclude();
    }
}
