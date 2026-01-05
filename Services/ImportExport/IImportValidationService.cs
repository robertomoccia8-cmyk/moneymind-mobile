using MoneyMindApp.Models;

namespace MoneyMindApp.Services.ImportExport;

/// <summary>
/// Service per validare e importare transazioni da file CSV/Excel
/// Step 4 del wizard: validation + import finale
/// </summary>
public interface IImportValidationService
{
    /// <summary>
    /// Valida tutte le righe del file prima dell'import
    /// Ritorna lista con flag validazione (verde/rosso) per ogni riga
    /// </summary>
    /// <param name="filePath">Path del file da validare</param>
    /// <param name="mapping">Mapping colonne (include HeaderRowNumber)</param>
    /// <param name="maxRows">Numero massimo righe da validare (default 1000)</param>
    /// <returns>Lista righe validate con flag errori</returns>
    Task<List<TransactionValidationRow>> ValidateFileAsync(
        string filePath,
        ColumnMapping mapping,
        int maxRows = 1000);

    /// <summary>
    /// Importa solo le righe valide nel database
    /// Skippa automaticamente righe con errori
    /// </summary>
    /// <param name="filePath">Path del file da importare</param>
    /// <param name="mapping">Mapping colonne</param>
    /// <param name="validRows">Lista righe validate (solo quelle senza errori verranno importate)</param>
    /// <returns>Risultato import con statistiche</returns>
    Task<ImportResult> ImportValidRowsAsync(
        string filePath,
        ColumnMapping mapping,
        List<TransactionValidationRow> validRows);
}
