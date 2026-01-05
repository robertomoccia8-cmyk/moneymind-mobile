using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

[QueryProperty(nameof(FilePath), "FilePath")]
[QueryProperty(nameof(FileName), "FileName")]
[QueryProperty(nameof(HeaderRowNumber), "HeaderRowNumber")]
[QueryProperty(nameof(HasHeader), "HasHeaders")]
[QueryProperty(nameof(Configurazione), "Configurazione")]
[QueryProperty(nameof(ConfigurazioneName), "ConfigurazioneName")]
public partial class ImportViewModel : ObservableObject
{
    private readonly IImportExportService _importExportService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ConfigurazioneImportazione? configurazione;

    [ObservableProperty]
    private string configurazioneName = string.Empty;

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string selectedFileName = string.Empty;

    [ObservableProperty]
    private bool isFileSelected;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<string> availableColumns = new();

    [ObservableProperty]
    private int selectedDataColumn = -1;

    [ObservableProperty]
    private int selectedImportoColumn = -1;

    [ObservableProperty]
    private int selectedDescrizioneColumn = -1;

    [ObservableProperty]
    private int selectedCausaleColumn = -1;

    [ObservableProperty]
    private string selectedDateFormat = "dd/MM/yyyy";

    [ObservableProperty]
    private string selectedDecimalSeparator = ",";

    [ObservableProperty]
    private bool hasHeader = true;

    [ObservableProperty]
    private int headerRowNumber = 1;

    [ObservableProperty]
    private ObservableCollection<ImportPreviewRow> previewRows = new();

    [ObservableProperty]
    private bool canImport;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public List<string> DateFormats { get; } = new()
    {
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "dd-MM-yyyy",
        "dd.MM.yyyy"
    };

    public List<string> DecimalSeparators { get; } = new() { ",", "." };

    public ImportViewModel(IImportExportService importExportService, ILoggingService loggingService)
    {
        _importExportService = importExportService;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Chiamato dalla pagina quando tutti i QueryProperty sono stati impostati
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsFileSelected && AvailableColumns.Count == 0)
        {
            await LoadFileHeadersAsync();
        }
    }

    partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && value != selectedFilePath)
        {
            SelectedFilePath = value;
            SelectedFileName = fileName;
            IsFileSelected = true;

            // NON caricare qui - aspetta InitializeAsync() chiamato da OnAppearing
            // per evitare race condition con HeaderRowNumber
        }
    }

    partial void OnConfigurazioneChanged(ConfigurazioneImportazione? value)
    {
        if (value != null)
        {
            // Pre-compila con valori dalla configurazione
            HeaderRowNumber = value.RigaIntestazione;
            HasHeader = value.HasHeaders;
            SelectedDateFormat = value.FormatoData;
            SelectedDecimalSeparator = value.SeparatoreDecimali;

            // Pre-compila mapping colonne se disponibili
            if (value.MappingColonne.ContainsKey("Data"))
                SelectedDataColumn = value.MappingColonne["Data"];
            if (value.MappingColonne.ContainsKey("Importo"))
                SelectedImportoColumn = value.MappingColonne["Importo"];
            if (value.MappingColonne.ContainsKey("Descrizione"))
                SelectedDescrizioneColumn = value.MappingColonne["Descrizione"];
            if (value.MappingColonne.ContainsKey("Causale"))
                SelectedCausaleColumn = value.MappingColonne["Causale"];

            StatusMessage = $"📋 Configurazione '{value.Nome}' caricata";
            _loggingService.LogInfo($"Applied configuration: {value.Nome}");
        }
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        try
        {
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

            if (result != null)
            {
                SelectedFilePath = result.FullPath;
                SelectedFileName = result.FileName;
                IsFileSelected = true;

                await LoadFileHeadersAsync();
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error selecting file", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    private async Task LoadFileHeadersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Caricamento intestazioni dalla riga {HeaderRowNumber}...";

            var headers = await _importExportService.GetHeadersAsync(SelectedFilePath, HeaderRowNumber);

            AvailableColumns.Clear();
            for (int i = 0; i < headers.Count; i++)
            {
                AvailableColumns.Add($"{i}: {headers[i]}");
            }

            // Auto-detect columns (solo se non già impostati da configurazione)
            if (SelectedDataColumn < 0 && SelectedImportoColumn < 0 && SelectedDescrizioneColumn < 0)
            {
                AutoDetectColumns(headers);
            }

            StatusMessage = $"✅ {headers.Count} colonne trovate dalla riga {HeaderRowNumber}";
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading headers", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AutoDetectColumns(List<string> headers)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].ToLowerInvariant();

            if (header.Contains("data") || header.Contains("date"))
                SelectedDataColumn = i;
            else if (header.Contains("importo") || header.Contains("amount") || header.Contains("valore"))
                SelectedImportoColumn = i;
            else if (header.Contains("descrizione") || header.Contains("description") || header.Contains("causale"))
            {
                if (SelectedDescrizioneColumn < 0)
                    SelectedDescrizioneColumn = i;
                else
                    SelectedCausaleColumn = i;
            }
        }
    }

    [RelayCommand]
    private async Task PreviewImportAsync()
    {
        if (!ValidateMapping()) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Preparazione validazione...";

            var mapping = CreateMapping();

            // Naviga alla pagina di validazione (step 4)
            var navigationParameter = new Dictionary<string, object>
            {
                { "FilePath", SelectedFilePath },
                { "FileName", SelectedFileName },
                { "DataColumn", mapping.DataColumn },
                { "ImportoColumn", mapping.ImportoColumn },
                { "DescrizioneColumn", mapping.DescrizioneColumn },
                { "CausaleColumn", mapping.CausaleColumn },
                { "DateFormat", mapping.DateFormat },
                { "DecimalSeparator", mapping.DecimalSeparator },
                { "HasHeader", mapping.HasHeader },
                { "HeaderRowNumber", mapping.HeaderRowNumber },
                { "SkipToValidation", false }
            };

            // Pass ConfigurazioneName (priorità: stringa nome, altrimenti oggetto.Nome)
            if (!string.IsNullOrWhiteSpace(ConfigurazioneName))
            {
                navigationParameter["ConfigurazioneName"] = ConfigurazioneName;
                _loggingService.LogInfo($"Passing new configuration name: {ConfigurazioneName}");
            }
            else if (Configurazione != null)
            {
                navigationParameter["ConfigurazioneName"] = Configurazione.Nome;
                _loggingService.LogInfo($"Passing existing configuration name: {Configurazione.Nome}");
            }

            _loggingService.LogInfo($"Navigating to validation page for {SelectedFileName}");

            await Shell.Current.GoToAsync("importValidation", navigationParameter);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error navigating to validation", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (!ValidateMapping()) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Importazione in corso...";

            var mapping = CreateMapping();
            var result = await _importExportService.ImportTransactionsAsync(SelectedFilePath, mapping);

            StatusMessage = result.Message;

            if (result.Success)
            {
                await Shell.Current.DisplayAlert(
                    "✅ Import Completato",
                    $"Importate: {result.ImportedCount}\n" +
                    $"Saltate: {result.SkippedCount}\n" +
                    $"Duplicati: {result.DuplicateCount}\n" +
                    $"Errori: {result.ErrorCount}",
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("❌ Errore", string.Join("\n", result.Errors.Take(5)), "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error importing", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool ValidateMapping()
    {
        if (SelectedDataColumn < 0)
        {
            StatusMessage = "❌ Seleziona la colonna Data";
            return false;
        }
        if (SelectedImportoColumn < 0)
        {
            StatusMessage = "❌ Seleziona la colonna Importo";
            return false;
        }
        if (SelectedDescrizioneColumn < 0)
        {
            StatusMessage = "❌ Seleziona la colonna Descrizione";
            return false;
        }
        return true;
    }

    private ColumnMapping CreateMapping() => new()
    {
        DataColumn = SelectedDataColumn,
        ImportoColumn = SelectedImportoColumn,
        DescrizioneColumn = SelectedDescrizioneColumn,
        CausaleColumn = SelectedCausaleColumn,
        DateFormat = SelectedDateFormat,
        DecimalSeparator = SelectedDecimalSeparator,
        HasHeader = HasHeader,
        HeaderRowNumber = HeaderRowNumber
    };

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
