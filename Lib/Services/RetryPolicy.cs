using System.Net;

namespace JellyfinPlayer.Lib.Services;

public sealed class RetryPolicy(
    int maxRetries = 3,
    TimeSpan? initialDelay = null,
    TimeSpan? maxDelay = null
)
{
    private readonly TimeSpan _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
    private readonly TimeSpan _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);

    public async Task<T?> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt))
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // All retries exhausted
        throw lastException ?? new InvalidOperationException("Retry operation failed");
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default
    )
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt))
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // All retries exhausted
        throw lastException ?? new InvalidOperationException("Retry operation failed");
    }

    private bool ShouldRetry(Exception exception, int attempt)
    {
        if (attempt >= maxRetries)
            return false;

        // Use pattern matching (C# 14) to determine if exception is retryable
        return exception switch
        {
            HttpRequestException httpEx => IsRetryableHttpException(httpEx),
            TaskCanceledException => false, // Don't retry cancellation
            OperationCanceledException => false, // Don't retry cancellation
            TimeoutException => true, // Retry timeouts
            IOException => true, // Retry IO errors
            _ => false, // Don't retry unknown exceptions
        };
    }

    private static bool IsRetryableHttpException(HttpRequestException ex)
    {
        // Check inner exception for HttpRequestError
        return ex.Data.Contains("StatusCode") switch
        {
            true when ex.Data["StatusCode"] is HttpStatusCode statusCode => IsRetryableStatusCode(
                statusCode
            ),
            _ => ex.InnerException switch
            {
                HttpRequestException innerHttpEx => IsRetryableHttpException(innerHttpEx),
                _ => true, // Retry if we can't determine status code
            },
        };
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            >= HttpStatusCode.InternalServerError => true, // 5xx errors
            _ => false,
        };
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Exponential backoff with jitter
        var exponentialDelay = TimeSpan.FromMilliseconds(
            _initialDelay.TotalMilliseconds * Math.Pow(2, attempt)
        );
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
        var totalDelay = exponentialDelay + jitter;

        return totalDelay > _maxDelay ? _maxDelay : totalDelay;
    }
}
