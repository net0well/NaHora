using System.Diagnostics.CodeAnalysis;

namespace NaHora.Shared.Results;

/// <summary>
/// Outcome of an operation that can fail for expected reasons.
/// </summary>
/// <remarks>
/// This is the notification pattern: a failure carries <em>every</em> error
/// found, not just the first. A registration form that rejects an invalid CPF,
/// then an invalid email, then a weak password, one round trip at a time, is a
/// form people abandon.
///
/// Exceptions stay for the genuinely exceptional — a dropped database
/// connection, a null reference bug — and reach the global handler.
/// </remarks>
public class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = [];

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException("A successful result cannot carry errors.");
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException("A failed result must carry at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    [MemberNotNullWhen(false, nameof(FirstError))]
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    /// The error that decides the HTTP status when several are present.
    /// </summary>
    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    public static Result Success() => new(true, NoErrors);

    public static Result Failure(Error error) => new(false, [error]);

    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Ok(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Fail([error]);

    public static Result<TValue> Failure<TValue>(IReadOnlyList<Error> errors) => Result<TValue>.Fail(errors);

    /// <summary>
    /// Combines several outcomes, keeping every error from all of them.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var errors = results.Where(result => result.IsFailure)
            .SelectMany(result => result.Errors)
            .ToArray();

        return errors.Length == 0 ? Success() : Failure(errors);
    }
}
