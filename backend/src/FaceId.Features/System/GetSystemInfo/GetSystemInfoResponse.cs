namespace FaceId.Features.System.GetSystemInfo;

/// <summary>Build and runtime information, for diagnostics.</summary>
public sealed record GetSystemInfoResponse(
    string Application,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
