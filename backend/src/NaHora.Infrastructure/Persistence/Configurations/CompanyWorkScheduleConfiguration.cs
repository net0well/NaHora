using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Scheduling;

namespace NaHora.Infrastructure.Persistence.Configurations;

internal sealed class CompanyWorkScheduleConfiguration : IEntityTypeConfiguration<CompanyWorkSchedule>
{
    public void Configure(EntityTypeBuilder<CompanyWorkSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("company_work_schedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.CompanyId).IsRequired();
        builder.Property(schedule => schedule.CreatedAt).IsRequired();
        builder.Property(schedule => schedule.UpdatedAt).IsRequired();

        // One per company. A second row would leave "what time do they open"
        // with two answers and no rule for choosing.
        builder.HasIndex(schedule => schedule.CompanyId)
            .IsUnique()
            .HasDatabaseName("ix_company_work_schedules_company");

        builder.OwnsMany(schedule => schedule.Days, day => day.ConfigureWorkDays(
            "company_work_schedule_days",
            "company_work_schedule_id"));

        builder.Navigation(schedule => schedule.Days).AutoInclude();
    }
}
