namespace FaceId.Features.System.GetSystemInfo;

/// <summary>
/// Reference slice proving the endpoint to query to handler path end to end.
/// </summary>
/// <remarks>
/// Wolverine discovers handlers by type name, so the suffix is load-bearing.
/// Dependencies arrive as method parameters rather than through a constructor.
/// </remarks>
public static class GetSystemInfoHandler
{
    public static GetSystemInfoResponse Handle(GetSystemInfoQuery query, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var version = typeof(GetSystemInfoHandler).Assembly.GetName().Version?.ToString() ?? "unknown";
        var environment = global::System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        return new GetSystemInfoResponse("FaceId", version, environment, clock.GetUtcNow());
    }
}
