using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private readonly ICrashReportingService _crashReportingService;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private string globalDatabaseSize = "0 KB";

    [ObservableProperty]
    private string accountDatabaseSize = "0 KB";

    [ObservableProperty]
    private string totalDatabaseSize = "0 KB";

    [ObservableProperty]
    private int totalAccounts = 0;

    [ObservableProperty]
    private int totalTransactions = 0;

    [ObservableProperty]
    private int totalSettings = 0;

    [ObservableProperty]
    private string logFileSize = "0 KB";

    [ObservableProperty]
    private int logEntryCount = 0;

    [ObservableProperty]
    private int crashReportCount = 0;

    [ObservableProperty]
    private ObservableCollection<string> recentLogs = new();

    [ObservableProperty]
    private ObservableCollection<string> recentCrashes = new();

    public AdminViewModel(
        GlobalDatabaseService globalDatabaseService,
        DatabaseService databaseService,
        ILoggingService loggingService,
        ICrashReportingService crashReportingService)
    {
        _globalDatabaseService = globalDatabaseService;
        _databaseService = databaseService;
        _loggingService = loggingService;
        _crashReportingService = crashReportingService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadDatabaseStatsAsync();
            await LoadLogStatsAsync();
            await LoadCrashStatsAsync();
            await LoadRecentLogsAsync();

            _loggingService.LogInfo("Admin panel loaded");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading admin panel", ex);
        }
    }

    private async Task LoadDatabaseStatsAsync()
    {
        try
        {
            var appDataPath = FileSystem.AppDataDirectory;

            // Global Database
            var globalDbPath = Path.Combine(appDataPath, "MoneyMind_Global.db");
            long globalSize = 0;
            if (File.Exists(globalDbPath))
            {
                globalSize = new FileInfo(globalDbPath).Length;
            }
            GlobalDatabaseSize = $"{globalSize / 1024.0:F2} KB";

            // Account Databases
            long accountSize = 0;
            var currentAccountId = Preferences.Get("current_account_id", 0);
            if (currentAccountId > 0)
            {
                var accountDbPath = Path.Combine(appDataPath, $"MoneyMind_Conto_{currentAccountId:D3}.db");
                if (File.Exists(accountDbPath))
                {
                    accountSize = new FileInfo(accountDbPath).Length;
                }
            }
            AccountDatabaseSize = $"{accountSize / 1024.0:F2} KB";

            // Total
            TotalDatabaseSize = $"{(globalSize + accountSize) / 1024.0:F2} KB";

            // Counts
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            TotalAccounts = accounts.Count;

            var startDate = new DateTime(2020, 1, 1);
            var endDate = DateTime.Now.AddDays(1);
            var transactions = await _databaseService.GetTransactionsAsync(startDate, endDate);
            TotalTransactions = transactions.Count;

            // Settings count (mock - TODO: implement GetAllSettings in GlobalDatabaseService)
            TotalSettings = 10; // Placeholder

            _loggingService.LogDebug($"Database stats loaded: Total {TotalDatabaseSize}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading database stats", ex);
        }
    }

    private async Task LoadLogStatsAsync()
    {
        try
        {
            var logFilePath = Path.Combine(FileSystem.AppDataDirectory, "logs", "moneymind.log");
            if (File.Exists(logFilePath))
            {
                var logSize = new FileInfo(logFilePath).Length;
                LogFileSize = $"{logSize / 1024.0:F2} KB";

                // Count log lines
                var lines = await File.ReadAllLinesAsync(logFilePath);
                LogEntryCount = lines.Length;
            }
            else
            {
                LogFileSize = "0 KB";
                LogEntryCount = 0;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading log stats", ex);
            LogFileSize = "Errore";
        }
    }

    private async Task LoadCrashStatsAsync()
    {
        try
        {
            var crashes = await _crashReportingService.GetCrashReportsAsync();
            CrashReportCount = crashes.Count;

            RecentCrashes.Clear();
            foreach (var crash in crashes.Take(5))
            {
                RecentCrashes.Add($"[{crash.Timestamp:dd/MM HH:mm}] {crash.ExceptionType}: {crash.Message}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading crash stats", ex);
        }
    }

    private async Task LoadRecentLogsAsync()
    {
        try
        {
            var logs = await _loggingService.GetRecentLogsAsync(20);

            RecentLogs.Clear();
            foreach (var log in logs)
            {
                RecentLogs.Add($"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {log.Message}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading recent logs", ex);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadDatabaseStatsAsync();
            await LoadLogStatsAsync();
            await LoadCrashStatsAsync();
            await LoadRecentLogsAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task VacuumDatabaseAsync()
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma",
                "Vuoi ottimizzare il database? Questo può richiedere alcuni secondi.",
                "Ottimizza",
                "Annulla");

            if (!confirm)
                return;

            _loggingService.LogInfo("Database vacuum started");

            // VACUUM Global DB
            long globalSaved = await _globalDatabaseService.VacuumDatabaseAsync();

            // VACUUM current account DB
            long accountSaved = await _databaseService.VacuumDatabaseAsync();

            long totalSaved = globalSaved + accountSaved;

            await Shell.Current.DisplayAlert(
                "Successo",
                $"Database ottimizzato con successo.\n\nSpazio recuperato: {totalSaved / 1024.0:F1} KB",
                "OK");

            _loggingService.LogInfo($"Database vacuum completed - saved {totalSaved} bytes");

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error vacuuming database", ex);
            await Shell.Current.DisplayAlert("Errore", $"Impossibile ottimizzare database: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportAllLogsAsync()
    {
        try
        {
            var logFilePath = await _loggingService.ExportLogsAsync();

            await Shell.Current.DisplayAlert(
                "Log Esportati",
                $"Log completi salvati in:\n{logFilePath}\n\n" +
                $"Per copiare su PC:\n" +
                $"adb pull {logFilePath} C:\\Logs\\",
                "OK");

            _loggingService.LogInfo("All logs exported from admin panel");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting logs", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile esportare log", "OK");
        }
    }

    [RelayCommand]
    private async Task CopyLogsToClipboardAsync()
    {
        try
        {
            var logs = await _loggingService.GetRecentLogsAsync(50);

            if (logs.Count == 0)
            {
                await Shell.Current.DisplayAlert("Info", "Nessun log disponibile", "OK");
                return;
            }

            var logText = string.Join("\n", logs.Select(log =>
                $"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {log.Message}"));

            await Clipboard.SetTextAsync(logText);

            await Shell.Current.DisplayAlert(
                "Successo",
                $"{logs.Count} log copiati negli appunti",
                "OK");

            _loggingService.LogInfo("Logs copied to clipboard from admin panel");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error copying logs to clipboard", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile copiare log", "OK");
        }
    }

    [RelayCommand]
    private async Task ClearOldLogsAsync()
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma",
                "Vuoi eliminare i log più vecchi di 7 giorni?",
                "Elimina",
                "Annulla");

            if (!confirm)
                return;

            await _loggingService.ClearOldLogsAsync(7);

            await Shell.Current.DisplayAlert(
                "Successo",
                "Log vecchi eliminati con successo.",
                "OK");

            _loggingService.LogInfo("Old logs cleared from admin panel");

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error clearing old logs", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile eliminare log", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportCrashReportsAsync()
    {
        try
        {
            var exportPath = await _crashReportingService.ExportCrashReportsAsync();

            await Shell.Current.DisplayAlert(
                "Crash Reports Esportati",
                $"Crash reports salvati in:\n{exportPath}",
                "OK");

            _loggingService.LogInfo("Crash reports exported from admin panel");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting crash reports", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile esportare crash reports", "OK");
        }
    }

    [RelayCommand]
    private async Task ViewFullLogsAsync()
    {
        try
        {
            var logFilePath = Path.Combine(FileSystem.AppDataDirectory, "logs", "moneymind.log");

            if (!File.Exists(logFilePath))
            {
                await Shell.Current.DisplayAlert("Log", "Nessun log disponibile", "OK");
                return;
            }

            var allLogs = await File.ReadAllTextAsync(logFilePath);

            // TODO: Create dedicated LogViewerPage
            await Shell.Current.DisplayAlert(
                "Log Completi",
                $"File: {logFilePath}\nDimensione: {LogFileSize}\nRighe: {LogEntryCount}\n\n" +
                $"Ultimi 500 caratteri:\n{allLogs.Substring(Math.Max(0, allLogs.Length - 500))}",
                "OK");

            _loggingService.LogInfo("Full logs viewed from admin panel");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error viewing full logs", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile visualizzare log", "OK");
        }
    }
}
