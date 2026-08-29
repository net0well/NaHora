using System.ComponentModel.DataAnnotations;

namespace FaceId.Api.RateLimiting;

/// <summary>
/// Request limits per surface.
/// </summary>
/// <remarks>
/// Configurable and deliberately generous to start. A badly calibrated limit is
/// a denial of service we inflict on ourselves, and it is far easier to tighten
/// a limit after watching real traffic than to explain to a customer why their
/// office cannot sign in.
/// </remarks>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(10, 10_000)]
    public int GlobalPerMinute { get; init; } = 300;

    [Range(1, 100)]
    public int LoginPerMinute { get; init; } = 10;

    [Range(1, 100)]
    public int CompanyRegistrationPerHour { get; init; } = 3;

    [Range(1, 100)]
    public int CnpjLookupPerMinute { get; init; } = 10;

    [Range(1, 100)]
    public int ExportPerHour { get; init; } = 10;
}
