namespace VPet.Cognition.Domain.Errors;

/// <summary>
/// A discriminated-union result: either a success value of <typeparamref name="T"/>
/// or an <see cref="Error"/>.
/// </summary>
public readonly struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => Error is not null;

    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public void Match(
        Action<T> onSuccess,
        Action<Error> onFailure)
    {
        if (IsSuccess) onSuccess(Value!);
        else onFailure(Error!);
    }

    public T ValueOrThrow() =>
        IsSuccess ? Value! : throw new InvalidOperationException($"Result is failure: {Error}");

    public T? ValueOrDefault(T? fallback = default) =>
        IsSuccess ? Value : fallback;

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}