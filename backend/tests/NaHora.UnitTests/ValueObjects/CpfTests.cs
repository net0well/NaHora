using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.ValueObjects;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData(" 529 982 247 25 ")]
    public void Create_WhenCheckDigitsAreValid_SucceedsRegardlessOfPunctuation(string input)
    {
        // Act
        var result = Cpf.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("52998224725");
    }

    [Theory]
    [InlineData("52998224726")] // último dígito errado
    [InlineData("12345678900")]
    [InlineData("529982247")] // curto demais
    [InlineData("529982247250")] // longo demais
    [InlineData("")]
    [InlineData(null)]
    public void Create_WhenInvalid_FailsWithCpfInvalid(string? input)
    {
        // Act
        var result = Cpf.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.Errors[0].Code.ShouldBe("CPF_INVALID");
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void Create_WhenAllDigitsAreEqual_Fails(string input)
    {
        // Act — these satisfy the modulus but are never issued, so the check
        // digit alone would let them through.
        var result = Cpf.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ToFormattedString_ReturnsPunctuatedDocument()
    {
        // Arrange
        var cpf = Cpf.Create("52998224725").Value;

        // Act & Assert
        cpf.ToFormattedString().ShouldBe("529.982.247-25");
    }

    [Fact]
    public void ToMaskedString_HidesTheDigitsThatIdentifyThePerson()
    {
        // Arrange
        var cpf = Cpf.Create("52998224725").Value;

        // Act
        var masked = cpf.ToMaskedString();

        // Assert — enough to tell records apart in an audit trail, not enough
        // to republish the document.
        masked.ShouldBe("***.982.247-**");
        masked.ShouldNotContain("529");
        masked.ShouldNotContain("25");
    }

    [Fact]
    public void Equality_ComparesByValue()
    {
        // Arrange
        var first = Cpf.Create("529.982.247-25").Value;
        var second = Cpf.Create("52998224725").Value;

        // Act & Assert
        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void Create_WhenFieldIsSupplied_AttachesItToTheError()
    {
        // Act
        var result = Cpf.Create("invalid", "founder.cpf");

        // Assert — lets the UI highlight the input instead of showing a banner
        result.Errors[0].Field.ShouldBe("founder.cpf");
    }
}
