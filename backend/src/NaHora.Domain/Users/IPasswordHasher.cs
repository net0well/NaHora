namespace NaHora.Domain.Users;

/// <summary>
/// Hashes and verifies passwords.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <remarks>
    /// Comparison is constant time. Returning early on the first differing byte
    /// would leak, through response timing, how much of a guess was correct.
    /// </remarks>
    bool Verify(string password, string hash);

    /// <summary>
    /// Whether the stored hash was produced with weaker parameters than the
    /// current ones, so it can be upgraded silently after a successful sign-in.
    /// </summary>
    bool NeedsRehash(string hash);
}
