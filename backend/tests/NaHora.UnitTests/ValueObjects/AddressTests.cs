using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.ValueObjects;

/// <summary>
/// The address shared by the company and the employee.
/// </summary>
public sealed class AddressTests
{
    [Fact]
    public void Create_WithACompleteAddress_Succeeds()
    {
        // Arrange & Act
        var result = Address.Create(
            "01310-100",
            " Avenida Paulista ",
            "1000",
            "Conjunto 42",
            "Bela Vista",
            "São Paulo",
            "sp");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Street.ShouldBe("Avenida Paulista", "surrounding whitespace is not part of the name");
        result.Value.State.ShouldBe("SP", "the state code is stored upper case whatever was typed");
        result.Value.PostalCode.Value.ShouldBe("01310100");
    }

    [Fact]
    public void Create_WithoutDistrictOrComplement_Succeeds()
    {
        // Arrange & Act — plenty of real addresses have neither, and a required
        // field somebody invents a value for is worse than an absent one
        var result = Address.Create("01310100", "Avenida Paulista", "1000", "  ", null, "São Paulo", "SP");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Complement.ShouldBeNull();
        result.Value.District.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNumberSpelledOut_IsAccepted()
    {
        // Arrange & Act — "S/N", "1000-A" and "Km 12" are all real; an integer
        // column would make them unrepresentable
        var result = Address.Create("01310100", "Rodovia Anhanguera", "Km 12", null, null, "Jundiaí", "SP");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Number.ShouldBe("Km 12");
    }

    [Fact]
    public void Create_WhenSeveralFieldsAreWrong_ReportsAllOfThem()
    {
        // Arrange & Act — a form that rejects one field per round trip is a
        // form people abandon
        var result = Address.Create("123", "  ", "", null, null, "", "XX");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Select(error => error.Code).ShouldBe([
            "POSTAL_CODE_INVALID",
            "ADDRESS_STREET_REQUIRED",
            "ADDRESS_NUMBER_REQUIRED",
            "ADDRESS_CITY_REQUIRED",
            "ADDRESS_STATE_INVALID"
        ]);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("São Paulo")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithAnUnknownState_IsRejected(string? state)
    {
        // Arrange & Act
        var result = Address.Create("01310100", "Avenida Paulista", "1000", null, null, "São Paulo", state);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "ADDRESS_STATE_INVALID");
    }

    [Fact]
    public void SingleLine_ReadsTheWayItWouldBeWrittenOnAnEnvelope()
    {
        // Arrange
        var address = Address.Create(
            "01310100",
            "Avenida Paulista",
            "1000",
            "Conjunto 42",
            "Bela Vista",
            "São Paulo",
            "SP").Value;

        // Act & Assert
        address.SingleLine.ShouldBe(
            "Avenida Paulista, 1000 - Conjunto 42 - Bela Vista - São Paulo/SP - 01310-100");
    }

    [Fact]
    public void SingleLine_OmitsTheOptionalParts_WhenTheyAreAbsent()
    {
        // Arrange
        var address = Address.Create("01310100", "Avenida Paulista", "1000", null, null, "São Paulo", "SP").Value;

        // Act & Assert — no stray separators where nothing was filled in
        address.SingleLine.ShouldBe("Avenida Paulista, 1000 - São Paulo/SP - 01310-100");
    }

    [Fact]
    public void TwoAddressesWithTheSameParts_AreEqual()
    {
        // Arrange — a value object has no identity of its own
        var first = Address.Create("01310-100", "Avenida Paulista", "1000", null, null, "São Paulo", "SP").Value;
        var second = Address.Create("01310100", "Avenida Paulista", "1000", null, null, "São Paulo", "sp").Value;

        // Act & Assert
        first.ShouldBe(second);
    }
}
