using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;
using NaHora.Infrastructure.Persistence;
using NaHora.Shared.Results;

namespace NaHora.Api.Development;

/// <summary>
/// Plants one company, one manager and one employee so the app can be signed
/// into on a fresh database.
/// </summary>
/// <remarks>
/// <para>
/// Nothing else creates them yet: there is no onboarding endpoint, so without
/// this a developer who runs the stack has a schema and no way to reach a
/// single screen behind the login.
/// </para>
/// <para>
/// <b>Development only, twice over.</b> The environment must be Development and
/// the flag must be set. It writes an account whose password lives in
/// configuration, and a single guard is one deployment mistake away from
/// putting that account in production.
/// </para>
/// <para>
/// Idempotent by the fixed company identifier, so restarting the API does not
/// stack duplicates.
/// </para>
/// </remarks>
internal static class DevelopmentSeed
{
    public static async Task ApplyAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetRequiredService<IOptions<DevelopmentSeedOptions>>().Value;
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (!options.Enabled)
        {
            return;
        }

        // The second lock. Configuration can be wrong; the environment is what
        // the deployment itself asserts.
        if (!app.Environment.IsDevelopment())
        {
            DevelopmentSeedLog.IgnoredOutsideDevelopment(logger);

            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NaHoraDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // IgnoreQueryFilters because the tenant filter reads the current user,
        // and at startup there is no request and therefore nobody signed in.
        var alreadySeeded = await dbContext.Companies
            .IgnoreQueryFilters()
            .AnyAsync(company => company.Id == DevelopmentSeedData.CompanyId);

        if (alreadySeeded)
        {
            DevelopmentSeedLog.AlreadyPresent(logger, DevelopmentSeedData.CompanyId);

            return;
        }

        var result = Build(passwordHasher, options.Password, clock.GetUtcNow());
        if (result.IsFailure)
        {
            // Loud, and not fatal. A broken seed must not stop the API from
            // starting: health and OpenAPI are still worth reaching.
            DevelopmentSeedLog.Failed(logger, string.Join("; ", result.Errors.Select(error => error.Code)));

            return;
        }

        var seeded = result.Value;

        dbContext.Companies.Add(seeded.Company);
        dbContext.Users.Add(seeded.Manager);
        dbContext.Users.Add(seeded.EmployeeUser);
        dbContext.Employees.Add(seeded.Employee);

        await dbContext.SaveChangesAsync();

        DevelopmentSeedLog.Seeded(
            logger,
            DevelopmentSeedData.CompanyId,
            DevelopmentSeedData.ManagerCpf,
            DevelopmentSeedData.EmployeeCpf);
    }

    /// <summary>
    /// Builds the whole set, collecting every problem rather than stopping at
    /// the first.
    /// </summary>
    private static Result<SeededTenant> Build(
        IPasswordHasher passwordHasher,
        string password,
        DateTimeOffset now)
    {
        var cnpj = Cnpj.Create(DevelopmentSeedData.Cnpj);
        var companyEmail = EmailAddress.Create(DevelopmentSeedData.CompanyEmail);
        var managerCpf = Cpf.Create(DevelopmentSeedData.ManagerCpf);
        var managerEmail = EmailAddress.Create(DevelopmentSeedData.ManagerEmail);
        var employeeCpf = Cpf.Create(DevelopmentSeedData.EmployeeCpf);
        var employeeEmail = EmailAddress.Create(DevelopmentSeedData.EmployeeEmail);
        var employeePis = Pis.Create(DevelopmentSeedData.EmployeePis);
        var companyPhone = PhoneNumber.Create(DevelopmentSeedData.CompanyPhone);
        var employeePhone = PhoneNumber.Create(DevelopmentSeedData.EmployeePhone);
        var address = Address.Create(
            DevelopmentSeedData.PostalCode,
            DevelopmentSeedData.Street,
            DevelopmentSeedData.Number,
            DevelopmentSeedData.Complement,
            DevelopmentSeedData.District,
            DevelopmentSeedData.City,
            DevelopmentSeedData.State);

        Result[] documents =
        [
            cnpj, companyEmail, managerCpf, managerEmail, employeeCpf, employeeEmail, employeePis,
            companyPhone, employeePhone, address
        ];

        var documentErrors = documents
            .Where(document => document.IsFailure)
            .SelectMany(document => document.Errors)
            .ToList();

        if (documentErrors.Count > 0)
        {
            return Result.Failure<SeededTenant>(documentErrors);
        }

        var companyResult = Company.Register(
            cnpj.Value,
            DevelopmentSeedData.LegalName,
            DevelopmentSeedData.TradeName,
            companyEmail.Value,
            address.Value,
            Company.DefaultTimeZoneId,
            now,
            companyPhone.Value,
            DevelopmentSeedData.CompanyStateRegistration);

        if (companyResult.IsFailure)
        {
            return Result.Failure<SeededTenant>(companyResult.Errors);
        }

        var company = companyResult.Value;

        // Signing in refuses a company that is not operational, and a freshly
        // registered one is still awaiting email confirmation.
        company.ConfirmEmail(now);

        ForceSeedIdentifier(company, DevelopmentSeedData.CompanyId);

        var passwordHash = passwordHasher.Hash(password);

        var managerResult = User.Create(
            company.Id,
            managerCpf.Value,
            DevelopmentSeedData.ManagerName,
            managerEmail.Value,
            passwordHash,
            UserRole.Manager,

            // False on purpose: a change-password prompt after every reset is
            // friction with no security value on a throwaway database.
            mustChangePassword: false,
            now);

        var employeeUserResult = User.Create(
            company.Id,
            employeeCpf.Value,
            DevelopmentSeedData.EmployeeName,
            employeeEmail.Value,
            passwordHash,
            UserRole.Employee,
            mustChangePassword: false,
            now);

        if (managerResult.IsFailure || employeeUserResult.IsFailure)
        {
            return Result.Failure<SeededTenant>([.. managerResult.Errors, .. employeeUserResult.Errors]);
        }

        var employeeResult = Employee.Hire(
            company.Id,
            employeeUserResult.Value.Id,
            employeeCpf.Value,
            employeePis.Value,
            DevelopmentSeedData.EmployeeName,
            DevelopmentSeedData.EmployeeRegistration,
            DevelopmentSeedData.EmployeePosition,
            DateOnly.FromDateTime(now.UtcDateTime),
            now,
            department: DevelopmentSeedData.EmployeeDepartment,
            birthDate: new DateOnly(1995, 4, 12),
            email: employeeEmail.Value,
            phone: employeePhone.Value,
            address: address.Value);

        if (employeeResult.IsFailure)
        {
            return Result.Failure<SeededTenant>(employeeResult.Errors);
        }

        return Result.Success(new SeededTenant(
            company,
            managerResult.Value,
            employeeUserResult.Value,
            employeeResult.Value));
    }

    /// <summary>
    /// Forces the company identifier to the fixed development one.
    /// </summary>
    /// <remarks>
    /// Reflection, and deliberately confined to this file. Identity is
    /// <c>protected init</c> so nothing in the product can choose it, and that
    /// rule earns its keep. But signing in is per company, so the app must know
    /// a company identifier before anyone can authenticate, and one that
    /// changed on every reset would mean editing the Flutter configuration
    /// after each one.
    /// </remarks>
    private static void ForceSeedIdentifier(Company company, Guid id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(company, id);

    private readonly record struct SeededTenant(
        Company Company,
        User Manager,
        User EmployeeUser,
        Employee Employee);
}
