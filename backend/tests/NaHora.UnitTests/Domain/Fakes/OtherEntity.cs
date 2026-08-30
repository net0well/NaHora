using NaHora.Domain.Abstractions;

namespace NaHora.UnitTests.Domain.Fakes;

internal sealed class OtherEntity : Entity
{
    public OtherEntity(Guid id) => Id = id;
}
