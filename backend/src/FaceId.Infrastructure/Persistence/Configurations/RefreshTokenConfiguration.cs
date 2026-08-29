using FaceId.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaceId.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.CompanyId).IsRequired();
        builder.Property(token => token.UserId).IsRequired();
        builder.Property(token => token.FamilyId).IsRequired();

        // Base64 of a SHA-256 digest is always 44 characters.
        builder.Property(token => token.TokenHash)
            .HasMaxLength(44)
            .IsRequired();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        builder.Property(token => token.CreatedByIp).HasMaxLength(45);
        builder.Property(token => token.UserAgent).HasMaxLength(256);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The lookup on every refresh. Unique because a hash collision here
        // would mean two sessions sharing one credential.
        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_hash");

        // Revoking a whole chain when reuse is detected.
        builder.HasIndex(token => token.FamilyId)
            .HasDatabaseName("ix_refresh_tokens_family");

        // Logout-all, password reset, and the job that prunes expired rows.
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt })
            .HasDatabaseName("ix_refresh_tokens_user_expiry");
    }
}
