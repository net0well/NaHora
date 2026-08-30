using Microsoft.Extensions.DependencyInjection;
using NaHora.Shared.DependencyInjection;

namespace NaHora.UnitTests.DependencyInjection.Fakes;

/// <summary>
/// Carries both an explicit attribute and a marker interface, so the scanner's
/// precedence rule has something to prove.
/// </summary>
[Service(ServiceLifetime.Transient)]
internal sealed class SampleOverriddenService : ISampleOverriddenService, IScopedService;
