using Serilog;
using System.Text;

namespace MoneyMindApp.Services.Logging;

/// <summary>
/// Implementation of logging service using Serilog
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;

    public LoggingService()
    {
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "moneymind.log");
    }

    public void LogDebug(string message)
    {
#if DEBUG
        Log.Debug(message);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
#endif
    }

    public void LogInfo(string message)
    {
        Log.Information(message);
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        Log.Warning(message);
        System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            Log.Error(exception, message);
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}: {exception}");
        }
        else
        {
            Log.Error(message);
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
        }
    }

    public void LogFatal(string message, Exception exception)
    {
        Log.Fatal(exception, message);
        System.Diagnostics.Debug.WriteLine($"[FATAL] {message}: {exception}");
    }

    /// <summary>
    /// Get recent log entries from log files
    /// </summary>
    public async Task<List<LogEntry>> GetRecentLogsAsync(int count = 100)
    {
        var logs = new List<LogEntry>();

        try
        {
            // Get all log files in directory (sorted by date)
            var logFiles = Directory.GetFiles(_logDirectory, "moneymind*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(3); // Last 3 days

            foreach (var logFile in logFiles)
            {
                if (!File.Exists(logFile))
                    continue;

                var lines = await File.ReadAllLinesAsync(logFile);

                // Parse log lines (Serilog format)
                foreach (var line in lines.Reverse().Take(count))
                {
                    var entry = ParseLogLine(line);
                    if (entry != null)
                    {
                        logs.Add(entry);
                    }

                    if (logs.Count >= count)
                        break;
                }

                if (logs.Count >= count)
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading logs: {ex.Message}");
        }

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    /// <summary>
    /// Clear old logs (older than specified days)
    /// </summary>
    public async Task ClearOldLogsAsync(int retainDays = 7)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retainDays);
            var logFiles = Directory.GetFiles(_logDirectory, "moneymind*.log");

            foreach (var logFile in logFiles)
            {
                var fileDate = File.GetLastWriteTime(logFile);
                if (fileDate < cutoffDate)
                {
                    File.Delete(logFile);
                    LogInfo($"Deleted old log file: {Path.GetFileName(logFile)}");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogError("Error clearing old logs", ex);
        }
    }

    /// <summary>
    /// Export all logs to a single file in Downloads folder
    /// </summary>
    public async Task<string> ExportLogsAsync()
    {
        try
        {
            // Save to Downloads folder (platform-specific)
            string exportDirectory;
#if ANDROID
            // Android: Use app-specific directory in Downloads (no permissions needed)
            exportDirectory = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath
                ?? FileSystem.CacheDirectory;
#else
            // Other platforms: use Documents folder
            exportDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
            Directory.CreateDirectory(exportDirectory);

            var exportPath = Path.Combine(exportDirectory, $"moneymind_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var sb = new StringBuilder();

            sb.AppendLine("MoneyMind App Logs");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Device: {DeviceInfo.Model} ({DeviceInfo.Platform} {DeviceInfo.VersionString})");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            var logFiles = Directory.GetFiles(_logDirectory, "moneymind*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var logFile in logFiles)
            {
                sb.AppendLine($"--- {Path.GetFileName(logFile)} ---");
                var content = await File.ReadAllTextAsync(logFile);
                sb.AppendLine(content);
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(exportPath, sb.ToString());
            LogInfo($"Logs exported to: {exportPath}");

            return exportPath;
        }
        catch (Exception ex)
        {
            LogError("Error exporting logs", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Parse Serilog log line to LogEntry
    /// Format: "2024-01-15 10:30:45.123 +01:00 [INF] Message here"
    /// </summary>
    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Simple parser for Serilog format
            var parts = line.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var timestampPart = parts[0].Trim();
            var levelPart = parts[1].Trim();
            var messagePart = parts.Length > 2 ? string.Join(" ", parts.Skip(2)).Trim() : "";

            // Parse timestamp
            if (!DateTime.TryParse(timestampPart.Split('+')[0].Trim(), out var timestamp))
                return null;

            // Parse level
            var level = levelPart switch
            {
                "DBG" => LogLevel.Debug,
                "INF" => LogLevel.Info,
                "WRN" => LogLevel.Warning,
                "ERR" => LogLevel.Error,
                "FTL" => LogLevel.Fatal,
                _ => LogLevel.Info
            };

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = messagePart
            };
        }
        catch
        {
            return null;
        }
    }
}
