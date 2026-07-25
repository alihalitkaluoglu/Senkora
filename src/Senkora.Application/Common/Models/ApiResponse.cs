namespace Senkora.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public object? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, object? meta = null)
        => new() { Success = true, Data = data, Message = message, Meta = meta };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null)
        => new() { Success = false, Errors = errors, Message = message };

    public static ApiResponse<T> Fail(string error)
        => Fail([error]);
}

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponse Fail(string error) => new() { Success = false, Errors = [error] };
}
