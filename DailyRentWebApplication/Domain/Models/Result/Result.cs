namespace Domain.Models.Result;

public class Result<T>: Result
{
    public T? Value { get; }

    private Result(bool isSuccess, Error? error, T? value) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) =>
        new(true, null, value);
    public static Result<T> Failure(Error error, T? value = default) => 
        new(false, error, value);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
}