using MoneyMindApp.Models;

namespace MoneyMindApp.Services.ImportExport;

public interface IImportExportService
{
    // Import
    Task<List<string[]>> ReadFileAsync(string filePath, bool hasHeader = true, int headerRowNumber = 1);
    Task<List<string>> GetHeadersAsync(string filePath, int headerRowNumber = 1);
    Task<List<ImportPreviewRow>> PreviewImportAsync(string filePath, ColumnMapping mapping, int maxRows = 10);
    Task<ImportResult> ImportTransactionsAsync(string filePath, ColumnMapping mapping);

    /// <summary>
    /// Carica le prime N righe raw del file per anteprima (mostra contenuto grezzo)
    /// Usato per permettere all'utente di vedere le prime righe e scegliere quale contiene l'header
    /// </summary>
    Task<List<FilePreviewRow>> GetFilePreviewAsync(string filePath, int maxRows = 20);

    // Export
    Task<ExportResult> ExportToCsvAsync(List<Transaction> transactions, string filePath, ExportOptions options);
    Task<ExportResult> ExportToExcelAsync(List<Transaction> transactions, string filePath, ExportOptions options);

    // Helpers
    bool IsDuplicate(Transaction newTransaction, List<Transaction> existing);
}
