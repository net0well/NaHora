using System.Security.Cryptography;

namespace NaHora.Domain.Users;

/// <summary>
/// The password a newly hired employee receives.
/// </summary>
/// <remarks>
/// <para>
/// Generated rather than chosen by whoever does the hiring. A manager who picks
/// the password knows the credential of the person whose hours they audit, and
/// that is the one relationship in this product where that matters most.
/// </para>
/// <para>
/// It is emailed to the employee and never returned to the caller, so it exists
/// in memory for exactly as long as it takes to hash and to send.
/// </para>
/// </remarks>
public static class TemporaryPassword
{
    /// <summary>
    /// Longer than the policy floor, because nobody has to remember this one —
    /// it is replaced on first sign-in.
    /// </summary>
    private const int Length = 14;

    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";

    /// <summary>
    /// No characters that argue over a phone call or in a handwritten note.
    /// </summary>
    private const string Symbols = "!@#$%*-_+=?";

    /// <summary>
    /// A password that satisfies <see cref="PasswordPolicy"/> by construction.
    /// </summary>
    /// <remarks>
    /// One character from each class first, so the result cannot fail the
    /// class requirement by chance, then the rest at random and the whole thing
    /// shuffled — otherwise the first four positions would always be lower,
    /// upper, digit, symbol, and that is a pattern worth not publishing.
    ///
    /// Ambiguous glyphs are left out on purpose: l, I, 1, O and 0 turn a
    /// password read aloud into three attempts and a reset.
    /// </remarks>
    public static string Generate()
    {
        const string all = Lowercase + Uppercase + Digits + Symbols;

        var characters = new List<char>(Length)
        {
            Pick(Lowercase),
            Pick(Uppercase),
            Pick(Digits),
            Pick(Symbols)
        };

        while (characters.Count < Length)
        {
            characters.Add(Pick(all));
        }

        return new string([.. Shuffle(characters)]);
    }

    private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

    /// <summary>Fisher-Yates, drawing from the cryptographic generator.</summary>
    private static List<char> Shuffle(List<char> characters)
    {
        for (var index = characters.Count - 1; index > 0; index--)
        {
            var swap = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swap]) = (characters[swap], characters[index]);
        }

        return characters;
    }
}
