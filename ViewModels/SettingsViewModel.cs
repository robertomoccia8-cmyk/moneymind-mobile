using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.License;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.Updates;

namespace MoneyMindApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private readonly IDatabaseMigrationService _migrationService;
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string appVersion = "1.0.0";

    [ObservableProperty]
    private string licenseEmail = "Non attivata";

    [ObservableProperty]
    private string licenseSubscription = "N/A";

    [ObservableProperty]
    private string licenseStatus = "Non verificata";

    [ObservableProperty]
    private string licenseExpiration = "N/A";

    [ObservableProperty]
    private LicenseData? currentLicense;

    [ObservableProperty]
    private string selectedTheme = "Auto";

    [ObservableProperty]
    private string currencySymbol = "€";

    [ObservableProperty]
    private bool backupOnCloud = false;

    [ObservableProperty]
    private bool showNotifications = true;

    [ObservableProperty]
    private bool biometricEnabled = false;

    [ObservableProperty]
    private string selectedTransactionGrouping = "Mese Solare";

    [ObservableProperty]
    private string databaseSize = "Calcolando...";

    [ObservableProperty]
    private int totalTransactions = 0;

    [ObservableProperty]
    private int totalAccounts = 0;

    [ObservableProperty]
    private bool isAdmin = false;

    [ObservableProperty]
    private bool isRefreshing = false;

    private int _adminTapCount = 0;
    private DateTime _lastAdminTap = DateTime.MinValue;

    public List<string> AvailableThemes { get; } = new() { "Light", "Dark", "Auto" };

    public List<string> AvailableTransactionGroupings { get; } = new() { "Mese Solare", "Mese Stipendiale" };

    public SettingsViewModel(
        GlobalDatabaseService globalDatabaseService,
        DatabaseService databaseService,
        ILoggingService loggingService,
        IDatabaseMigrationService migrationService,
        ILicenseService licenseService,
        IUpdateService updateService)
    {
        _globalDatabaseService = globalDatabaseService;
        _databaseService = databaseService;
        _loggingService = loggingService;
        _migrationService = migrationService;
        _licenseService = licenseService;
        _updateService = updateService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Load settings from Preferences
            SelectedTheme = Preferences.Get("app_theme", "Auto");
            CurrencySymbol = Preferences.Get("currency_symbol", "€");
            BackupOnCloud = Preferences.Get("backup_cloud", false);
            ShowNotifications = Preferences.Get("show_notifications", true);
            BiometricEnabled = Preferences.Get("biometric_enabled", false);
            SelectedTransactionGrouping = Preferences.Get("transaction_grouping", "Mese Solare");

            // Load license info from cache
            CurrentLicense = _licenseService.GetCachedLicense();
            if (CurrentLicense != null)
            {
                LicenseEmail = CurrentLicense.Email;
                LicenseSubscription = CurrentLicense.Subscription;
                LicenseStatus = CurrentLicense.StatusText;
                LicenseExpiration = CurrentLicense.FormattedExpiration;
            }

            // Get app version
            AppVersion = _updateService.GetCurrentVersion();

            // Check if user is admin (based on license, not Preferences!)
            IsAdmin = CurrentLicense?.IsAdmin ?? false;
            _loggingService.LogDebug($"Admin status: {IsAdmin} (Subscription: {CurrentLicense?.Subscription ?? "None"})");

            // Load database statistics
            await LoadDatabaseStatsAsync();

            _loggingService.LogInfo("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading settings", ex);
        }
    }

    private async Task LoadDatabaseStatsAsync()
    {
        try
        {
            // Get database file sizes
            var appDataPath = FileSystem.AppDataDirectory;
            var globalDbPath = Path.Combine(appDataPath, "MoneyMind_Global.db");

            long totalSize = 0;
            if (File.Exists(globalDbPath))
            {
                totalSize += new FileInfo(globalDbPath).Length;
            }

            // Get account database sizes
            var currentAccountId = Preferences.Get("current_account_id", 0);
            if (currentAccountId > 0)
            {
                var accountDbPath = Path.Combine(appDataPath, $"MoneyMind_Conto_{currentAccountId:D3}.db");
                if (File.Exists(accountDbPath))
                {
                    totalSize += new FileInfo(accountDbPath).Length;
                }
            }

            DatabaseSize = $"{totalSize / 1024.0:F1} KB";

            // Get account count
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            TotalAccounts = accounts.Count;

            // Get transaction count across ALL accounts
            int totalTransactionCount = 0;

            // ALSO check for orphaned account DBs not in GlobalDB
            var allDbFiles = Directory.GetFiles(appDataPath, "MoneyMind_Conto_*.db");
            _loggingService.LogDebug($"Found {allDbFiles.Length} account DB files on disk");

            foreach (var account in accounts)
            {
                var accountDbPath = Path.Combine(appDataPath, $"MoneyMind_Conto_{account.Id:D3}.db");
                if (File.Exists(accountDbPath))
                {
                    // Create temporary DatabaseService for this account
                    var accountDbService = new DatabaseService(_loggingService, _migrationService);
                    await accountDbService.InitializeAsync(account.Id);

                    var startDate = new DateTime(2020, 1, 1);
                    var endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1); // Fine di oggi
                    var transactions = await accountDbService.GetTransactionsAsync(startDate, endDate);
                    totalTransactionCount += transactions.Count;

                    _loggingService.LogDebug($"Account {account.Id} ({account.Nome}): {transactions.Count} transactions");

                    // Log each transaction
                    foreach (var t in transactions)
                    {
                        _loggingService.LogDebug($"  - {t.Data:dd/MM/yyyy} | {t.Descrizione} | {t.Importo:C2}");
                    }
                }
            }
            TotalTransactions = totalTransactionCount;

            _loggingService.LogDebug($"Database stats loaded: {DatabaseSize}, {TotalTransactions} transactions, {TotalAccounts} accounts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading database stats", ex);
            DatabaseSize = "Errore";
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Preferences.Set("app_theme", value);
        _loggingService.LogInfo($"Theme changed to: {value}");

        // Apply theme change
        ApplyTheme(value);
    }

    partial void OnCurrencySymbolChanged(string value)
    {
        Preferences.Set("currency_symbol", value);
        _loggingService.LogInfo($"Currency symbol changed to: {value}");
    }

    partial void OnBackupOnCloudChanged(bool value)
    {
        Preferences.Set("backup_cloud", value);
        _loggingService.LogInfo($"Backup on cloud: {value}");
    }

    partial void OnShowNotificationsChanged(bool value)
    {
        Preferences.Set("show_notifications", value);
        _loggingService.LogInfo($"Show notifications: {value}");
    }

    partial void OnBiometricEnabledChanged(bool value)
    {
        Preferences.Set("biometric_enabled", value);
        _loggingService.LogInfo($"Biometric enabled: {value}");
    }

    partial void OnSelectedTransactionGroupingChanged(string value)
    {
        Preferences.Set("transaction_grouping", value);
        _loggingService.LogInfo($"Transaction grouping changed to: {value}");
        
        // ✅ Notify TransactionsViewModel to reload grouping
        MessagingCenter.Send(this, "TransactionGroupingChanged", value);
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            _loggingService.LogInfo("Manual database backup requested");

            var appDataPath = FileSystem.AppDataDirectory;
            var backupDir = Path.Combine(appDataPath, "backups");
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"MoneyMind_Backup_{timestamp}");
            Directory.CreateDirectory(backupPath);

            int filesCopied = 0;

            // Backup Global Database
            var globalDbPath = Path.Combine(appDataPath, "MoneyMind_Global.db");
            if (File.Exists(globalDbPath))
            {
                File.Copy(globalDbPath, Path.Combine(backupPath, "MoneyMind_Global.db"), true);
                filesCopied++;
            }

            // Backup all account databases
            var accountDbFiles = Directory.GetFiles(appDataPath, "MoneyMind_Conto_*.db");
            foreach (var dbFile in accountDbFiles)
            {
                var fileName = Path.GetFileName(dbFile);
                File.Copy(dbFile, Path.Combine(backupPath, fileName), true);
                filesCopied++;
            }

            // Create backup info file
            var backupInfo = new
            {
                CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                AppVersion = AppVersion,
                TotalAccounts = TotalAccounts,
                TotalTransactions = TotalTransactions,
                FilesCopied = filesCopied
            };
            var infoJson = Newtonsoft.Json.JsonConvert.SerializeObject(backupInfo, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(Path.Combine(backupPath, "backup_info.json"), infoJson);

            _loggingService.LogInfo($"Backup completed: {filesCopied} files to {backupPath}");

            await Shell.Current.DisplayAlert(
                "✅ Backup Completato",
                $"Backup creato con successo!\n\n" +
                $"📁 Cartella: {backupPath}\n" +
                $"📊 File copiati: {filesCopied}\n" +
                $"📅 Data: {timestamp}\n\n" +
                $"Per copiare su PC:\n" +
                $"adb pull \"{backupPath}\" C:\\Backup\\",
                "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating backup", ex);
            await Shell.Current.DisplayAlert("Errore", $"Impossibile creare backup: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        try
        {
            _loggingService.LogInfo("Database restore requested");

            var appDataPath = FileSystem.AppDataDirectory;
            var backupDir = Path.Combine(appDataPath, "backups");

            if (!Directory.Exists(backupDir))
            {
                await Shell.Current.DisplayAlert(
                    "Nessun Backup",
                    "Non sono presenti backup da ripristinare.\n\nCrea prima un backup dalla sezione Dati.",
                    "OK");
                return;
            }

            // Find available backups
            var backupFolders = Directory.GetDirectories(backupDir)
                .OrderByDescending(d => d)
                .ToList();

            if (backupFolders.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "Nessun Backup",
                    "Non sono presenti backup da ripristinare.",
                    "OK");
                return;
            }

            // Show available backups for selection
            var backupNames = backupFolders.Select(f => Path.GetFileName(f)).ToArray();
            var selectedBackup = await Shell.Current.DisplayActionSheet(
                "Seleziona Backup da Ripristinare",
                "Annulla",
                null,
                backupNames);

            if (string.IsNullOrEmpty(selectedBackup) || selectedBackup == "Annulla")
                return;

            var selectedBackupPath = backupFolders.First(f => Path.GetFileName(f) == selectedBackup);

            // Confirm restore
            bool confirm = await Shell.Current.DisplayAlert(
                "⚠️ Conferma Ripristino",
                $"Vuoi ripristinare il backup:\n\n{selectedBackup}?\n\n" +
                "ATTENZIONE: I dati attuali verranno sovrascritti!",
                "Ripristina",
                "Annulla");

            if (!confirm)
                return;

            int filesRestored = 0;

            // Restore Global Database
            var globalBackupPath = Path.Combine(selectedBackupPath, "MoneyMind_Global.db");
            if (File.Exists(globalBackupPath))
            {
                var globalDbPath = Path.Combine(appDataPath, "MoneyMind_Global.db");
                File.Copy(globalBackupPath, globalDbPath, true);
                filesRestored++;
            }

            // Restore account databases
            var accountBackupFiles = Directory.GetFiles(selectedBackupPath, "MoneyMind_Conto_*.db");
            foreach (var backupFile in accountBackupFiles)
            {
                var fileName = Path.GetFileName(backupFile);
                var destPath = Path.Combine(appDataPath, fileName);
                File.Copy(backupFile, destPath, true);
                filesRestored++;
            }

            _loggingService.LogInfo($"Restore completed: {filesRestored} files from {selectedBackupPath}");

            await Shell.Current.DisplayAlert(
                "✅ Ripristino Completato",
                $"Database ripristinato con successo!\n\n" +
                $"📊 File ripristinati: {filesRestored}\n\n" +
                "⚠️ Riavvia l'app per applicare le modifiche.",
                "OK");

            // Reload statistics
            await LoadDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error restoring database", ex);
            await Shell.Current.DisplayAlert("Errore", $"Impossibile ripristinare database: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma",
                "Vuoi cancellare la cache? L'app verrà riavviata.",
                "Cancella",
                "Annulla");

            if (!confirm)
                return;

            _loggingService.LogInfo("Cache cleared by user");

            // Clear Preferences cache (keep license and important data)
            var licenseEmail = Preferences.Get("license_email", "");
            var licenseStatus = Preferences.Get("license_status", "");
            var currentAccountId = Preferences.Get("current_account_id", 0);

            Preferences.Clear();

            // Restore important data
            Preferences.Set("license_email", licenseEmail);
            Preferences.Set("license_status", licenseStatus);
            Preferences.Set("current_account_id", currentAccountId);
            Preferences.Set("onboarding_completed", true);

            await Shell.Current.DisplayAlert(
                "Successo",
                "Cache cancellata. Riavvia l'app per applicare le modifiche.",
                "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error clearing cache", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile cancellare cache", "OK");
        }
    }

    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        try
        {
            var recentLogs = await _loggingService.GetRecentLogsAsync(50);

            if (recentLogs.Count == 0)
            {
                await Shell.Current.DisplayAlert("Log", "Nessun log disponibile", "OK");
                return;
            }

            var logText = string.Join("\n", recentLogs.Select(log =>
                $"[{log.Timestamp:HH:mm:ss}] [{log.Level}] {log.Message}"));

            // TODO: Creare LogViewerPage dedicata
            await Shell.Current.DisplayAlert(
                "Ultimi Log",
                logText.Length > 1000 ? logText.Substring(0, 1000) + "..." : logText,
                "OK");

            _loggingService.LogInfo("Logs viewed by user");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error viewing logs", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile visualizzare log", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        try
        {
            var logFilePath = await _loggingService.ExportLogsAsync();

            _loggingService.LogInfo($"Logs exported to: {logFilePath}");

            // Show share sheet to let user share the log file immediately
            if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
            {
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Condividi Log MoneyMind",
                    File = new ShareFile(logFilePath)
                });

                _loggingService.LogInfo("Share sheet opened for log file");
            }
            else
            {
                // Fallback: show alert with file path if share fails
                await Shell.Current.DisplayAlert(
                    "Log Esportati",
                    $"Log salvati in Downloads:\n{Path.GetFileName(logFilePath)}\n\n" +
                    $"Puoi trovare il file nella cartella Download del tuo dispositivo.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting/sharing logs", ex);
            await Shell.Current.DisplayAlert("Errore", $"Impossibile esportare log: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task OpenAdminPanelAsync()
    {
        if (!IsAdmin)
        {
            await Shell.Current.DisplayAlert(
                "Accesso Negato",
                "Solo gli amministratori possono accedere al pannello admin.",
                "OK");
            return;
        }

        _loggingService.LogInfo("Admin panel opened");

        await Shell.Current.GoToAsync("admin");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadDatabaseStatsAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task CheckLicenseAsync()
    {
        try
        {
            _loggingService.LogInfo("Manual license check requested");

            var (isValid, message, license) = await _licenseService.CheckLicenseStatusAsync();

            if (license != null)
            {
                CurrentLicense = license;
                LicenseEmail = license.Email;
                LicenseSubscription = license.Subscription;
                LicenseStatus = license.StatusText;
                LicenseExpiration = license.FormattedExpiration;
                
                // ✅ Update admin status based on license
                IsAdmin = license.IsAdmin;
                _loggingService.LogInfo($"Admin status updated: {IsAdmin} (Subscription: {license.Subscription})");
            }

            await Shell.Current.DisplayAlert("Licenza", message, "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error checking license", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile verificare licenza", "OK");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma Logout",
                "Sei sicuro di voler uscire? Dovrai riattivare la licenza.",
                "Logout",
                "Annulla");

            if (!confirm)
                return;

            _licenseService.RevokeLicense();

            LicenseEmail = "Non attivata";
            LicenseSubscription = "N/A";
            LicenseStatus = "Non verificata";
            LicenseExpiration = "N/A";
            CurrentLicense = null;

            _loggingService.LogInfo("User logged out - closing app");

            // 🚫 CHIUDI APP IMMEDIATAMENTE - al riavvio verrà richiesta la licenza
            await Shell.Current.DisplayAlert(
                "Logout Effettuato",
                "Licenza rimossa. L'app verrà chiusa.\n\nAl prossimo avvio dovrai inserire una nuova licenza.",
                "OK");

            // Aspetta che Preferences.Remove() faccia flush su disco
            await Task.Delay(500);

            // Chiudi app - al riavvio OnStart() rileverà licenza mancante
            Application.Current?.Quit();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error logging out", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile effettuare logout", "OK");
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            _loggingService.LogInfo("Manual update check requested");

            var updateInfo = await _updateService.CheckForUpdatesAsync();

            if (updateInfo.IsUpdateAvailable)
            {
                var releaseNotesPreview = updateInfo.ReleaseNotes?.Length > 200
                    ? updateInfo.ReleaseNotes.Substring(0, 200) + "..."
                    : updateInfo.ReleaseNotes ?? "";

                bool download = await Shell.Current.DisplayAlert(
                    "🔔 Aggiornamento Disponibile",
                    $"Versione {updateInfo.LatestVersion} disponibile\n\n" +
                    $"Dimensione: {updateInfo.FormattedFileSize}\n" +
                    $"Rilasciata: {updateInfo.FormattedReleaseDate}\n\n" +
                    $"Note:\n{releaseNotesPreview}",
                    "Scarica e Installa",
                    "Annulla");

                if (download && !string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    await DownloadAndInstallUpdateAsync(updateInfo.DownloadUrl);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "✅ App Aggiornata",
                    updateInfo.ReleaseNotes,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error checking updates", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile verificare aggiornamenti", "OK");
        }
    }

    private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
    {
        try
        {
            _loggingService.LogInfo($"Starting update download: {downloadUrl}");

            // TODO: Mostrare progress dialog (CommunityToolkit.Maui Popup)
            // Per ora usiamo alert semplice
            var progressMessage = "Download in corso...";

            var progress = new Progress<double>(value =>
            {
                var percentage = (int)(value * 100);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update UI se necessario (futura implementazione progress bar)
                    _loggingService.LogDebug($"Download progress: {percentage}%");
                });
            });

            var success = await _updateService.DownloadAndInstallUpdateAsync(downloadUrl, progress);

            if (success)
            {
                await Shell.Current.DisplayAlert(
                    "✅ Download Completato",
                    "L'aggiornamento è pronto per l'installazione.\n\n" +
                    "Tocca 'Installa' nella schermata successiva.",
                    "OK");

                _loggingService.LogInfo("Update download successful, installation started");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "⚠️ Errore",
                    "Impossibile scaricare o installare l'aggiornamento.\n\n" +
                    "Verifica:\n" +
                    "1. Connessione internet\n" +
                    "2. Spazio disponibile\n" +
                    "3. Permessi installazione app",
                    "OK");

                _loggingService.LogWarning("Update download/installation failed");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error downloading/installing update", ex);
            await Shell.Current.DisplayAlert(
                "Errore",
                $"Errore durante l'aggiornamento:\n{ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task OpenWiFiSyncAsync()
    {
        try
        {
            _loggingService.LogInfo("Opening WiFi Sync page");
            await Shell.Current.GoToAsync("wifisync");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error opening WiFi Sync", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile aprire la pagina di sincronizzazione", "OK");
        }
    }

    /// <summary>
    /// RIMOSSO: Easter egg 5-tap per attivare admin
    /// Admin access è ora controllato SOLO dalla licenza
    /// </summary>
    [RelayCommand]
    private async Task ToggleAdminModeAsync()
    {
        // Mostra info sulla licenza corrente invece di attivare admin mode
        if (CurrentLicense == null)
        {
            await Shell.Current.DisplayAlert(
                "Licenza",
                "Nessuna licenza attiva.\n\nAccedi con una licenza Admin per usare il pannello amministratore.",
                "OK");
            return;
        }

        var message = $"Licenza corrente:\n\n" +
                     $"📧 Email: {CurrentLicense.Email}\n" +
                     $"📦 Piano: {CurrentLicense.Subscription}\n" +
                     $"📅 Scadenza: {CurrentLicense.FormattedExpiration}\n" +
                     $"✅ Stato: {CurrentLicense.StatusText}\n\n";

        if (CurrentLicense.IsAdmin)
        {
            message += "👑 Hai accesso al pannello Admin!";
        }
        else
        {
            message += "ℹ️ Solo la licenza Admin può accedere al pannello amministratore.";
        }

        await Shell.Current.DisplayAlert("Info Licenza", message, "OK");
        _loggingService.LogDebug("License info displayed via settings tap");
    }

    private void ApplyTheme(string theme)
    {
        try
        {
            Application.Current!.UserAppTheme = theme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                "Auto" => AppTheme.Unspecified,
                _ => AppTheme.Unspecified
            };

            _loggingService.LogInfo($"Theme applied: {theme}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error applying theme", ex);
        }
    }
}
