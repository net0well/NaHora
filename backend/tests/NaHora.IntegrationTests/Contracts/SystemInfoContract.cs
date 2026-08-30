namespace NaHora.IntegrationTests.Contracts;

/// <summary>
/// Mirrors the API response shape. Declared here rather than reusing the
/// feature type, so a breaking change to the contract fails a test instead of
/// silently compiling.
/// </summary>
internal sealed record SystemInfoContract(
    string Application,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
