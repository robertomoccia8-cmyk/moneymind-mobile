namespace MoneyMindApp.Services.Logging;

/// <summary>
/// Service for application logging
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Log debug message (development only)
    /// </summary>
    void LogDebug(string message);

    /// <summary>
    /// Log informational message
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Log warning message
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Log error message
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Log fatal error (application crash)
    /// </summary>
    void LogFatal(string message, Exception exception);

    /// <summary>
    /// Get recent log entries
    /// </summary>
    Task<List<LogEntry>> GetRecentLogsAsync(int count = 100);

    /// <summary>
    /// Clear old logs (older than specified days)
    /// </summary>
    Task ClearOldLogsAsync(int retainDays = 7);

    /// <summary>
    /// Export logs to file
    /// </summary>
    Task<string> ExportLogsAsync();
}

/// <summary>
/// Log entry model
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

/// <summary>
/// Log levels
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
