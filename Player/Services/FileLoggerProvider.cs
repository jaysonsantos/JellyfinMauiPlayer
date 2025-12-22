using Microsoft.Extensions.Logging;

namespace Player.Services;

/// <summary>
/// File-based logger provider that writes logs to a file in the app's data directory.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly LogLevel _minimumLogLevel;

    public FileLoggerProvider(string logFilePath, LogLevel minimumLogLevel = LogLevel.Information)
    {
        _logFilePath = logFilePath;
        _minimumLogLevel = minimumLogLevel;

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize log file with a header
        File.AppendAllText(
            _logFilePath,
            $"\n=== Log session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"
        );
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logFilePath, _minimumLogLevel);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private sealed class FileLogger(
        string categoryName,
        string logFilePath,
        LogLevel minimumLogLevel
    ) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minimumLogLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var message = formatter(state, exception);
                var logEntry =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{categoryName}] {message}";

                if (exception is not null)
                {
                    logEntry += $"\n{exception}";
                }

                logEntry += "\n";

                // Use a lock to prevent concurrent writes
                lock (logFilePath)
                {
                    File.AppendAllText(logFilePath, logEntry);
                }
            }
            catch
            {
                // Silently fail if we can't write to the log file
                // to prevent logging errors from crashing the app
            }
        }
    }
}
