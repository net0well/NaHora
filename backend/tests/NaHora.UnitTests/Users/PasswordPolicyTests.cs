using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.Users;

public sealed class PasswordPolicyTests
{
    private static readonly Cpf OwnerCpf = Cpf.Create("529.982.247-25").Value;
    private static readonly EmailAddress OwnerEmail = EmailAddress.Create("maria.souza@acme.com.br").Value;
    private const string OwnerName = "Maria Souza";

    [Theory]
    [InlineData("Tr0vao-Azul!")]
    [InlineData("chuva-Forte-2026")]
    [InlineData("QUEIJO+minas9")]
    public void Validate_WhenPasswordIsStrong_Succeeds(string password)
    {
        // Act
        var result = PasswordPolicy.Validate(password, OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Ab1!")]
    [InlineData("short9!A")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenTooShort_FailsWithWeakPassword(string? password)
    {
        // Act
        var result = PasswordPolicy.Validate(password, OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("WEAK_PASSWORD");
    }

    [Fact]
    public void Validate_WhenOnlyTwoCharacterClasses_Fails()
    {
        // Act — lower case and digits only
        var result = PasswordPolicy.Validate("chuvaforte2026", OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenPasswordContainsTheCpf_Fails()
    {
        // Act
        var result = PasswordPolicy.Validate("Ab!52998224725", OwnerCpf, OwnerName, OwnerEmail);

        // Assert — a password derived from a document that appears on every
        // form is not a secret.
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "WEAK_PASSWORD");
    }

    [Fact]
    public void Validate_WhenPasswordContainsANamePart_Fails()
    {
        // Act
        var result = PasswordPolicy.Validate("Maria-2026!x", OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenPasswordContainsTheEmailLocalPart_Fails()
    {
        // Act
        var result = PasswordPolicy.Validate("maria.souza-9A!", OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenNamePartIsShort_DoesNotRejectOnIt()
    {
        // Arrange — rejecting three-letter fragments would refuse reasonable
        // passwords for anyone with a short name.
        var shortName = "Ana Li";

        // Act
        var result = PasswordPolicy.Validate("Tr0vao-Azul!", OwnerCpf, shortName, OwnerEmail);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenSeveralRulesFail_ReportsAllOfThem()
    {
        // Act — too simple and contains the name
        var result = PasswordPolicy.Validate("mariasouzaaa", OwnerCpf, OwnerName, OwnerEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
    }
}
