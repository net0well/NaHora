using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Notifications;
using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Employees.HireEmployee;

/// <summary>
/// Registers an employee and the account they sign in with.
/// </summary>
/// <remarks>
/// <para>
/// Both in one transaction. An employment record nobody can sign in as is a row
/// that produces no punches, and an account with no employment record is
/// refused at the only screen the person opened the app for.
/// </para>
/// <para>
/// The password is generated, hashed, emailed, and never returned. The manager
/// who hires does not learn the credential of the person whose hours they will
/// audit.
/// </para>
/// </remarks>
public static class HireEmployeeHandler
{
    public static async Task<Result<HireEmployeeResponse>> Handle(
        HireEmployeeCommand command,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IUserRepository users,
        ICompanyRepository companies,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var parsed = ParseInput(command);
        if (parsed.IsFailure)
        {
            return Result.Failure<HireEmployeeResponse>(parsed.Errors);
        }

        var input = parsed.Value;
        var companyId = currentUser.CompanyId;

        // Both checked before anything is written, and both reported together
        // so a form does not reject one document per round trip.
        var conflicts = new List<Error>();

        if (await users.ExistsByCpfAsync(companyId, input.Cpf, cancellationToken)
            || await employees.ExistsByCpfAsync(companyId, input.Cpf, cancellationToken))
        {
            conflicts.Add(EmployeeErrors.CpfAlreadyRegistered());
        }

        if (await employees.ExistsByRegistrationAsync(companyId, input.Registration, cancellationToken))
        {
            conflicts.Add(EmployeeErrors.RegistrationAlreadyUsed());
        }

        if (await users.ExistsByEmailAsync(companyId, input.Email, cancellationToken))
        {
            conflicts.Add(UserErrors.EmailAlreadyRegistered());
        }

        if (conflicts.Count > 0)
        {
            return Result.Failure<HireEmployeeResponse>(conflicts);
        }

        var now = clock.GetUtcNow();
        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var password = TemporaryPassword.Generate();

        var userResult = User.Create(
            companyId,
            input.Cpf,
            command.FullName!,
            input.Email,
            passwordHasher.Hash(password),
            UserRole.Employee,

            // Nobody but the employee should end up knowing this one.
            mustChangePassword: true,
            now);

        if (userResult.IsFailure)
        {
            return Result.Failure<HireEmployeeResponse>(userResult.Errors);
        }

        var employeeResult = Employee.Hire(
            companyId,
            userResult.Value.Id,
            input.Cpf,
            input.Pis,
            command.FullName!,
            input.Registration,
            command.Position,
            command.HiredAt ?? Company.LocalDateAt(timeZoneId, now),
            now,
            input.ContractType,
            command.Department,
            command.BirthDate,
            input.Email,
            input.Phone,
            input.Address);

        if (employeeResult.IsFailure)
        {
            return Result.Failure<HireEmployeeResponse>(employeeResult.Errors);
        }

        users.Add(userResult.Value);
        employees.Add(employeeResult.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // After the commit, and its failure does not undo the hire. An
        // undelivered password is recoverable; a rolled-back employment record
        // is a person who cannot clock in tomorrow.
        var delivered = await email.SendAsync(
            WelcomeEmail.For(company, employeeResult.Value, input.Email, password),
            cancellationToken);

        return Result.Success(new HireEmployeeResponse(
            employeeResult.Value.Id,
            userResult.Value.Id,
            employeeResult.Value.FullName,
            employeeResult.Value.Cpf.Formatted,
            employeeResult.Value.Registration,
            input.Email.Value,
            delivered,
            employeeResult.Value.IsExportableToAfd));
    }

    /// <summary>
    /// Parses every value object at once, so a caller sees all the problems
    /// with their payload rather than one per round trip.
    /// </summary>
    private static Result<ParsedInput> ParseInput(HireEmployeeCommand command)
    {
        var cpf = Cpf.Create(command.Cpf, "cpf");
        var emailAddress = EmailAddress.Create(command.Email, "email");

        var errors = new List<Error>();

        foreach (var result in new Result[] { cpf, emailAddress }.Where(result => result.IsFailure))
        {
            errors.AddRange(result.Errors);
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add(EmployeeErrors.NameRequired());
        }

        if (string.IsNullOrWhiteSpace(command.Registration))
        {
            errors.Add(EmployeeErrors.RegistrationRequired());
        }

        // Optional, so absence is fine and a malformed value is not.
        Pis? pis = null;
        if (!string.IsNullOrWhiteSpace(command.Pis))
        {
            var parsedPis = Pis.Create(command.Pis, "pis");
            if (parsedPis.IsFailure)
            {
                errors.AddRange(parsedPis.Errors);
            }
            else
            {
                pis = parsedPis.Value;
            }
        }

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

        Address? address = null;
        if (command.Address is { } addressInput)
        {
            var parsedAddress = Address.Create(
                addressInput.PostalCode,
                addressInput.Street,
                addressInput.Number,
                addressInput.Complement,
                addressInput.District,
                addressInput.City,
                addressInput.State);

            if (parsedAddress.IsFailure)
            {
                errors.AddRange(parsedAddress.Errors);
            }
            else
            {
                address = parsedAddress.Value;
            }
        }

        var contractType = EmployeeContractType.Clt;
        if (!string.IsNullOrWhiteSpace(command.ContractType)
            && !Enum.TryParse(command.ContractType, ignoreCase: true, out contractType))
        {
            errors.Add(EmployeeErrors.ContractTypeUnknown(command.ContractType));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<ParsedInput>(errors);
        }

        return Result.Success(new ParsedInput(
            cpf.Value,
            emailAddress.Value,
            pis,
            command.Registration!.Trim(),
            contractType,
            phone,
            address));
    }

    private readonly record struct ParsedInput(
        Cpf Cpf,
        EmailAddress Email,
        Pis? Pis,
        string Registration,
        EmployeeContractType ContractType,
        PhoneNumber? Phone,
        Address? Address);
}
