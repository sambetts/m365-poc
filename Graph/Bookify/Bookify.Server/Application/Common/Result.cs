namespace Bookify.Server.Application.Common;

/// <summary>
/// Lightweight functional result wrapper replacing ad-hoc tuple status patterns.
/// </summary>
public class Result
{
    public bool Success { get; }
    public string? Error { get; }

    protected Result(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool success, T? value, string? error) : base(success, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}
