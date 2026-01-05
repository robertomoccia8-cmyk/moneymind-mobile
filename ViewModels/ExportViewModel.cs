using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IImportExportService _importExportService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private DateTime startDate = DateTime.Now.AddMonths(-1);

    [ObservableProperty]
    private DateTime endDate = DateTime.Now;

    [ObservableProperty]
    private int selectedFormatIndex = 0;

    [ObservableProperty]
    private bool includeHeader = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private int transactionCount;

    [ObservableProperty]
    private ObservableCollection<Transaction> previewTransactions = new();

    public List<string> ExportFormats { get; } = new() { "CSV", "Excel" };

    public ExportViewModel(
        IImportExportService importExportService,
        DatabaseService databaseService,
        ILoggingService loggingService)
    {
        _importExportService = importExportService;
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    public async Task InitializeAsync()
    {
        await LoadPreviewAsync();
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Caricamento transazioni...";

            var transactions = await _databaseService.GetTransactionsAsync(StartDate, EndDate);
            TransactionCount = transactions.Count;

            PreviewTransactions.Clear();
            foreach (var t in transactions.Take(10))
            {
                PreviewTransactions.Add(t);
            }

            StatusMessage = $"✅ {TransactionCount} transazioni da esportare";
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading preview", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnStartDateChanged(DateTime value) => _ = LoadPreviewAsync();
    partial void OnEndDateChanged(DateTime value) => _ = LoadPreviewAsync();

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Esportazione in corso...";

            var transactions = await _databaseService.GetTransactionsAsync(StartDate, EndDate);

            if (transactions.Count == 0)
            {
                await Shell.Current.DisplayAlert("Attenzione", "Nessuna transazione da esportare nel periodo selezionato.", "OK");
                return;
            }

            var options = new ExportOptions
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Format = SelectedFormatIndex == 0 ? ExportFormat.Csv : ExportFormat.Excel,
                IncludeHeader = IncludeHeader
            };

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = SelectedFormatIndex == 0 ? "csv" : "csv"; // Both CSV for now
            var fileName = $"MoneyMind_Export_{timestamp}.{extension}";

            // Save to public Download folder
            string filePath;
#if ANDROID
            var downloadPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            if (!string.IsNullOrEmpty(downloadPath))
            {
                filePath = Path.Combine(downloadPath, fileName);
            }
            else
            {
                // Fallback to app data
                filePath = Path.Combine(FileSystem.AppDataDirectory, "exports", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }
#else
            // For other platforms, use app data directory
            filePath = Path.Combine(FileSystem.AppDataDirectory, "exports", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
#endif

            ExportResult result;
            if (SelectedFormatIndex == 0)
            {
                result = await _importExportService.ExportToCsvAsync(transactions, filePath, options);
            }
            else
            {
                result = await _importExportService.ExportToExcelAsync(transactions, filePath, options);
            }

            if (result.Success)
            {
                StatusMessage = result.Message;

                await Shell.Current.DisplayAlert(
                    "✅ Export Completato",
                    $"Esportate {result.ExportedCount} transazioni.\n\n" +
                    $"File salvato in:\n{result.FilePath}",
                    "OK");

                // Torna alla schermata Transazioni
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("❌ Errore", "Errore durante l'export", "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task TryShareFileAsync(string filePath)
    {
        try
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Esporta Transazioni",
                File = new ShareFile(filePath)
            });
        }
        catch
        {
            // Share not supported on all platforms
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
