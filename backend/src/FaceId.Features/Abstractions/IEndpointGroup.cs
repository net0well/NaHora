using Microsoft.AspNetCore.Routing;

namespace FaceId.Features.Abstractions;

/// <summary>
/// A feature slice's HTTP surface. Implementations are discovered by assembly
/// scanning, so a new slice becomes routable without editing a central map file.
/// </summary>
public interface IEndpointGroup
{
    void Map(IEndpointRouteBuilder endpoints);
}
