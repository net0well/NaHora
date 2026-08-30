namespace NaHora.Api.Development;

/// <summary>
/// Whether to plant a company, a manager and an employee at startup.
/// </summary>
/// <remarks>
/// Off unless switched on. Two locks guard it — this flag and the Development
/// environment — because what it creates is an account with a password written
/// in the repository, and one lock is one mistake away from production.
/// </remarks>
public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";

    public bool Enabled { get; set; }

    /// <summary>
    /// The password given to both seeded accounts.
    /// </summary>
    /// <remarks>
    /// Configurable so a developer can change it locally without touching code,
    /// and so nothing here has to know a real one.
    /// </remarks>
    public string Password { get; set; } = "NaHora@2026";
}
