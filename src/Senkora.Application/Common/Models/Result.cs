namespace Senkora.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool success, T? data, string? error, string? code)
        => (IsSuccess, Data, Error, ErrorCode) = (success, data, error, code);

    public static Result<T> Success(T data) => new(true, data, null, null);
    public static Result<T> Failure(string error, string code = "ERROR") => new(false, default, error, code);

    public static implicit operator Result<T>(T data) => Success(data);
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool success, string? error, string? code)
        => (IsSuccess, Error, ErrorCode) = (success, error, code);

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string code = "ERROR") => new(false, error, code);
}
