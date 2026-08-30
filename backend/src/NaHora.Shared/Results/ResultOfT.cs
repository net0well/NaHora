namespace NaHora.Shared.Results;

/// <summary>
/// Outcome carrying a value on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    private Result(TValue value) : base(true, []) => _value = value;

    private Result(IReadOnlyList<Error> errors) : base(false, errors) => _value = default!;

    /// <summary>
    /// The produced value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when read from a failed result. Reading the value of a failure is
    /// a programming error, not a business outcome, so it throws rather than
    /// handing back a default that would travel silently.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException(
            $"Cannot read the value of a failed result. First error: {FirstError}");

    internal static Result<TValue> Ok(TValue value) => new(value);

    internal static Result<TValue> Fail(IReadOnlyList<Error> errors) => new(errors);

    public static implicit operator Result<TValue>(TValue value) => Ok(value);

    /// <summary>Projects the value, preserving the errors of a failure.</summary>
    public Result<TNext> Map<TNext>(Func<TValue, TNext> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return IsSuccess ? Result<TNext>.Ok(map(_value)) : Result<TNext>.Fail(Errors);
    }
}
