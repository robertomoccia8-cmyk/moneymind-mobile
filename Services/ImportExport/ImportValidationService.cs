using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;

namespace MoneyMindApp.Services.ImportExport;

/// <summary>
/// Service per validare e importare transazioni
/// Implementa step 4 del wizard: validation verde/rosso + import finale
/// </summary>
public class ImportValidationService : IImportValidationService
{
    private readonly IImportExportService _importExportService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    public ImportValidationService(
        IImportExportService importExportService,
        DatabaseService databaseService,
        ILoggingService loggingService)
    {
        _importExportService = importExportService;
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    public async Task<List<TransactionValidationRow>> ValidateFileAsync(
        string filePath,
        ColumnMapping mapping,
        int maxRows = 1000)
    {
        var validationRows = new List<TransactionValidationRow>();

        try
        {
            _loggingService.LogInfo($"Starting validation of {filePath} with max {maxRows} rows");

            // 1. Leggi file con header row custom (usa logica esistente)
            var rows = await _importExportService.ReadFileAsync(
                filePath,
                mapping.HasHeader,
                mapping.HeaderRowNumber);

            if (rows.Count == 0)
            {
                _loggingService.LogWarning("No data rows found in file");
                return validationRows;
            }

            // 2. Calcola numero riga iniziale nel file originale
            var rowNumber = mapping.HasHeader ? mapping.HeaderRowNumber + 1 : 1;

            // 3. Valida ogni riga (max maxRows)
            foreach (var row in rows.Take(maxRows))
            {
                var validationRow = new TransactionValidationRow
                {
                    RowNumber = rowNumber
                };

                try
                {
                    // Valida Data
                    if (!ValidateData(row, mapping, validationRow))
                    {
                        validationRows.Add(validationRow);
                        rowNumber++;
                        continue;
                    }

                    // Valida Importo
                    if (!ValidateImporto(row, mapping, validationRow))
                    {
                        validationRows.Add(validationRow);
                        rowNumber++;
                        continue;
                    }

                    // Valida Descrizione
                    if (!ValidateDescrizione(row, mapping, validationRow))
                    {
                        validationRows.Add(validationRow);
                        rowNumber++;
                        continue;
                    }

                    // Opzionale: Causale
                    if (mapping.CausaleColumn >= 0 && mapping.CausaleColumn < row.Length)
                    {
                        validationRow.Causale = row[mapping.CausaleColumn].Trim();
                    }

                    // Riga valida!
                    validationRow.HasErrors = false;
                }
                catch (Exception ex)
                {
                    validationRow.HasErrors = true;
                    validationRow.ErrorMessage = $"Errore parsing: {ex.Message}";
                    _loggingService.LogWarning($"Error validating row {rowNumber}: {ex.Message}");
                }

                validationRows.Add(validationRow);
                rowNumber++;
            }

            // ✅ Check for duplicates against existing transactions
            try
            {
                // Initialize database with current account
                var currentAccountId = Preferences.Get("current_account_id", 0);
                if (currentAccountId > 0)
                {
                    await _databaseService.InitializeAsync(currentAccountId);
                    var existingTransactions = await _databaseService.GetAllTransactionsAsync();

                    // Check each valid row for duplicates
                    foreach (var validRow in validationRows.Where(r => !r.HasErrors))
                    {
                        // Create a transaction from the validation row
                        try
                        {
                            var transaction = new Transaction
                            {
                                Data = DateTime.ParseExact(validRow.Data, mapping.DateFormat, System.Globalization.CultureInfo.InvariantCulture),
                                Importo = ParseImporto(validRow.Importo, mapping.DecimalSeparator),
                                Descrizione = validRow.Descrizione.Trim(),
                                Causale = validRow.Causale?.Trim()
                            };

                            // Check if duplicate
                            if (_importExportService.IsDuplicate(transaction, existingTransactions))
                            {
                                validRow.IsDuplicate = true;
                                validRow.ErrorMessage = "Duplicato - già presente nel database";
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error checking duplicate for row {validRow.RowNumber}: {ex.Message}");
                        }
                    }

                    var duplicateCount = validationRows.Count(r => r.IsDuplicate);
                    _loggingService.LogInfo($"Duplicate check completed: {duplicateCount} duplicates found");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Could not check for duplicates: {ex.Message}");
                // Continue without duplicate checking
            }

            var validCount = validationRows.Count(r => !r.HasErrors);
            var errorCount = validationRows.Count(r => r.HasErrors);

            _loggingService.LogInfo($"Validation completed: {validCount} valid, {errorCount} errors, total {validationRows.Count}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during file validation", ex);
            throw;
        }

        return validationRows;
    }

    public async Task<ImportResult> ImportValidRowsAsync(
        string filePath,
        ColumnMapping mapping,
        List<TransactionValidationRow> validRows)
    {
        var result = new ImportResult
        {
            TotalRows = validRows.Count
        };

        try
        {
            _loggingService.LogInfo($"Starting import of {validRows.Count} validated rows");

            // Filtra solo righe valide (senza errori)
            var rowsToImport = validRows.Where(r => !r.HasErrors).ToList();

            if (rowsToImport.Count == 0)
            {
                result.Success = false;
                result.Errors.Add("Nessuna riga valida da importare");
                _loggingService.LogWarning("No valid rows to import");
                return result;
            }

            // ✅ Initialize DatabaseService with current account
            var currentAccountId = Preferences.Get("current_account_id", 0);
            if (currentAccountId == 0)
            {
                result.Success = false;
                result.Errors.Add("Nessun account selezionato. Seleziona un account prima di importare.");
                _loggingService.LogError("No account selected for import", null);
                return result;
            }

            _loggingService.LogInfo($"Initializing database for account {currentAccountId}");
            await _databaseService.InitializeAsync(currentAccountId);

            // Carica transazioni esistenti per check duplicati
            var existingTransactions = await _databaseService.GetAllTransactionsAsync();

            foreach (var validRow in rowsToImport)
            {
                try
                {
                    // Parse dati validati
                    var transaction = new Transaction
                    {
                        Data = DateTime.ParseExact(validRow.Data, mapping.DateFormat,
                            CultureInfo.InvariantCulture),
                        Importo = ParseImporto(validRow.Importo, mapping.DecimalSeparator),
                        Descrizione = validRow.Descrizione.Trim(),
                        Causale = validRow.Causale?.Trim()
                    };

                    // Check duplicati (usa logica esistente)
                    if (_importExportService.IsDuplicate(transaction, existingTransactions))
                    {
                        result.DuplicateCount++;
                        result.SkippedCount++;
                        _loggingService.LogInfo($"Row {validRow.RowNumber} skipped (duplicate)");
                        continue;
                    }

                    // Insert nel database
                    await _databaseService.InsertTransactionAsync(transaction);
                    existingTransactions.Add(transaction);
                    result.ImportedCount++;

                    _loggingService.LogInfo($"Row {validRow.RowNumber} imported successfully");
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Riga {validRow.RowNumber}: {ex.Message}");
                    _loggingService.LogError($"Error importing row {validRow.RowNumber}", ex);
                }
            }

            result.Success = result.ImportedCount > 0;

            _loggingService.LogInfo($"Import completed: {result.ImportedCount} imported, " +
                $"{result.SkippedCount} skipped, {result.ErrorCount} errors");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during import", ex);
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    #region Validation Helpers

    /// <summary>
    /// Valida campo Data
    /// </summary>
    private bool ValidateData(string[] row, ColumnMapping mapping, TransactionValidationRow validationRow)
    {
        if (mapping.DataColumn < 0 || mapping.DataColumn >= row.Length)
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = $"Riga {validationRow.RowNumber}: Colonna Data non trovata - Indice richiesto: {mapping.DataColumn}, Colonne disponibili: {row.Length} (Contenuto: {string.Join("|", row)})";
            _loggingService.LogWarning($"Row {validationRow.RowNumber}: Expected DataColumn at index {mapping.DataColumn}, but row only has {row.Length} columns. Row content: [{string.Join(", ", row)}]");
            return false;
        }

        var dataStr = row[mapping.DataColumn]?.Trim();

        if (string.IsNullOrWhiteSpace(dataStr))
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = "Data mancante";
            return false;
        }

        // Prova parsing con formato specificato
        if (!DateTime.TryParseExact(dataStr, mapping.DateFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            // Fallback: prova parsing generico
            if (!DateTime.TryParse(dataStr, out _))
            {
                validationRow.HasErrors = true;
                validationRow.ErrorMessage = $"Formato data non valido: '{dataStr}'";
                return false;
            }
        }

        validationRow.Data = dataStr;
        return true;
    }

    /// <summary>
    /// Valida campo Importo
    /// </summary>
    private bool ValidateImporto(string[] row, ColumnMapping mapping, TransactionValidationRow validationRow)
    {
        if (mapping.ImportoColumn < 0 || mapping.ImportoColumn >= row.Length)
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = "Colonna Importo non trovata";
            return false;
        }

        var importoStr = row[mapping.ImportoColumn]?.Trim();

        if (string.IsNullOrWhiteSpace(importoStr))
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = "Importo mancante";
            return false;
        }

        // Rimuovi simboli comuni
        var cleanImporto = importoStr.Replace("€", "").Replace("$", "").Replace(" ", "").Trim();

        // Prova parsing (usa logica robusta)
        try
        {
            var parsed = ParseImporto(cleanImporto, mapping.DecimalSeparator);
            validationRow.Importo = importoStr;  // Salva valore originale
            return true;
        }
        catch
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = $"Formato importo non valido: '{importoStr}'";
            return false;
        }
    }

    /// <summary>
    /// Valida campo Descrizione
    /// </summary>
    private bool ValidateDescrizione(string[] row, ColumnMapping mapping, TransactionValidationRow validationRow)
    {
        if (mapping.DescrizioneColumn < 0 || mapping.DescrizioneColumn >= row.Length)
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = "Colonna Descrizione non trovata";
            return false;
        }

        var descrizioneStr = row[mapping.DescrizioneColumn]?.Trim();

        if (string.IsNullOrWhiteSpace(descrizioneStr))
        {
            validationRow.HasErrors = true;
            validationRow.ErrorMessage = "Descrizione mancante";
            return false;
        }

        validationRow.Descrizione = descrizioneStr;
        return true;
    }

    /// <summary>
    /// Parse importo con supporto formati IT/USA
    /// Riutilizza logica NormalizeDecimalString da ImportExportService
    /// </summary>
    private decimal ParseImporto(string importoStr, string decimalSeparator)
    {
        // Rimuovi simboli
        var clean = importoStr.Replace("€", "").Replace("$", "").Replace(" ", "").Trim();

        // Normalizza formato
        var normalized = NormalizeDecimalString(clean, decimalSeparator);

        // Parse
        if (!decimal.TryParse(normalized, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"Cannot parse '{importoStr}' as decimal");
        }

        return result;
    }

    /// <summary>
    /// Normalizza string decimale (supporta IT/USA format)
    /// DUPLICATO da ImportExportService per evitare dipendenza circolare
    /// </summary>
    private string NormalizeDecimalString(string input, string hintDecimalSeparator)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0";

        int dotCount = input.Count(c => c == '.');
        int commaCount = input.Count(c => c == ',');
        int lastDotPos = input.LastIndexOf('.');
        int lastCommaPos = input.LastIndexOf(',');

        // Se hint è fornito e chiaro, usalo
        if (!string.IsNullOrEmpty(hintDecimalSeparator))
        {
            if (hintDecimalSeparator == "," && commaCount > 0)
            {
                // Formato italiano: 1.500,00 → 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
            else if (hintDecimalSeparator == "." && dotCount > 0)
            {
                // Formato USA: 1,500.00 → 1500.00
                return input.Replace(",", "");
            }
        }

        // Auto-detect basato su posizione
        if (dotCount == 0 && commaCount == 0)
        {
            return input;  // Nessun separatore: "1500"
        }
        else if (dotCount == 1 && commaCount == 0)
        {
            int digitsAfterDot = input.Length - lastDotPos - 1;
            if (digitsAfterDot <= 2)
                return input;  // Decimale: "1500.00"
            else
                return input.Replace(".", "");  // Migliaia: "1.500" → "1500"
        }
        else if (commaCount == 1 && dotCount == 0)
        {
            int digitsAfterComma = input.Length - lastCommaPos - 1;
            if (digitsAfterComma <= 2)
                return input.Replace(",", ".");  // Decimale: "1500,00" → "1500.00"
            else
                return input.Replace(",", "");  // Migliaia: "1,500" → "1500"
        }
        else if (dotCount >= 1 && commaCount == 1)
        {
            if (lastCommaPos > lastDotPos)
            {
                // Italiano: 1.500,00 → 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // USA: 1,500.00 → 1500.00
                return input.Replace(",", "");
            }
        }
        else if (commaCount >= 1 && dotCount == 1)
        {
            if (lastDotPos > lastCommaPos)
            {
                // USA: 1,500.00 → 1500.00
                return input.Replace(",", "");
            }
            else
            {
                // Italiano: 1.500,00 → 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
        }
        else if (dotCount > 1)
        {
            // Multiple dots: migliaia
            return input.Replace(".", "").Replace(",", ".");
        }
        else if (commaCount > 1)
        {
            // Multiple commas: migliaia
            return input.Replace(",", "");
        }

        // Fallback
        return input.Replace(",", ".");
    }

    #endregion
}
