using FaceId.Domain.Abstractions;

namespace FaceId.UnitTests.Domain;

public sealed class EntityTests
{
    [Fact]
    public void Id_WhenEntityIsCreated_UsesUuidVersion7()
    {
        // Arrange & Act
        var entity = new SampleEntity();

        // Assert — the version nibble sits in byte 6 of the big-endian layout.
        var version = (entity.Id.ToByteArray(bigEndian: true)[6] & 0xF0) >> 4;
        version.ShouldBe(7, "a time-ordered identifier keeps index locality on insert");
    }

    [Fact]
    public void Id_WhenCreatedAtDifferentTimes_SortsByCreationTime()
    {
        // Arrange
        var earlier = Guid.CreateVersion7(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var later = Guid.CreateVersion7(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        // Act — the 48-bit timestamp occupies the leading bytes of the
        // big-endian layout. Guid.CompareTo compares .NET field order, which is
        // not the same thing, so the bytes are compared directly.
        var comparison = CompareBigEndian(earlier, later);

        // Assert
        comparison.ShouldBeLessThan(0);
    }

    [Fact]
    public void Equals_WhenSameTypeAndId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = new SampleEntity(id);
        var second = new SampleEntity(id);

        // Act & Assert
        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_WhenDifferentType_ReturnsFalse()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var sample = new SampleEntity(id);
        var other = new OtherEntity(id);

        // Act & Assert — the same key in two different tables is not the same thing
        sample.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WhenOtherIsNull_ReturnsFalse()
    {
        // Arrange
        var entity = new SampleEntity();

        // Act & Assert
        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
    }

    private static int CompareBigEndian(Guid left, Guid right)
    {
        var leftBytes = left.ToByteArray(bigEndian: true);
        var rightBytes = right.ToByteArray(bigEndian: true);

        return leftBytes.AsSpan().SequenceCompareTo(rightBytes.AsSpan());
    }

    private sealed class SampleEntity : Entity
    {
        public SampleEntity()
        {
        }

        public SampleEntity(Guid id) => Id = id;
    }

    private sealed class OtherEntity : Entity
    {
        public OtherEntity(Guid id) => Id = id;
    }
}
