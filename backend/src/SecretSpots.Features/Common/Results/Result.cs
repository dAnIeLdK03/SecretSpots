namespace SecretSpots.Features.Common.Results;

public class Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _error = error;
    }

    public bool IsSuccess { get; }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(ResultMessages.ValueAccessedOnFailure);

    public Error Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException(ResultMessages.ErrorAccessedOnSuccess);

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error) => new(error);
}
