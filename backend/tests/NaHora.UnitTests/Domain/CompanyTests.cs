using NaHora.Domain.Companies;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;

namespace NaHora.UnitTests.Domain;

public sealed class CompanyTests
{
    private static readonly Cnpj ValidCnpj = Cnpj.Create("11.222.333/0001-81").Value;
    private static readonly EmailAddress ValidEmail = EmailAddress.Create("contato@acme.com.br").Value;
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WhenDataIsValid_StartsPendingConfirmation()
    {
        // Act
        var result = Register();

        // Assert — a company is not usable until its email is confirmed
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CompanyStatus.PendingEmailConfirmation);
        result.Value.IsOperational.ShouldBeFalse();
        result.Value.ActivatedAt.ShouldBeNull();
    }

    [Fact]
    public void Register_WhenNameIsBlankAndTimeZoneIsUnknown_ReportsBothErrors()
    {
        // Act
        var result = Company.Register(ValidCnpj, "  ", null, ValidEmail, ValidAddress, "Mars/Olympus", Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Select(error => error.Code)
            .ShouldBe(["COMPANY_LEGAL_NAME_REQUIRED", "COMPANY_TIMEZONE_INVALID"]);
    }

    [Theory]
    [InlineData("America/Sao_Paulo")]
    [InlineData("America/Manaus")]
    [InlineData("America/Rio_Branco")]
    [InlineData("America/Noronha")]
    public void Register_AcceptsEveryBrazilianTimeZone(string timeZoneId)
    {
        // Act — Brazil spans four zones; assuming one would misreport lateness
        // for anyone outside it.
        var result = Company.Register(ValidCnpj, "Acme", null, ValidEmail, ValidAddress, timeZoneId, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TimeZoneId.ShouldBe(timeZoneId);
    }

    [Fact]
    public void ConfirmEmail_ActivatesTheCompany()
    {
        // Arrange
        var company = Register().Value;

        // Act
        var result = company.ConfirmEmail(Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        company.Status.ShouldBe(CompanyStatus.Active);
        company.IsOperational.ShouldBeTrue();
        company.ActivatedAt.ShouldBe(Now);
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_Fails()
    {
        // Arrange
        var company = Register().Value;
        company.ConfirmEmail(Now);

        // Act
        var result = company.ConfirmEmail(Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("COMPANY_ALREADY_CONFIRMED");
    }

    [Fact]
    public void Suspend_WhenPendingConfirmation_Fails()
    {
        // Arrange
        var company = Register().Value;

        // Act
        var result = company.Suspend();

        // Assert
        result.IsFailure.ShouldBeTrue();
        company.Status.ShouldBe(CompanyStatus.PendingEmailConfirmation);
    }

    [Fact]
    public void Deactivate_IsTerminal()
    {
        // Arrange
        var company = Register().Value;
        company.ConfirmEmail(Now);
        company.Deactivate();

        // Act
        var result = company.Deactivate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("COMPANY_ALREADY_INACTIVE");
    }

    private static Address ValidAddress => Address.Create(
        "01310100",
        "Avenida Paulista",
        "1000",
        null,
        "Bela Vista",
        "São Paulo",
        "SP").Value;

    private static Result<Company> Register() =>
        Company.Register(ValidCnpj, "Acme Industria Ltda", "Acme", ValidEmail, ValidAddress, Company.DefaultTimeZoneId, Now);
}
