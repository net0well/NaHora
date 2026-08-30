using NaHora.Domain.Abstractions;

namespace NaHora.UnitTests.Domain.Fakes;

internal sealed class SampleEntity : Entity
{
    public SampleEntity()
    {
    }

    public SampleEntity(Guid id) => Id = id;
}
