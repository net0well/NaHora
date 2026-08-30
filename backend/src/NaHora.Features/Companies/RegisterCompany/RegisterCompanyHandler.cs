using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;

namespace NaHora.Features.Companies.RegisterCompany;

/// <summary>
/// Provisions a tenant: the company and the administrator who can sign into it.
/// </summary>
/// <remarks>
/// <para>
/// Both in one transaction. A company with no account is a row nobody can
/// reach, and creating the account afterwards would require an authenticated
/// caller that does not exist until the account does.
/// </para>
/// <para>
/// The company is activated here rather than left awaiting an email. This is an
/// operator action behind the API, not a public sign-up funnel — there is
/// nobody to confirm an address to, and a pending state nothing can leave is
/// worse than no pending state. When self-serve sign-up exists, that flow keeps
/// the company pending and this one stays as it is.
/// </para>
/// </remarks>
public static class RegisterCompanyHandler
{
    public static async Task<Result<RegisterCompanyResponse>> Handle(
        RegisterCompanyCommand command,
        ICompanyRepository companies,
        IUserRepository users,
        ICompanyRegistryLookup registry,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var parsed = ParseInput(command);
        if (parsed.IsFailure)
        {
            return Result.Failure<RegisterCompanyResponse>(parsed.Errors);
        }

        var input = parsed.Value;

        // Uniqueness is system-wide: one CNPJ is one legal entity, and a second
        // tenant for it would split one company's records in two.
        if (await companies.ExistsByCnpjAsync(input.Cnpj, cancellationToken))
        {
            return Result.Failure<RegisterCompanyResponse>(CompanyErrors.CnpjAlreadyRegistered());
        }

        var lookup = await registry.LookupAsync(input.Cnpj, cancellationToken);
        if (lookup.Outcome is RegistryLookupOutcome.NotFound)
        {
            return Result.Failure<RegisterCompanyResponse>(CompanyErrors.CnpjNotFoundInRegistry());
        }

        var now = clock.GetUtcNow();

        var companyResult = Company.Register(
            input.Cnpj,
            command.LegalName!,
            command.TradeName,
            input.Email,
            input.Address,
            command.TimeZoneId ?? Company.DefaultTimeZoneId,
            now,
            input.Phone,
            command.StateRegistration,
            command.MunicipalRegistration,
            command.FoundedOn);

        if (companyResult.IsFailure)
        {
            return Result.Failure<RegisterCompanyResponse>(companyResult.Errors);
        }

        var company = companyResult.Value;

        // Only when the registry actually answered. Unreachable is not the same
        // as confirmed, and recording it as such would launder an outage into a
        // verification nobody performed.
        if (lookup.Outcome is RegistryLookupOutcome.Found)
        {
            company.MarkRegistryVerified();
        }

        company.ConfirmEmail(now);

        var administratorResult = User.Create(
            company.Id,
            input.AdministratorCpf,
            command.Administrator!.FullName!,
            input.AdministratorEmail,
            passwordHasher.Hash(command.Administrator.Password!),
            UserRole.Administrator,

            // The operator typed this password, not its owner. They change it
            // on first sign-in.
            mustChangePassword: true,
            now);

        if (administratorResult.IsFailure)
        {
            return Result.Failure<RegisterCompanyResponse>(administratorResult.Errors);
        }

        companies.Add(company);
        users.Add(administratorResult.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RegisterCompanyResponse(
            company.Id,
            administratorResult.Value.Id,
            company.Cnpj.Value,
            company.LegalName,
            company.Status.ToString(),
            company.RegistryVerified));
    }

    /// <summary>
    /// Parses every value object at once, so a caller sees all the problems
    /// with their payload rather than one per round trip.
    /// </summary>
    private static Result<ParsedInput> ParseInput(RegisterCompanyCommand command)
    {
        if (command.Address is null)
        {
            return Result.Failure<ParsedInput>(CompanyErrors.AddressRequired());
        }

        if (command.Administrator is null)
        {
            return Result.Failure<ParsedInput>(CompanyErrors.AdministratorRequired());
        }

        var cnpj = Cnpj.Create(command.Cnpj, "cnpj");
        var email = EmailAddress.Create(command.Email, "email");
        var administratorCpf = Cpf.Create(command.Administrator.Cpf, "administrator.cpf");
        var administratorEmail = EmailAddress.Create(command.Administrator.Email, "administrator.email");

        var address = Address.Create(
            command.Address.PostalCode,
            command.Address.Street,
            command.Address.Number,
            command.Address.Complement,
            command.Address.District,
            command.Address.City,
            command.Address.State);

        var errors = new List<Error>();
        CollectErrors(errors, cnpj, email, administratorCpf, administratorEmail, address);

        // Optional, so absence is fine and a malformed value is not.
        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            var parsedPhone = PhoneNumber.Create(command.Phone, "phone");
            if (parsedPhone.IsFailure)
            {
                errors.AddRange(parsedPhone.Errors);
            }
            else
            {
                phone = parsedPhone.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(command.Administrator.FullName))
        {
            errors.Add(UserErrors.NameRequired());
        }

        // Checked against the person's own details, so a password that is their
        // name or their CPF is refused before it is ever hashed.
        if (administratorCpf.IsSuccess && administratorEmail.IsSuccess)
        {
            var password = PasswordPolicy.Validate(
                command.Administrator.Password,
                administratorCpf.Value,
                command.Administrator.FullName ?? string.Empty,
                administratorEmail.Value);

            if (password.IsFailure)
            {
                errors.AddRange(password.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<ParsedInput>(errors);
        }

        return Result.Success(new ParsedInput(
            cnpj.Value,
            email.Value,
            phone,
            address.Value,
            administratorCpf.Value,
            administratorEmail.Value));
    }

    private static void CollectErrors(List<Error> errors, params Result[] results)
    {
        foreach (var result in results.Where(result => result.IsFailure))
        {
            errors.AddRange(result.Errors);
        }
    }

    private readonly record struct ParsedInput(
        Cnpj Cnpj,
        EmailAddress Email,
        PhoneNumber? Phone,
        Address Address,
        Cpf AdministratorCpf,
        EmailAddress AdministratorEmail);
}
