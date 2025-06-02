namespace Domain.Models.Result;

public class Result<T>: Result
{
    public T? Value { get; }

    private Result(bool isSuccess, Error error, T? value) : base(isSuccess, error)
    {
        Value = value;
    }
    
    private Result(bool isSuccess, SuccessType error, T? value) : base(isSuccess, error)
    {
        Value = value;
    }
    public static Result<T> Success(SuccessType successType, T value) =>
        new(true, successType, value);
    public static Result<T> Failure(Error error, T? value = default) => 
        new(false, error, value);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    public SuccessType? SuccessType { get; set; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    protected Result(bool isSuccess, SuccessType? successType)
    {
        IsSuccess = isSuccess;
        SuccessType = successType;
    }
    public static Result Success(SuccessType successType) => new(true, successType);
    public static Result Failure(Error error) => new(false, error);
}