using NaHora.Domain.ValueObjects;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// A valid address for seeds that need one but do not care what it says.
/// </summary>
internal static class AddressFactory
{
    public static Address New() => Address.Create(
        "01310100",
        "Avenida Paulista",
        "1000",
        null,
        "Bela Vista",
        "São Paulo",
        "SP").Value;
}
