using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.ValueObjects;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Create_WhenNumericAndValid_Succeeds(string input)
    {
        // Act
        var result = Cnpj.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("11222333000181");
    }

    [Theory]
    [InlineData("11222333000182")] // dígito verificador errado
    [InlineData("112223330001")] // curto demais
    [InlineData("")]
    [InlineData(null)]
    public void Create_WhenInvalid_FailsWithCnpjInvalid(string? input)
    {
        // Act
        var result = Cnpj.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("CNPJ_INVALID");
    }

    [Fact]
    public void Create_WhenAllCharactersAreEqual_Fails()
    {
        // Act
        var result = Cnpj.Create("00000000000000");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WhenCheckDigitsAreNotNumeric_Fails()
    {
        // Act — the alphanumeric format allows letters in the first twelve
        // positions, but the two check digits stay numeric in both layouts.
        var result = Cnpj.Create("1122233300018A");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalisesLetterCase()
    {
        // Arrange — the alphanumeric layout in force since 2026
        var lower = Cnpj.TryParse("ab222333000100", out var fromLower);
        var upper = Cnpj.TryParse("AB222333000100", out var fromUpper);

        // Act & Assert — whatever the check digit outcome, both inputs must
        // agree: case is not part of the identity.
        lower.ShouldBe(upper);

        if (lower)
        {
            fromLower.ShouldBe(fromUpper);
            fromLower.Value.ShouldBe(fromLower.Value.ToUpperInvariant());
        }
    }

    [Fact]
    public void ToFormattedString_ReturnsPunctuatedDocument()
    {
        // Arrange
        var cnpj = Cnpj.Create("11222333000181").Value;

        // Act & Assert
        cnpj.ToFormattedString().ShouldBe("11.222.333/0001-81");
    }
}
