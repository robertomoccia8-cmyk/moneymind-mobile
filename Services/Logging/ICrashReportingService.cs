namespace MoneyMindApp.Services.Logging;

/// <summary>
/// Service for crash reporting and unhandled exception tracking
/// </summary>
public interface ICrashReportingService
{
    /// <summary>
    /// Initialize crash reporting (register handlers)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Record a non-fatal exception
    /// </summary>
    void RecordException(Exception exception, string context = "");

    /// <summary>
    /// Get crash reports
    /// </summary>
    Task<List<CrashReport>> GetCrashReportsAsync();

    /// <summary>
    /// Clear old crash reports
    /// </summary>
    Task ClearOldReportsAsync(int retainDays = 30);

    /// <summary>
    /// Export crash reports to file
    /// </summary>
    Task<string> ExportCrashReportsAsync();
}

/// <summary>
/// Crash report model
/// </summary>
public class CrashReport
{
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
}
