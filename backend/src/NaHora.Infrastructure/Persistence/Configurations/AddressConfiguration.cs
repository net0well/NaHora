using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaHora.Domain.ValueObjects;
using NaHora.Infrastructure.Persistence.ValueConverters;

namespace NaHora.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the shared <see cref="Address"/> value object into the owning table.
/// </summary>
/// <remarks>
/// One definition used by both the company and the employee. Two copies would
/// eventually differ in a column length, and the difference would only show up
/// when a real address was one character too long for one of them.
/// </remarks>
internal static class AddressConfiguration
{
    public static void ConfigureAddress<TOwner>(this OwnedNavigationBuilder<TOwner, Address> builder)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The owned rows share the owner's table, so the link back to it is the
        // owner's own primary key. Left to convention the shadow property would
        // be "CompanyId", which the snake-case rule turns into a "company_id"
        // column that does not exist on a table keyed by "id".
        builder.WithOwner().HasForeignKey("Id");

        builder.Property(address => address.PostalCode)
            .HasConversion<PostalCodeConverter>()
            .HasColumnName("postal_code")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(address => address.Street)
            .HasColumnName("street")
            .HasMaxLength(200)
            .IsRequired();

        // Text, not a number: "S/N", "1000-A" and "Km 12" are all real.
        builder.Property(address => address.Number)
            .HasColumnName("number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(address => address.Complement)
            .HasColumnName("complement")
            .HasMaxLength(100);

        builder.Property(address => address.District)
            .HasColumnName("district")
            .HasMaxLength(100);

        builder.Property(address => address.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(address => address.State)
            .HasColumnName("state")
            .HasMaxLength(2)
            .IsRequired();
    }
}
