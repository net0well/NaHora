namespace NaHora.Api.Development;

/// <summary>
/// Source-generated log messages for the development seed.
/// </summary>
/// <remarks>
/// The CPFs are logged on purpose. They belong to the well-known Brazilian test
/// numbers, not to a person, and a developer who cannot find the credentials
/// cannot use the seed at all. Nothing here runs outside Development.
/// </remarks>
internal static partial class DevelopmentSeedLog
{
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Information,
        Message = "Development seed applied. Company {CompanyId}. "
                  + "Sign in with manager CPF {ManagerCpf} or employee CPF {EmployeeCpf}.")]
    public static partial void Seeded(ILogger logger, Guid companyId, string managerCpf, string employeeCpf);

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Debug,
        Message = "Development seed skipped: company {CompanyId} is already present.")]
    public static partial void AlreadyPresent(ILogger logger, Guid companyId);

    [LoggerMessage(
        EventId = 2103,
        Level = LogLevel.Warning,
        Message = "DevelopmentSeed:Enabled is set outside development and was ignored.")]
    public static partial void IgnoredOutsideDevelopment(ILogger logger);

    [LoggerMessage(
        EventId = 2104,
        Level = LogLevel.Error,
        Message = "Development seed could not be built and was skipped: {Errors}")]
    public static partial void Failed(ILogger logger, string errors);
}
