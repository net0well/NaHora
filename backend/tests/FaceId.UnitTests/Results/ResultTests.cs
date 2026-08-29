using FaceId.Shared.Results;

namespace FaceId.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_CarriesNoErrors()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
        result.FirstError.ShouldBeNull();
    }

    [Fact]
    public void Failure_KeepsEveryError()
    {
        // Arrange — the point of the notification pattern: a form shows all of
        // its problems at once instead of one per round trip.
        Error[] errors =
        [
            Error.Validation("CPF_INVALID", "CPF is not valid.", "cpf"),
            Error.Validation("EMAIL_INVALID", "Email is not valid.", "email"),
            Error.Validation("USER_NAME_REQUIRED", "Full name is required.", "fullName")
        ];

        // Act
        var result = Result.Failure(errors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(3);
        result.Errors.Select(error => error.Code)
            .ShouldBe(["CPF_INVALID", "EMAIL_INVALID", "USER_NAME_REQUIRED"]);
    }

    [Fact]
    public void FirstError_DecidesTheResponseStatus()
    {
        // Arrange
        var result = Result.Failure(
        [
            Error.Conflict("CNPJ_ALREADY_REGISTERED", "Already registered."),
            Error.Validation("EMAIL_INVALID", "Email is not valid.")
        ]);

        // Act & Assert
        result.FirstError!.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Combine_MergesErrorsFromEveryFailure()
    {
        // Arrange
        var first = Result.Failure(Error.Validation("A", "first"));
        var second = Result.Success();
        var third = Result.Failure(Error.Validation("B", "second"));

        // Act
        var combined = Result.Combine(first, second, third);

        // Assert
        combined.IsFailure.ShouldBeTrue();
        combined.Errors.Select(error => error.Code).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void Combine_WhenEveryResultSucceeds_Succeeds()
    {
        // Act
        var combined = Result.Combine(Result.Success(), Result.Success());

        // Assert
        combined.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Value_WhenResultFailed_Throws()
    {
        // Arrange
        var result = Result.Failure<string>(Error.NotFound("X", "missing"));

        // Act & Assert — reading the value of a failure is a programming error,
        // not a business outcome, so it must not hand back a silent default.
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Map_PreservesErrorsOfAFailure()
    {
        // Arrange
        var result = Result.Failure<int>(Error.Validation("A", "bad"));

        // Act
        var mapped = result.Map(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors[0].Code.ShouldBe("A");
    }

    [Fact]
    public void Failure_WithoutErrors_IsRejected()
    {
        // Act & Assert — a failure nobody can explain is worse than a crash
        Should.Throw<InvalidOperationException>(() => Result.Failure([]));
    }
}
