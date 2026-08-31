using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Scheduling;

namespace NaHora.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the seven days a schedule owns.
/// </summary>
/// <remarks>
/// One definition shared by the company default and the per-employee override,
/// so the two cannot drift into different column types for the same value.
///
/// A table of its own rather than fourteen columns on the owner: a week is a
/// collection, and flattening it would make "give me Monday" a choice between
/// seven column names.
/// </remarks>
internal static class WorkDayScheduleConfiguration
{
    public static void ConfigureWorkDays<TOwner>(
        this OwnedNavigationBuilder<TOwner, WorkDaySchedule> builder,
        string tableName,
        string foreignKeyColumn)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tableName);

        builder.WithOwner().HasForeignKey(foreignKeyColumn);

        // The day is the identity within the week, so the key is the owner plus
        // the weekday. Nothing else distinguishes two rows here.
        builder.Property(day => day.Day)
            .HasConversion<string>()
            .HasColumnName("day_of_week")
            .HasMaxLength(16)
            .IsRequired();

        builder.HasKey(foreignKeyColumn, nameof(WorkDaySchedule.Day));

        builder.Property(day => day.StartTime).HasColumnName("start_time");
        builder.Property(day => day.LunchStartTime).HasColumnName("lunch_start_time");
        builder.Property(day => day.LunchEndTime).HasColumnName("lunch_end_time");
        builder.Property(day => day.EndTime).HasColumnName("end_time");
    }
}
