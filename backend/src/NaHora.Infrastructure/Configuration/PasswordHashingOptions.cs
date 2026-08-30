using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

/// <summary>
/// Argon2id cost parameters.
/// </summary>
/// <remarks>
/// The defaults follow current OWASP guidance. Memory is the parameter that
/// matters most: it is what denies an attacker the massive parallelism a GPU or
/// an ASIC would otherwise bring, and it is exactly what PBKDF2 and BCrypt
/// lack.
///
/// Raising a value later costs nothing: the parameters are embedded in every
/// stored hash, so old hashes keep verifying and are upgraded on the owner's
/// next successful sign-in.
/// </remarks>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    [Range(1, 10)]
    public int Iterations { get; init; } = 3;

    [Range(8 * 1024, 1024 * 1024)]
    public int MemoryKb { get; init; } = 64 * 1024;

    [Range(1, 16)]
    public int DegreeOfParallelism { get; init; } = 2;

    [Range(16, 64)]
    public int SaltBytes { get; init; } = 16;

    [Range(16, 64)]
    public int HashBytes { get; init; } = 32;
}
