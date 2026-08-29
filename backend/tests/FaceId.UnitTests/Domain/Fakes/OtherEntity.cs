using FaceId.Domain.Abstractions;

namespace FaceId.UnitTests.Domain.Fakes;

internal sealed class OtherEntity : Entity
{
    public OtherEntity(Guid id) => Id = id;
}
