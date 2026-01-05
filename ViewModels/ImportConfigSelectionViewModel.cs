using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel per la selezione/gestione delle configurazioni di importazione
/// Step 1 del wizard: scegli configurazione esistente o crea nuova
/// </summary>
public partial class ImportConfigSelectionViewModel : ObservableObject
{
    private readonly IConfigurazioneImportazioneService _configService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ObservableCollection<ConfigurazioneImportazione> configurazioni = new();

    [ObservableProperty]
    private ConfigurazioneImportazione? configurazioneSelezionata;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasConfigurations;

    public ImportConfigSelectionViewModel(
        IConfigurazioneImportazioneService configService,
        ILoggingService loggingService)
    {
        _configService = configService;
        _loggingService = loggingService;
    }

    public async Task InitializeAsync()
    {
        await LoadConfigurazioniAsync();
    }

    [RelayCommand]
    private async Task LoadConfigurazioniAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Caricamento configurazioni...";

            // Load user configurations only (no presets)
            var configs = await _configService.GetConfigurazioniAsync();

            Configurazioni.Clear();
            foreach (var config in configs)
            {
                Configurazioni.Add(config);
            }

            HasConfigurations = Configurazioni.Count > 0;
            StatusMessage = $"✅ {Configurazioni.Count} configurazioni trovate";

            _loggingService.LogInfo($"Loaded {Configurazioni.Count} configurations");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading configurations", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Seleziona una configurazione dalla lista (senza navigare)
    /// </summary>
    [RelayCommand]
    private void SelectConfiguration(ConfigurazioneImportazione config)
    {
        ConfigurazioneSelezionata = config;
        StatusMessage = $"✅ Configurazione '{config.Nome}' selezionata. Usa i pulsanti sopra per caricarla, modificarla o eliminarla.";
        _loggingService.LogInfo($"Selected configuration: {config.Nome}");
    }

    /// <summary>
    /// Carica configurazione selezionata e salta direttamente allo step 4 (validazione)
    /// Implementa logica "Carica Selezionata" del desktop
    /// </summary>
    [RelayCommand]
    private async Task LoadSelectedConfigurationAsync()
    {
        if (ConfigurazioneSelezionata == null)
        {
            await Shell.Current.DisplayAlert(
                "⚠️ Attenzione",
                "Seleziona prima una configurazione dall'elenco.",
                "OK");
            return;
        }

        try
        {
            // Seleziona file da importare
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "com.microsoft.excel.xls" } },
                { DevicePlatform.WinUI, new[] { ".csv", ".xls", ".xlsx" } }
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona file CSV o Excel",
                FileTypes = customFileType
            });

            if (result == null) return;

            // Aggiorna ultimo utilizzo
            await _configService.AggiornaUltimoUtilizzoAsync(ConfigurazioneSelezionata.Nome);

            // 🔍 DEBUG: Check MappingColonne content
            var mappingInfo = string.Join(", ", ConfigurazioneSelezionata.MappingColonne.Select(kv => $"{kv.Key}={kv.Value}"));
            _loggingService.LogInfo($"[DEBUG] Config '{ConfigurazioneSelezionata.Nome}' - MappingColonne count: {ConfigurazioneSelezionata.MappingColonne.Count}, Content: [{mappingInfo}]");

            // Naviga DIRETTAMENTE allo step 4 (validazione) saltando step 2 e 3
            var navigationParameter = new Dictionary<string, object>
            {
                { "FilePath", result.FullPath },
                { "FileName", result.FileName },
                { "ConfigurazioneName", ConfigurazioneSelezionata.Nome },
                { "DataColumn", ConfigurazioneSelezionata.MappingColonne.ContainsKey("Data") ? ConfigurazioneSelezionata.MappingColonne["Data"] : -1 },
                { "ImportoColumn", ConfigurazioneSelezionata.MappingColonne.ContainsKey("Importo") ? ConfigurazioneSelezionata.MappingColonne["Importo"] : -1 },
                { "DescrizioneColumn", ConfigurazioneSelezionata.MappingColonne.ContainsKey("Descrizione") ? ConfigurazioneSelezionata.MappingColonne["Descrizione"] : -1 },
                { "CausaleColumn", ConfigurazioneSelezionata.MappingColonne.ContainsKey("Causale") ? ConfigurazioneSelezionata.MappingColonne["Causale"] : -1 },
                { "DateFormat", ConfigurazioneSelezionata.FormatoData },
                { "DecimalSeparator", ConfigurazioneSelezionata.SeparatoreDecimali },
                { "HasHeader", ConfigurazioneSelezionata.HasHeaders },
                { "HeaderRowNumber", ConfigurazioneSelezionata.RigaIntestazione },
                { "SkipToValidation", true }
            };

            _loggingService.LogInfo($"Loading configuration '{ConfigurazioneSelezionata.Nome}' and skipping to validation");

            await Shell.Current.GoToAsync("importValidation", navigationParameter);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading selected configuration", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    /// <summary>
    /// Modifica configurazione selezionata - naviga a column mapping con file picker
    /// </summary>
    [RelayCommand]
    private async Task EditConfigurationAsync()
    {
        if (ConfigurazioneSelezionata == null)
        {
            await Shell.Current.DisplayAlert(
                "⚠️ Attenzione",
                "Seleziona prima una configurazione dall'elenco.",
                "OK");
            return;
        }

        // Non permettere modifica preset
        if (ConfigurazioneSelezionata.IsPreset)
        {
            await Shell.Current.DisplayAlert(
                "⚠️ Attenzione",
                "Non puoi modificare una configurazione preset. Creane una nuova basata su questa.",
                "OK");
            return;
        }

        try
        {
            // Seleziona file da importare
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "com.microsoft.excel.xls" } },
                { DevicePlatform.WinUI, new[] { ".csv", ".xls", ".xlsx" } }
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona file CSV o Excel",
                FileTypes = customFileType
            });

            if (result == null) return;

            // Naviga allo step 3 (column mapping) con configurazione caricata
            var navigationParameter = new Dictionary<string, object>
            {
                { "FilePath", result.FullPath },
                { "FileName", result.FileName },
                { "HeaderRowNumber", ConfigurazioneSelezionata.RigaIntestazione },
                { "HasHeaders", ConfigurazioneSelezionata.HasHeaders },
                { "Configurazione", ConfigurazioneSelezionata }
            };

            _loggingService.LogInfo($"Editing configuration '{ConfigurazioneSelezionata.Nome}'");

            await Shell.Current.GoToAsync("importColumnMapping", navigationParameter);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error editing configuration", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateNewConfigurationAsync()
    {
        try
        {
            // Prompt for configuration name
            var configName = await Shell.Current.DisplayPromptAsync(
                "Nuova Configurazione",
                "Inserisci un nome per la nuova configurazione:",
                "Crea",
                "Annulla",
                placeholder: "Es: La Mia Banca CSV");

            if (string.IsNullOrWhiteSpace(configName))
            {
                _loggingService.LogInfo("Configuration creation cancelled - no name provided");
                return;
            }

            // Check if configuration with this name already exists
            var existing = await _configService.GetConfigurazioneAsync(configName);
            if (existing != null)
            {
                await Shell.Current.DisplayAlert(
                    "Nome Duplicato",
                    $"Esiste già una configurazione con nome '{configName}'. Scegli un altro nome.",
                    "OK");
                return;
            }

            // Navigate to header selection with configuration name
            await Shell.Current.GoToAsync($"importHeaderSelection?ConfigurazioneName={Uri.EscapeDataString(configName)}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error navigating to file selection", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteConfigurationAsync(ConfigurazioneImportazione config)
    {
        try
        {
            // Non eliminare preset
            if (config.IsPreset)
            {
                await Shell.Current.DisplayAlert(
                    "⚠️ Attenzione",
                    "Non puoi eliminare una configurazione preset.",
                    "OK");
                return;
            }

            var confirm = await Shell.Current.DisplayAlert(
                "Conferma Eliminazione",
                $"Sei sicuro di voler eliminare '{config.Nome}'?",
                "Elimina",
                "Annulla");

            if (confirm)
            {
                await _configService.EliminaConfigurazioneAsync(config.Nome);
                Configurazioni.Remove(config);
                HasConfigurations = Configurazioni.Count > 0;

                StatusMessage = $"✅ Configurazione '{config.Nome}' eliminata";
                _loggingService.LogInfo($"Deleted configuration: {config.Nome}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error deleting configuration '{config.Nome}'", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
