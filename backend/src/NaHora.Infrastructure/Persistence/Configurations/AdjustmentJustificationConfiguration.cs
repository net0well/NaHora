using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.Adjustments;

namespace NaHora.Infrastructure.Persistence.Configurations;

/// <remarks>
/// The rows are seeded through <c>HasData</c>, so the catalogue arrives with the
/// migration rather than through startup code. A fresh database has it before
/// the first request, and a change to it is reviewed as a schema change — which
/// is what a fixed catalogue referenced by every adjustment ought to be.
/// </remarks>
internal sealed class AdjustmentJustificationConfiguration : IEntityTypeConfiguration<AdjustmentJustification>
{
    public void Configure(EntityTypeBuilder<AdjustmentJustification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("adjustment_justifications");

        builder.HasKey(justification => justification.Id);

        builder.Property(justification => justification.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(justification => justification.Description)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(justification => justification.RequiresNotes).IsRequired();

        builder.Property(justification => justification.DisplayOrder).IsRequired();

        // The code is the contract the app translates on, so two rows sharing
        // one would make a translation ambiguous.
        builder.HasIndex(justification => justification.Code).IsUnique();

        builder.HasData(AdjustmentJustificationCatalogue.All);
    }
}
