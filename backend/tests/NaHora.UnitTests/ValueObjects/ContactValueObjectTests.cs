using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.ValueObjects;

/// <summary>
/// The postal code and phone number that address and profile fields are built
/// from.
/// </summary>
public sealed class ContactValueObjectTests
{
    [Theory]
    [InlineData("01310-100")]
    [InlineData("01310100")]
    [InlineData(" 01310 100 ")]
    public void PostalCode_IgnoresPunctuation(string input)
    {
        // Arrange & Act — the same code typed two ways is one value
        var result = PostalCode.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("01310100");
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("")]
    [InlineData(null)]
    public void PostalCode_WithTheWrongNumberOfDigits_IsRejected(string? input)
    {
        // Arrange & Act
        var result = PostalCode.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("POSTAL_CODE_INVALID");
    }

    [Fact]
    public void PostalCode_Formatted_CarriesTheHyphen()
    {
        // Arrange & Act & Assert
        PostalCode.Create("01310100").Value.Formatted.ShouldBe("01310-100");
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11987654321", "11987654321")]
    [InlineData("(11) 3333-4444", "1133334444")]
    public void PhoneNumber_IgnoresPunctuation(string input, string expected)
    {
        // Arrange & Act
        var result = PhoneNumber.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("987654321")]
    [InlineData("119876543210")]
    [InlineData("")]
    [InlineData(null)]
    public void PhoneNumber_WithoutTenOrElevenDigits_IsRejected(string? input)
    {
        // Arrange & Act — an area code is not optional
        var result = PhoneNumber.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("PHONE_NUMBER_INVALID");
    }

    [Theory]
    [InlineData("11987654321", "(11) 98765-4321", true)]
    [InlineData("1133334444", "(11) 3333-4444", false)]
    public void PhoneNumber_FormatsByLength(string input, string expected, bool isMobile)
    {
        // Arrange & Act
        var phone = PhoneNumber.Create(input).Value;

        // Assert
        phone.Formatted.ShouldBe(expected);
        phone.IsMobile.ShouldBe(isMobile);
    }
}
