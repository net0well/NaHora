using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

public sealed class RegistryOptions
{
    public const string SectionName = "BrasilApi";

    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; init; } = "https://brasilapi.com.br/api/";

    /// <summary>Per attempt, not for the whole pipeline.</summary>
    [Range(1, 30)]
    public int TimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Total budget across every attempt. Must exceed the per-attempt timeout
    /// multiplied by the attempts, or the retry is silently cancelled before it
    /// can happen.
    /// </summary>
    [Range(1, 120)]
    public int TotalTimeoutSeconds { get; init; } = 12;

    [Range(0, 5)]
    public int RetryAttempts { get; init; } = 2;
}
