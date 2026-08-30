using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Configuration;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Identity;

/// <summary>
/// Argon2id password hashing.
/// </summary>
/// <remarks>
/// Hashes are stored in PHC string format, so each one carries the parameters
/// it was produced with:
///
/// <code>$argon2id$v=19$m=65536,t=3,p=2$&lt;salt&gt;$&lt;hash&gt;</code>
///
/// That is what makes the cost adjustable over time. Raising memory or
/// iterations does not invalidate anything: an old hash still verifies under
/// its own parameters and is rewritten on the owner's next sign-in.
///
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class Argon2PasswordHasher(IOptions<PasswordHashingOptions> options)
    : IPasswordHasher, ISingletonService
{
    private const string Algorithm = "argon2id";
    private const int Version = 19;

    private readonly PasswordHashingOptions _options = options.Value;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(_options.SaltBytes);
        var hash = Compute(password, salt, _options.Iterations, _options.MemoryKb, _options.DegreeOfParallelism, _options.HashBytes);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"${Algorithm}$v={Version}$m={_options.MemoryKb},t={_options.Iterations},p={_options.DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || !TryParse(hash, out var parsed))
        {
            return false;
        }

        var computed = Compute(
            password,
            parsed.Salt,
            parsed.Iterations,
            parsed.MemoryKb,
            parsed.Parallelism,
            parsed.Hash.Length);

        // Constant time: returning early on the first differing byte would leak,
        // through response timing, how much of a guess was correct.
        return CryptographicOperations.FixedTimeEquals(computed, parsed.Hash);
    }

    public bool NeedsRehash(string hash)
    {
        if (!TryParse(hash, out var parsed))
        {
            // Unreadable is treated as outdated, so the next successful sign-in
            // replaces it with a hash in the current format.
            return true;
        }

        return parsed.Iterations < _options.Iterations
               || parsed.MemoryKb < _options.MemoryKb
               || parsed.Parallelism < _options.DegreeOfParallelism
               || parsed.Hash.Length < _options.HashBytes;
    }

    private static byte[] Compute(string password, byte[] salt, int iterations, int memoryKb, int parallelism, int outputBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            Iterations = iterations,
            MemorySize = memoryKb,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(outputBytes);
    }

    private static bool TryParse(string? encoded, out ParsedHash parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        // $argon2id$v=19$m=65536,t=3,p=2$<salt>$<hash>
        var segments = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5 || !string.Equals(segments[0], Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        var costs = segments[2].Split(',');
        if (costs.Length != 3)
        {
            return false;
        }

        if (!TryReadCost(costs[0], "m=", out var memoryKb)
            || !TryReadCost(costs[1], "t=", out var iterations)
            || !TryReadCost(costs[2], "p=", out var parallelism))
        {
            return false;
        }

        try
        {
            parsed = new ParsedHash(
                memoryKb,
                iterations,
                parallelism,
                Convert.FromBase64String(segments[3]),
                Convert.FromBase64String(segments[4]));

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadCost(string segment, string prefix, out int value)
    {
        value = 0;

        return segment.StartsWith(prefix, StringComparison.Ordinal)
               && int.TryParse(segment.AsSpan(prefix.Length), CultureInfo.InvariantCulture, out value);
    }

    private readonly record struct ParsedHash(
        int MemoryKb,
        int Iterations,
        int Parallelism,
        byte[] Salt,
        byte[] Hash);
}
