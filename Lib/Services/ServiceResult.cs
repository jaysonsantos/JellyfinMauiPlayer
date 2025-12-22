namespace JellyfinPlayer.Lib.Services;

/// <summary>
/// Result type for service operations using modern C# pattern matching.
/// </summary>
public abstract record ServiceResult<T>
{
    public sealed record Success(T Value) : ServiceResult<T>;

    public sealed record Error(string Message, Exception? Exception = null) : ServiceResult<T>;

    private sealed record NotFound(string? Message = null) : ServiceResult<T>;

    public sealed record Unauthorized(string? Message = null) : ServiceResult<T>;

    public sealed record ValidationError(
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null
    ) : ServiceResult<T>;

    public bool IsSuccess => this is Success;
    public bool IsError => this is Error or NotFound or Unauthorized or ValidationError;

    public T? GetValueOrDefault()
    {
        return this switch
        {
            Success success => success.Value,
            _ => default,
        };
    }

    public ServiceResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return this switch
        {
            Success success => new ServiceResult<TResult>.Success(mapper(success.Value)),
            Error error => new ServiceResult<TResult>.Error(error.Message, error.Exception),
            NotFound notFound => new ServiceResult<TResult>.NotFound(notFound.Message),
            Unauthorized unauthorized => new ServiceResult<TResult>.Unauthorized(
                unauthorized.Message
            ),
            ValidationError validationError => new ServiceResult<TResult>.ValidationError(
                validationError.Message,
                validationError.Errors
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(mapper)),
        };
    }

    public async Task<ServiceResult<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        return this switch
        {
            Success success => new ServiceResult<TResult>.Success(
                await mapper(success.Value).ConfigureAwait(false)
            ),
            Error error => new ServiceResult<TResult>.Error(error.Message, error.Exception),
            NotFound notFound => new ServiceResult<TResult>.NotFound(notFound.Message),
            Unauthorized unauthorized => new ServiceResult<TResult>.Unauthorized(
                unauthorized.Message
            ),
            ValidationError validationError => new ServiceResult<TResult>.ValidationError(
                validationError.Message,
                validationError.Errors
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(mapper)),
        };
    }
}

public static class ServiceResultExtensions
{
    public static ServiceResult<T> ToServiceResult<T>(this T value) =>
        new ServiceResult<T>.Success(value);

    public static ServiceResult<T> ToServiceResult<T>(this Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => httpEx.Message.Contains("401")
            || httpEx.Message.Contains("Unauthorized")
                ? new ServiceResult<T>.Unauthorized("Authentication failed")
                : new ServiceResult<T>.Error("HTTP request failed", httpEx),
            TaskCanceledException => new ServiceResult<T>.Error(
                "Operation was cancelled",
                exception
            ),
            TimeoutException => new ServiceResult<T>.Error("Operation timed out", exception),
            ArgumentNullException => new ServiceResult<T>.ValidationError(exception.Message),
            ArgumentException => new ServiceResult<T>.ValidationError(exception.Message),
            _ => new ServiceResult<T>.Error("An error occurred", exception),
        };
    }
}
