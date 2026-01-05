using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Services.Sync;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.Backup;
using MoneyMindApp.Models.Sync;

namespace MoneyMindApp.ViewModels;

public partial class WiFiSyncViewModel : ObservableObject
{
    private readonly IWiFiSyncService _wifiSyncService;
    private readonly ILoggingService _loggingService;
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private bool isServerRunning;

    [ObservableProperty]
    private string serverStatus = "Server non attivo";

    [ObservableProperty]
    private string? ipAddress;

    [ObservableProperty]
    private int port = 8765;

    [ObservableProperty]
    private string connectionUrl = "";

    [ObservableProperty]
    private string lastSyncTime = "Mai sincronizzato";

    [ObservableProperty]
    private string lastSyncDirection = "";

    [ObservableProperty]
    private int transactionsReceived;

    [ObservableProperty]
    private int transactionsSent;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool showInstructions = true;

    [ObservableProperty]
    private List<BackupInfo> recentBackups = new();

    [ObservableProperty]
    private bool hasBackups;

    public WiFiSyncViewModel(
        IWiFiSyncService wifiSyncService,
        ILoggingService loggingService,
        IBackupService backupService)
    {
        _wifiSyncService = wifiSyncService;
        _loggingService = loggingService;
        _backupService = backupService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Caricamento...";

            // Load sync statistics
            var stats = await _wifiSyncService.GetSyncStatisticsAsync();
            if (stats.LastSyncTime.HasValue)
            {
                LastSyncTime = stats.LastSyncTime.Value.ToString("dd/MM/yyyy HH:mm");
                ShowInstructions = false;
            }
            TransactionsReceived = stats.TransactionsReceived;
            TransactionsSent = stats.TransactionsSent;

            // Load last sync direction
            var direction = Preferences.Get("last_sync_direction", "");
            if (!string.IsNullOrEmpty(direction))
            {
                LastSyncDirection = direction == "DesktopToMobile"
                    ? "Desktop → Mobile"
                    : "Mobile → Desktop";
            }

            // Check current server status
            IsServerRunning = _wifiSyncService.IsServerRunning;
            if (IsServerRunning)
            {
                IpAddress = await _wifiSyncService.GetDeviceIPAddressAsync();
                UpdateConnectionUrl();
                ServerStatus = "🟢 Server attivo";
            }
            else
            {
                ServerStatus = "⚪ Server non attivo";
            }

            // Load recent backups
            await LoadBackupsAsync();

            StatusMessage = "";
            _loggingService.LogInfo("WiFi Sync page initialized");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing WiFi Sync page", ex);
            StatusMessage = $"Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleServerAsync()
    {
        try
        {
            IsLoading = true;

            if (IsServerRunning)
            {
                StatusMessage = "Arresto server...";
                await _wifiSyncService.StopServerAsync();
                IsServerRunning = false;
                ServerStatus = "⚪ Server non attivo";
                ConnectionUrl = "";
                IpAddress = null;
                StatusMessage = "Server arrestato";
            }
            else
            {
                StatusMessage = "Avvio server...";

                // Get IP first
                IpAddress = await _wifiSyncService.GetDeviceIPAddressAsync();

                if (string.IsNullOrEmpty(IpAddress))
                {
                    StatusMessage = "⚠️ Impossibile ottenere indirizzo IP. Verifica connessione WiFi o Hotspot.";
                    await Shell.Current.DisplayAlert(
                        "Errore Connessione",
                        "Impossibile ottenere l'indirizzo IP del dispositivo.\n\n" +
                        "Verifica che:\n" +
                        "• Il dispositivo sia connesso a una rete WiFi, oppure\n" +
                        "• L'Hotspot mobile sia attivo",
                        "OK");
                    return;
                }

                var success = await _wifiSyncService.StartServerAsync(Port);

                if (success)
                {
                    IsServerRunning = true;
                    ServerStatus = "🟢 Server attivo";
                    UpdateConnectionUrl();
                    StatusMessage = "Server avviato con successo!";

                    _loggingService.LogInfo($"WiFi Sync server started at {ConnectionUrl}");
                }
                else
                {
                    StatusMessage = "❌ Errore avvio server";
                    await Shell.Current.DisplayAlert(
                        "Errore",
                        "Impossibile avviare il server di sincronizzazione.\n\n" +
                        "Verifica che la porta non sia già in uso.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error toggling server", ex);
            StatusMessage = $"Errore: {ex.Message}";
            await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CopyConnectionUrlAsync()
    {
        if (string.IsNullOrEmpty(ConnectionUrl))
        {
            await Shell.Current.DisplayAlert("Info", "Avvia prima il server", "OK");
            return;
        }

        await Clipboard.SetTextAsync(ConnectionUrl);
        StatusMessage = "URL copiato negli appunti!";

        _loggingService.LogInfo("Connection URL copied to clipboard");
    }

    [RelayCommand]
    private async Task CreateManualBackupAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creazione backup...";

            var result = await _backupService.CreateBackupAsync("manual");

            if (result.Success)
            {
                StatusMessage = $"Backup creato: {result.FilesBackedUp.Count} file ({result.TotalSizeBytes / 1024.0:F1} KB)";
                await LoadBackupsAsync();

                await Shell.Current.DisplayAlert(
                    "✅ Backup Creato",
                    $"Backup creato con successo!\n\n" +
                    $"📁 File: {result.FilesBackedUp.Count}\n" +
                    $"📊 Dimensione: {result.TotalSizeBytes / 1024.0:F1} KB",
                    "OK");
            }
            else
            {
                StatusMessage = $"Errore backup: {result.Error}";
                await Shell.Current.DisplayAlert("Errore", $"Impossibile creare backup: {result.Error}", "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating backup", ex);
            StatusMessage = $"Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (RecentBackups.Count == 0)
        {
            await Shell.Current.DisplayAlert("Info", "Nessun backup disponibile", "OK");
            return;
        }

        var backupNames = RecentBackups
            .Select(b => $"{b.CreatedAt:dd/MM/yyyy HH:mm} - {b.Reason}")
            .ToArray();

        var selected = await Shell.Current.DisplayActionSheet(
            "Seleziona Backup",
            "Annulla",
            null,
            backupNames);

        if (string.IsNullOrEmpty(selected) || selected == "Annulla")
            return;

        var selectedIndex = Array.IndexOf(backupNames, selected);
        if (selectedIndex < 0 || selectedIndex >= RecentBackups.Count)
            return;

        var selectedBackup = RecentBackups[selectedIndex];

        var confirm = await Shell.Current.DisplayAlert(
            "⚠️ Conferma Ripristino",
            $"Vuoi ripristinare il backup del {selectedBackup.CreatedAt:dd/MM/yyyy HH:mm}?\n\n" +
            "ATTENZIONE: I dati attuali verranno sovrascritti!",
            "Ripristina",
            "Annulla");

        if (!confirm)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Ripristino backup...";

            // Find backup folder path
            var backupBasePath = _backupService.GetBackupBasePath();
            var backupFolder = Path.Combine(backupBasePath,
                $"MoneyMind_Backup_{selectedBackup.CreatedAt:yyyyMMdd_HHmmss}");

            // Try alternative folder naming
            if (!Directory.Exists(backupFolder))
            {
                var folders = Directory.GetDirectories(backupBasePath)
                    .OrderByDescending(d => d)
                    .ToList();

                if (selectedIndex < folders.Count)
                {
                    backupFolder = folders[selectedIndex];
                }
            }

            var success = await _backupService.RestoreBackupAsync(backupFolder);

            if (success)
            {
                StatusMessage = "Backup ripristinato!";
                await Shell.Current.DisplayAlert(
                    "✅ Ripristino Completato",
                    "Backup ripristinato con successo!\n\n⚠️ Riavvia l'app per applicare le modifiche.",
                    "OK");
            }
            else
            {
                StatusMessage = "Errore ripristino";
                await Shell.Current.DisplayAlert("Errore", "Impossibile ripristinare il backup", "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error restoring backup", ex);
            StatusMessage = $"Errore: {ex.Message}";
            await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowHelpAsync()
    {
        await Shell.Current.DisplayAlert(
            "📱 Come Sincronizzare",
            "1. CONNETTI entrambi i dispositivi alla stessa rete WiFi\n" +
            "   (oppure attiva Hotspot sul telefono e connetti il PC)\n\n" +
            "2. AVVIA il server su questa app\n\n" +
            "3. APRI MoneyMind Desktop e vai su:\n" +
            "   Impostazioni → Sincronizzazione WiFi\n\n" +
            "4. INSERISCI l'IP mostrato qui e premi Connetti\n\n" +
            "5. SCEGLI direzione e modalità di sync:\n" +
            "   • Sostituisci: cancella tutto e copia\n" +
            "   • Unisci: aggiunge solo non-duplicati\n" +
            "   • Solo nuove: aggiunge solo transazioni più recenti\n\n" +
            "⚠️ Viene creato un backup automatico prima di ogni sync!",
            "OK");
    }

    private async Task LoadBackupsAsync()
    {
        try
        {
            RecentBackups = await _backupService.GetBackupsAsync();
            HasBackups = RecentBackups.Count > 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading backups", ex);
            RecentBackups = new List<BackupInfo>();
            HasBackups = false;
        }
    }

    private void UpdateConnectionUrl()
    {
        if (!string.IsNullOrEmpty(IpAddress))
        {
            ConnectionUrl = $"http://{IpAddress}:{Port}";
        }
    }
}
