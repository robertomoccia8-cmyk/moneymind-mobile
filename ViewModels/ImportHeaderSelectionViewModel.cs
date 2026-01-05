using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel per la selezione del file e della riga header
/// Step 2 del wizard: seleziona file, visualizza prime 20 righe, scegli riga header
/// </summary>
[QueryProperty(nameof(Configurazione), "Configurazione")]
[QueryProperty(nameof(ConfigurazioneName), "ConfigurazioneName")]
public partial class ImportHeaderSelectionViewModel : ObservableObject
{
    private readonly IImportExportService _importExportService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ConfigurazioneImportazione? configurazione;

    [ObservableProperty]
    private string configurazioneName = string.Empty;

    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string selectedFileName = string.Empty;

    [ObservableProperty]
    private bool isFileSelected;

    [ObservableProperty]
    private ObservableCollection<FilePreviewRow> filePreviewRows = new();

    [ObservableProperty]
    private int headerRowNumber = 1;

    [ObservableProperty]
    private bool hasHeaders = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool canProceed;

    public ImportHeaderSelectionViewModel(
        IImportExportService importExportService,
        ILoggingService loggingService)
    {
        _importExportService = importExportService;
        _loggingService = loggingService;
    }

    partial void OnConfigurazioneChanged(ConfigurazioneImportazione? value)
    {
        if (value != null)
        {
            // Pre-compila con valori dalla configurazione
            HeaderRowNumber = value.RigaIntestazione;
            HasHeaders = value.HasHeaders;
            StatusMessage = $"📋 Usando configurazione: {value.Nome}";
            _loggingService.LogInfo($"Loaded configuration: {value.Nome}");
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

                await LoadFilePreviewAsync();
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error selecting file", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    private async Task LoadFilePreviewAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Caricamento anteprima...";

            var preview = await _importExportService.GetFilePreviewAsync(SelectedFilePath, 20);

            FilePreviewRows.Clear();
            foreach (var row in preview)
            {
                FilePreviewRows.Add(row);
            }

            StatusMessage = $"✅ {preview.Count} righe caricate. Seleziona la riga che contiene le intestazioni.";
            CanProceed = true;

            _loggingService.LogInfo($"Loaded {preview.Count} preview rows from {SelectedFileName}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading file preview", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
            CanProceed = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectPreviewRow(FilePreviewRow row)
    {
        // Deseleziona tutte
        foreach (var r in FilePreviewRows)
        {
            r.IsSelected = false;
        }

        // Seleziona la riga cliccata
        row.IsSelected = true;
        HeaderRowNumber = row.RowNumber;

        StatusMessage = $"✅ Selezionata riga {HeaderRowNumber} come intestazione";
    }

    [RelayCommand]
    private async Task ProceedToMappingAsync()
    {
        if (!CanProceed)
        {
            StatusMessage = "❌ Seleziona prima un file";
            return;
        }

        if (HeaderRowNumber < 1 || HeaderRowNumber > FilePreviewRows.Count)
        {
            await Shell.Current.DisplayAlert(
                "⚠️ Attenzione",
                $"Numero riga non valido. Deve essere tra 1 e {FilePreviewRows.Count}",
                "OK");
            return;
        }

        try
        {
            // Naviga allo step di mapping colonne
            var navigationParameter = new Dictionary<string, object>
            {
                { "FilePath", SelectedFilePath },
                { "FileName", SelectedFileName },
                { "HeaderRowNumber", HeaderRowNumber },
                { "HasHeaders", HasHeaders }
            };

            if (Configurazione != null)
            {
                navigationParameter["Configurazione"] = Configurazione;
            }

            // Pass ConfigurazioneName if we're creating a new configuration
            if (!string.IsNullOrWhiteSpace(ConfigurazioneName))
            {
                navigationParameter["ConfigurazioneName"] = ConfigurazioneName;
            }

            await Shell.Current.GoToAsync("importColumnMapping", navigationParameter);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error navigating to column mapping", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
