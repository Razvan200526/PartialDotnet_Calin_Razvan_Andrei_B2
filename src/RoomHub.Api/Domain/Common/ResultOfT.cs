namespace RoomHub.Api.Domain.Common;

public sealed class Result<T> : Result
{
    private Result(T? value, Error? error) : base(error) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, null);
    public new static Result<T> Failure(Error error) => new(default, error);
}
