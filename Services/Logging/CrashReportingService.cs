using Newtonsoft.Json;
using System.Text;

namespace MoneyMindApp.Services.Logging;

/// <summary>
/// Implementation of crash reporting service
/// Captures unhandled exceptions and saves crash reports locally
/// </summary>
public class CrashReportingService : ICrashReportingService
{
    private readonly ILoggingService _loggingService;
    private readonly string _crashDirectory;
    private bool _isInitialized = false;

    public CrashReportingService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _crashDirectory = Path.Combine(FileSystem.AppDataDirectory, "crashes");
        Directory.CreateDirectory(_crashDirectory);
    }

    /// <summary>
    /// Initialize crash reporting by registering exception handlers
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            // Register unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Register task exception handler
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _isInitialized = true;
            _loggingService.LogInfo("Crash reporting initialized");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to initialize crash reporting", ex);
        }
    }

    /// <summary>
    /// Record a non-fatal exception
    /// </summary>
    public void RecordException(Exception exception, string context = "")
    {
        try
        {
            var report = CreateCrashReport(exception, context, isFatal: false);
            SaveCrashReport(report);
            _loggingService.LogError($"Non-fatal exception in {context}", exception);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error recording exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all crash reports
    /// </summary>
    public async Task<List<CrashReport>> GetCrashReportsAsync()
    {
        var reports = new List<CrashReport>();

        try
        {
            var crashFiles = Directory.GetFiles(_crashDirectory, "crash_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var file in crashFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var report = JsonConvert.DeserializeObject<CrashReport>(json);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading crash report {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error getting crash reports", ex);
        }

        return reports;
    }

    /// <summary>
    /// Clear old crash reports (older than specified days)
    /// </summary>
    public async Task ClearOldReportsAsync(int retainDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retainDays);
            var crashFiles = Directory.GetFiles(_crashDirectory, "crash_*.json");

            foreach (var file in crashFiles)
            {
                var fileDate = File.GetLastWriteTime(file);
                if (fileDate < cutoffDate)
                {
                    File.Delete(file);
                    _loggingService.LogInfo($"Deleted old crash report: {Path.GetFileName(file)}");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error clearing old crash reports", ex);
        }
    }

    /// <summary>
    /// Export all crash reports to a single file
    /// </summary>
    public async Task<string> ExportCrashReportsAsync()
    {
        try
        {
            var exportPath = Path.Combine(FileSystem.CacheDirectory, $"crash_reports_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var reports = await GetCrashReportsAsync();

            var sb = new StringBuilder();
            sb.AppendLine("MoneyMind Crash Reports");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Reports: {reports.Count}");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            foreach (var report in reports)
            {
                sb.AppendLine($"--- Crash Report: {report.Timestamp:yyyy-MM-dd HH:mm:ss} ---");
                sb.AppendLine($"Exception: {report.ExceptionType}");
                sb.AppendLine($"Message: {report.Message}");
                sb.AppendLine($"Context: {report.Context}");
                sb.AppendLine($"Device: {report.DeviceInfo}");
                sb.AppendLine($"App Version: {report.AppVersion}");
                sb.AppendLine();
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(report.StackTrace);
                sb.AppendLine();
                sb.AppendLine("=".PadRight(80, '='));
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(exportPath, sb.ToString());
            _loggingService.LogInfo($"Crash reports exported to: {exportPath}");

            return exportPath;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting crash reports", ex);
            return string.Empty;
        }
    }

    #region Private Methods

    /// <summary>
    /// Handle unhandled exceptions from AppDomain
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                var report = CreateCrashReport(exception, "AppDomain.UnhandledException", isFatal: e.IsTerminating);
                SaveCrashReport(report);
                _loggingService.LogFatal("Unhandled exception", exception);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnUnhandledException: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle unobserved task exceptions
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            var report = CreateCrashReport(e.Exception, "TaskScheduler.UnobservedTaskException", isFatal: false);
            SaveCrashReport(report);
            _loggingService.LogError("Unobserved task exception", e.Exception);

            // Mark as observed to prevent process termination
            e.SetObserved();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnUnobservedTaskException: {ex.Message}");
        }
    }

    /// <summary>
    /// Create crash report from exception
    /// </summary>
    private CrashReport CreateCrashReport(Exception exception, string context, bool isFatal)
    {
        return new CrashReport
        {
            Timestamp = DateTime.Now,
            ExceptionType = exception.GetType().FullName ?? "Unknown",
            Message = exception.Message,
            StackTrace = exception.StackTrace ?? "No stack trace available",
            Context = $"{context} (Fatal: {isFatal})",
            DeviceInfo = $"{DeviceInfo.Model} ({DeviceInfo.Platform} {DeviceInfo.VersionString})",
            AppVersion = AppInfo.VersionString
        };
    }

    /// <summary>
    /// Save crash report to file
    /// </summary>
    private void SaveCrashReport(CrashReport report)
    {
        try
        {
            var filename = $"crash_{report.Timestamp:yyyyMMdd_HHmmss}.json";
            var filepath = Path.Combine(_crashDirectory, filename);

            var json = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText(filepath, json);

            System.Diagnostics.Debug.WriteLine($"Crash report saved: {filename}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving crash report: {ex.Message}");
        }
    }

    #endregion
}
