using FaceId.Infrastructure.Configuration;
using FaceId.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace FaceId.UnitTests.Identity;

public sealed class Argon2PasswordHasherTests
{
    private static Argon2PasswordHasher CreateHasher(PasswordHashingOptions? options = null) =>
        new(Options.Create(options ?? new PasswordHashingOptions
        {
            // Lowered so the suite stays fast. Production cost comes from
            // configuration, which is the point of it being configurable.
            Iterations = 1,
            MemoryKb = 8 * 1024,
            DegreeOfParallelism = 1
        }));

    [Fact]
    public void Verify_WhenPasswordIsCorrect_ReturnsTrue()
    {
        // Arrange
        var hasher = CreateHasher();
        var hash = hasher.Hash("Correct-Horse-9");

        // Act & Assert
        hasher.Verify("Correct-Horse-9", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WhenPasswordIsWrong_ReturnsFalse()
    {
        // Arrange
        var hasher = CreateHasher();
        var hash = hasher.Hash("Correct-Horse-9");

        // Act & Assert
        hasher.Verify("Correct-Horse-8", hash).ShouldBeFalse();
    }

    [Fact]
    public void Hash_ForTheSamePassword_ProducesDifferentHashes()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act — a fresh salt each time, so two people with the same password
        // do not share a hash and a rainbow table has nothing to match.
        var first = hasher.Hash("Correct-Horse-9");
        var second = hasher.Hash("Correct-Horse-9");

        // Assert
        first.ShouldNotBe(second);
        hasher.Verify("Correct-Horse-9", first).ShouldBeTrue();
        hasher.Verify("Correct-Horse-9", second).ShouldBeTrue();
    }

    [Fact]
    public void Hash_EmbedsItsOwnParameters()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var hash = hasher.Hash("Correct-Horse-9");

        // Assert — PHC format, which is what lets an old hash keep verifying
        // after the cost is raised.
        hash.ShouldStartWith("$argon2id$v=19$m=8192,t=1,p=1$");
        hash.Split('$', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(5);
    }

    [Fact]
    public void NeedsRehash_WhenStoredCostIsLower_ReturnsTrue()
    {
        // Arrange
        var weak = CreateHasher();
        var hash = weak.Hash("Correct-Horse-9");

        var stronger = CreateHasher(new PasswordHashingOptions
        {
            Iterations = 3,
            MemoryKb = 16 * 1024,
            DegreeOfParallelism = 1
        });

        // Act & Assert
        stronger.NeedsRehash(hash).ShouldBeTrue();
    }

    [Fact]
    public void NeedsRehash_WhenCostMatches_ReturnsFalse()
    {
        // Arrange
        var hasher = CreateHasher();
        var hash = hasher.Hash("Correct-Horse-9");

        // Act & Assert
        hasher.NeedsRehash(hash).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$argon2id$v=19$broken")]
    public void Verify_WhenStoredHashIsUnreadable_ReturnsFalseInsteadOfThrowing(string stored)
    {
        // Arrange
        var hasher = CreateHasher();

        // Act & Assert — a corrupt row must fail the sign-in, not the request
        hasher.Verify("Correct-Horse-9", stored).ShouldBeFalse();
        hasher.NeedsRehash(stored).ShouldBeTrue();
    }
}
