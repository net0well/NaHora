using FaceId.Domain.Abstractions;

namespace FaceId.UnitTests.Domain.Fakes;

internal sealed class SampleEntity : Entity
{
    public SampleEntity()
    {
    }

    public SampleEntity(Guid id) => Id = id;
}
