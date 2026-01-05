using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel per lo step 4 del wizard: validazione transazioni pre-import
/// Mostra preview verde/rosso di tutte le righe e permette import finale
/// </summary>
[QueryProperty(nameof(FilePath), "FilePath")]
[QueryProperty(nameof(FileName), "FileName")]
[QueryProperty(nameof(ConfigurazioneName), "ConfigurazioneName")]
[QueryProperty(nameof(DataColumn), "DataColumn")]
[QueryProperty(nameof(ImportoColumn), "ImportoColumn")]
[QueryProperty(nameof(DescrizioneColumn), "DescrizioneColumn")]
[QueryProperty(nameof(CausaleColumn), "CausaleColumn")]
[QueryProperty(nameof(DateFormat), "DateFormat")]
[QueryProperty(nameof(DecimalSeparator), "DecimalSeparator")]
[QueryProperty(nameof(HasHeader), "HasHeader")]
[QueryProperty(nameof(HeaderRowNumber), "HeaderRowNumber")]
[QueryProperty(nameof(SkipToValidation), "SkipToValidation")]
public partial class ImportValidationViewModel : ObservableObject
{
    private readonly IImportValidationService _validationService;
    private readonly IConfigurazioneImportazioneService _configService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private string configurazioneName = string.Empty;

    [ObservableProperty]
    private int dataColumn = -1;

    [ObservableProperty]
    private int importoColumn = -1;

    [ObservableProperty]
    private int descrizioneColumn = -1;

    [ObservableProperty]
    private int causaleColumn = -1;

    [ObservableProperty]
    private string dateFormat = "dd/MM/yyyy";

    [ObservableProperty]
    private string decimalSeparator = ",";

    [ObservableProperty]
    private bool hasHeader = true;

    [ObservableProperty]
    private int headerRowNumber = 1;

    [ObservableProperty]
    private bool skipToValidation;

    [ObservableProperty]
    private ObservableCollection<TransactionValidationRow> validationRows = new();

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int validCount;

    [ObservableProperty]
    private int errorCount;

    [ObservableProperty]
    private decimal validPercentage;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool canImport;

    [ObservableProperty]
    private bool validationCompleted;

    public ImportValidationViewModel(
        IImportValidationService validationService,
        IConfigurazioneImportazioneService configService,
        ILoggingService loggingService)
    {
        _validationService = validationService;
        _configService = configService;
        _loggingService = loggingService;
    }

    // 🔍 DEBUG: Log when DataColumn is set
    partial void OnDataColumnChanged(int value)
    {
        _loggingService.LogInfo($"[DEBUG] ImportValidationViewModel - DataColumn set to: {value}");
    }

    /// <summary>
    /// Chiamato quando i parametri di navigazione sono impostati
    /// </summary>
    public async Task InitializeAsync()
    {
        // ✅ FIX: Wait for Query Properties to be set before validating
        // If DataColumn is still -1, parameters haven't been set yet
        if (!string.IsNullOrEmpty(FilePath) && !ValidationCompleted && DataColumn >= 0)
        {
            await LoadValidationAsync();
        }
        else if (DataColumn < 0)
        {
            _loggingService.LogWarning("InitializeAsync called but DataColumn not set yet - skipping validation");
        }
    }

    partial void OnFilePathChanged(string value)
    {
        // ✅ FIX: Same race condition fix - wait for DataColumn to be set
        if (!string.IsNullOrEmpty(value) && !ValidationCompleted && DataColumn >= 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadValidationAsync();
            });
        }
        else if (!string.IsNullOrEmpty(value) && DataColumn < 0)
        {
            _loggingService.LogWarning("OnFilePathChanged triggered but DataColumn not set yet - skipping validation");
        }
    }

    [RelayCommand]
    private async Task LoadValidationAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Validazione righe in corso...";
            ValidationCompleted = false;

            // 🔍 DEBUG: Log parameters at start of validation
            _loggingService.LogInfo($"[DEBUG] LoadValidationAsync START - DataColumn={DataColumn}, ImportoColumn={ImportoColumn}, DescrizioneColumn={DescrizioneColumn}");
            _loggingService.LogInfo($"Starting validation of {FileName} with mapping: Data={DataColumn}, Importo={ImportoColumn}, Descrizione={DescrizioneColumn}");

            // Crea mapping dalle proprietà
            var mapping = new ColumnMapping
            {
                DataColumn = DataColumn,
                ImportoColumn = ImportoColumn,
                DescrizioneColumn = DescrizioneColumn,
                CausaleColumn = CausaleColumn,
                DateFormat = DateFormat,
                DecimalSeparator = DecimalSeparator,
                HasHeader = HasHeader,
                HeaderRowNumber = HeaderRowNumber
            };

            // Valida tutte le righe (max 1000)
            var results = await _validationService.ValidateFileAsync(FilePath, mapping, 1000);

            // Aggiorna UI
            ValidationRows.Clear();
            foreach (var row in results)
            {
                ValidationRows.Add(row);
            }

            // Calcola statistiche
            TotalCount = results.Count;
            ValidCount = results.Count(r => !r.HasErrors);
            ErrorCount = results.Count(r => r.HasErrors);
            ValidPercentage = TotalCount > 0 ? (decimal)ValidCount / TotalCount * 100 : 0;

            CanImport = ValidCount > 0;
            ValidationCompleted = true;

            StatusMessage = $"✅ Validazione completata: {ValidCount}/{TotalCount} righe valide ({ValidPercentage:F1}%)";

            _loggingService.LogInfo($"Validation completed: {ValidCount} valid, {ErrorCount} errors out of {TotalCount} total");

            // Log warning se troppe righe con errori
            if (ErrorCount > ValidCount)
            {
                _loggingService.LogWarning($"More errors ({ErrorCount}) than valid rows ({ValidCount}) - check column mapping!");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during validation", ex);
            StatusMessage = $"❌ Errore durante la validazione: {ex.Message}";
            CanImport = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (!CanImport)
        {
            await Shell.Current.DisplayAlert(
                "⚠️ Attenzione",
                "Nessuna riga valida da importare. Verifica il mapping delle colonne.",
                "OK");
            return;
        }

        // Conferma import
        var confirm = await Shell.Current.DisplayAlert(
            "Conferma Importazione",
            $"Importare {ValidCount} transazioni valide?\n\n" +
            $"Le {ErrorCount} righe con errori verranno saltate.",
            "Importa",
            "Annulla");

        if (!confirm) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Importazione in corso...";

            // Crea mapping
            var mapping = new ColumnMapping
            {
                DataColumn = DataColumn,
                ImportoColumn = ImportoColumn,
                DescrizioneColumn = DescrizioneColumn,
                CausaleColumn = CausaleColumn,
                DateFormat = DateFormat,
                DecimalSeparator = DecimalSeparator,
                HasHeader = HasHeader,
                HeaderRowNumber = HeaderRowNumber
            };

            // Importa solo righe valide
            var result = await _validationService.ImportValidRowsAsync(
                FilePath,
                mapping,
                ValidationRows.ToList());

            StatusMessage = result.Message;

            // ✅ Check if it's a "successful skip" (all duplicates) vs real errors
            bool allDuplicates = result.ImportedCount == 0 && result.DuplicateCount > 0 && result.ErrorCount == 0;

            if (result.Success || allDuplicates)
            {
                _loggingService.LogInfo($"Import completed: {result.ImportedCount} imported, {result.SkippedCount} skipped, {result.DuplicateCount} duplicates");

                // ✅ Save configuration if we have a name (new configuration)
                if (!string.IsNullOrWhiteSpace(ConfigurazioneName))
                {
                    try
                    {
                        // Create ColumnMapping from current wizard settings
                        var configMapping = new ColumnMapping
                        {
                            DataColumn = DataColumn,
                            ImportoColumn = ImportoColumn,
                            DescrizioneColumn = DescrizioneColumn,
                            CausaleColumn = CausaleColumn,
                            DateFormat = DateFormat,
                            DecimalSeparator = DecimalSeparator,
                            HasHeader = HasHeader,
                            HeaderRowNumber = HeaderRowNumber
                        };

                        // Create configuration from mapping
                        var config = ConfigurazioneImportazione.FromColumnMapping(ConfigurazioneName, configMapping);
                        config.IsPreset = false;

                        await _configService.SalvaConfigurazioneAsync(config);
                        _loggingService.LogInfo($"Saved new configuration: {ConfigurazioneName}");
                    }
                    catch (Exception saveEx)
                    {
                        _loggingService.LogError($"Error saving configuration {ConfigurazioneName}", saveEx);
                        // Don't fail import if config save fails
                    }
                }

                string title;
                string message;

                if (allDuplicates)
                {
                    // All transactions were duplicates - show info message
                    title = "ℹ️ Transazioni Già Presenti";
                    message = $"Tutte le {result.TotalRows} transazioni sono già presenti nel database.\n\n" +
                              $"✅ Nessun duplicato importato (comportamento corretto)";
                }
                else
                {
                    // Normal success
                    title = "✅ Import Completato";
                    message = $"Importate: {result.ImportedCount}\n" +
                              $"Saltate: {result.SkippedCount}\n" +
                              $"Duplicati: {result.DuplicateCount}\n" +
                              $"Errori: {result.ErrorCount}";
                }

                await Shell.Current.DisplayAlert(title, message, "OK");

                // Navigate to transactions tab with absolute reset to clear wizard stack
                await Shell.Current.GoToAsync("///transactions");
            }
            else
            {
                _loggingService.LogError($"Import failed: {string.Join(", ", result.Errors)}", null);

                await Shell.Current.DisplayAlert(
                    "❌ Errore Import",
                    $"📊 Statistiche:\n" +
                    $"Totale: {result.TotalRows}\n" +
                    $"Importate: {result.ImportedCount}\n" +
                    $"Saltate: {result.SkippedCount}\n" +
                    $"Duplicati: {result.DuplicateCount}\n" +
                    $"Errori: {result.ErrorCount}\n\n" +
                    $"❌ Dettagli errori:\n" +
                    $"{string.Join("\n", result.Errors.Take(10))}" +
                    (result.Errors.Count > 10 ? $"\n...e altri {result.Errors.Count - 10} errori" : ""),
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during import", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";

            await Shell.Current.DisplayAlert(
                "❌ Errore Exception",
                $"Si è verificato un errore durante l'importazione:\n\n" +
                $"Tipo: {ex.GetType().Name}\n" +
                $"Messaggio: {ex.Message}\n\n" +
                $"Stack Trace (prime righe):\n{string.Join("\n", ex.StackTrace?.Split('\n').Take(5) ?? new[] { "N/A" })}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackToMappingAsync()
    {
        // Torna allo step di mapping colonne
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Annulla Importazione",
            "Sei sicuro di voler annullare l'importazione?",
            "Sì",
            "No");

        if (confirm)
        {
            await Shell.Current.GoToAsync("///transactions");
        }
    }

    /// <summary>
    /// Filtra solo righe con errori (per debug)
    /// </summary>
    [RelayCommand]
    private void ShowErrorsOnly()
    {
        var errors = ValidationRows.Where(r => r.HasErrors).ToList();

        if (errors.Count == 0)
        {
            StatusMessage = "✅ Nessun errore trovato!";
            return;
        }

        // Temporaneamente mostra solo errori
        ValidationRows.Clear();
        foreach (var error in errors)
        {
            ValidationRows.Add(error);
        }

        StatusMessage = $"Mostrando {errors.Count} righe con errori";
    }

    /// <summary>
    /// Filtra solo righe valide (per review)
    /// </summary>
    [RelayCommand]
    private void ShowValidOnly()
    {
        var valid = ValidationRows.Where(r => !r.HasErrors).ToList();

        if (valid.Count == 0)
        {
            StatusMessage = "❌ Nessuna riga valida trovata!";
            return;
        }

        // Temporaneamente mostra solo valide
        ValidationRows.Clear();
        foreach (var row in valid)
        {
            ValidationRows.Add(row);
        }

        StatusMessage = $"Mostrando {valid.Count} righe valide";
    }

    /// <summary>
    /// Ripristina tutte le righe
    /// </summary>
    [RelayCommand]
    private async Task ShowAllRowsAsync()
    {
        await LoadValidationAsync();
    }
}
