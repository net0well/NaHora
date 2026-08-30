using NaHora.Domain.Users;

namespace NaHora.UnitTests.Sessions;

public sealed class RefreshTokenTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Expiry = Now.AddDays(14);

    [Fact]
    public void Issue_StartsAnActiveTokenWithItsOwnFamily()
    {
        // Act
        var token = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);

        // Assert
        token.IsActiveAt(Now).ShouldBeTrue();
        token.IsRevoked.ShouldBeFalse();
        token.FamilyId.ShouldNotBe(Guid.Empty);
        token.ReplacedByTokenId.ShouldBeNull();
    }

    [Fact]
    public void Rotate_RevokesTheOldTokenAndLinksToTheNewOne()
    {
        // Arrange
        var original = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);
        var later = Now.AddMinutes(10);

        // Act
        var rotated = original.Rotate("hash-2", later, later.AddDays(14));

        // Assert
        original.IsRevoked.ShouldBeTrue();
        original.RevokedAt.ShouldBe(later);
        original.ReplacedByTokenId.ShouldBe(rotated.Id);
        rotated.IsActiveAt(later).ShouldBeTrue();
    }

    [Fact]
    public void Rotate_KeepsTheFamily()
    {
        // Arrange
        var first = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);

        // Act — the chain is what lets a replayed token revoke every sibling
        var second = first.Rotate("hash-2", Now.AddMinutes(10), Expiry);
        var third = second.Rotate("hash-3", Now.AddMinutes(20), Expiry);

        // Assert
        second.FamilyId.ShouldBe(first.FamilyId);
        third.FamilyId.ShouldBe(first.FamilyId);
    }

    [Fact]
    public void Rotate_CarriesTheCompanyAndUserForward()
    {
        // Arrange
        var original = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);

        // Act
        var rotated = original.Rotate("hash-2", Now.AddMinutes(10), Expiry);

        // Assert — the token row is what the refresh flow trusts for the tenant,
        // since there is no session yet at that point.
        rotated.CompanyId.ShouldBe(CompanyId);
        rotated.UserId.ShouldBe(UserId);
    }

    [Fact]
    public void IsActiveAt_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var token = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);

        // Act & Assert
        token.IsActiveAt(Expiry.AddSeconds(1)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_IsIdempotentAndKeepsTheFirstMoment()
    {
        // Arrange
        var token = RefreshToken.Issue(CompanyId, UserId, "hash-1", Now, Expiry);
        var firstRevocation = Now.AddMinutes(5);

        // Act
        token.Revoke(firstRevocation);
        token.Revoke(Now.AddMinutes(30));

        // Assert — the audit answer is when it stopped being valid, which is
        // the first revocation, not the last call.
        token.RevokedAt.ShouldBe(firstRevocation);
    }
}
