using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;
using System.Text;
using System.Data;
using ExcelDataReader;

namespace MoneyMindApp.Services.ImportExport;

public class ImportExportService : IImportExportService
{
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    public ImportExportService(DatabaseService databaseService, ILoggingService loggingService)
    {
        _databaseService = databaseService;
        _loggingService = loggingService;

        // Register encoding provider for ExcelDataReader (required for .xls files)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    #region Import

    public async Task<List<string>> GetHeadersAsync(string filePath, int headerRowNumber = 1)
    {
        var headers = new List<string>();

        try
        {
            // First, try to read as CSV/text (works for many bank "Excel" files)
            try
            {
                _loggingService.LogInfo($"Attempting to read headers from row {headerRowNumber} as CSV from {Path.GetFileName(filePath)}");

                var allLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

                // Verifica che la riga header esista
                if (headerRowNumber > 0 && headerRowNumber <= allLines.Length)
                {
                    var headerLine = allLines[headerRowNumber - 1]; // 1-based → 0-based
                    var separator = DetectSeparator(headerLine);
                    headers = ParseCsvLine(headerLine, separator).ToList();

                    if (headers.Count > 0)
                    {
                        _loggingService.LogInfo($"✅ Successfully read {headers.Count} headers from row {headerRowNumber} as CSV");
                        return headers;
                    }
                }
                else
                {
                    _loggingService.LogWarning($"Header row {headerRowNumber} is out of range (file has {allLines.Length} lines)");
                }
            }
            catch (Exception csvEx)
            {
                _loggingService.LogWarning($"CSV read failed: {csvEx.Message}. Trying Excel reader...");
            }

            // Fallback: Try to read as Excel (.xls/.xlsx)
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".xls" || extension == ".xlsx")
            {
                _loggingService.LogInfo($"Attempting to read headers from row {headerRowNumber} as Excel from {Path.GetFileName(filePath)}");
                headers = await GetHeadersFromExcelAsync(filePath, headerRowNumber);

                if (headers.Count > 0)
                {
                    _loggingService.LogInfo($"✅ Successfully read {headers.Count} headers from row {headerRowNumber} from Excel");
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error reading headers (all methods failed)", ex);
        }

        return headers;
    }

    public async Task<List<string[]>> ReadFileAsync(string filePath, bool hasHeader = true, int headerRowNumber = 1)
    {
        var rows = new List<string[]>();

        try
        {
            // First, try to read as CSV/text
            try
            {
                _loggingService.LogInfo($"Attempting to read file as CSV: {Path.GetFileName(filePath)} (header row: {headerRowNumber})");

                var allLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

                if (allLines.Length == 0)
                {
                    _loggingService.LogWarning("File is empty");
                    return rows;
                }

                // Detect separator from header line
                var headerLine = headerRowNumber > 0 && headerRowNumber <= allLines.Length
                    ? allLines[headerRowNumber - 1]
                    : allLines[0];
                var separator = DetectSeparator(headerLine);

                // Determina da quale riga iniziare a leggere i dati
                // Se hasHeader=true e headerRowNumber=10, i dati partono da riga 11 (indice 10)
                int startDataRow = hasHeader ? headerRowNumber : 0; // 0-based index

                _loggingService.LogInfo($"Reading data starting from row {startDataRow + 1} (0-based index: {startDataRow})");

                for (int i = startDataRow; i < allLines.Length; i++)
                {
                    var line = allLines[i];
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var parsedRow = ParseCsvLine(line, separator);

                        // Skip empty rows (only separators)
                        if (parsedRow.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                        {
                            rows.Add(parsedRow);
                        }
                    }
                }

                if (rows.Count > 0)
                {
                    _loggingService.LogInfo($"✅ Successfully read {rows.Count} data rows as CSV (skipped first {startDataRow} rows)");
                    return rows;
                }
            }
            catch (Exception csvEx)
            {
                _loggingService.LogWarning($"CSV read failed: {csvEx.Message}. Trying Excel reader...");
            }

            // Fallback: Try to read as Excel (.xls/.xlsx)
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".xls" || extension == ".xlsx")
            {
                _loggingService.LogInfo($"Attempting to read file as Excel: {Path.GetFileName(filePath)}");
                rows = await ReadExcelFileAsync(filePath, hasHeader, headerRowNumber);

                if (rows.Count > 0)
                {
                    _loggingService.LogInfo($"✅ Successfully read {rows.Count} rows from Excel");
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error reading file (all methods failed)", ex);
        }

        return rows;
    }

    /// <summary>
    /// Carica le prime N righe raw del file per anteprima (mostra contenuto grezzo con numero riga)
    /// Usato per permettere all'utente di vedere le prime righe e scegliere quale contiene l'header
    /// </summary>
    public async Task<List<FilePreviewRow>> GetFilePreviewAsync(string filePath, int maxRows = 20)
    {
        var preview = new List<FilePreviewRow>();

        try
        {
            _loggingService.LogInfo($"Loading preview of first {maxRows} rows from {Path.GetFileName(filePath)}");

            // Try CSV first
            try
            {
                var allLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
                var linesToRead = Math.Min(maxRows, allLines.Length);

                for (int i = 0; i < linesToRead; i++)
                {
                    preview.Add(new FilePreviewRow
                    {
                        RowNumber = i + 1,
                        Content = allLines[i]
                    });
                }

                if (preview.Count > 0)
                {
                    _loggingService.LogInfo($"✅ Loaded {preview.Count} preview rows as CSV");
                    return preview;
                }
            }
            catch (Exception csvEx)
            {
                _loggingService.LogWarning($"CSV preview failed: {csvEx.Message}. Trying Excel...");
            }

            // Fallback: Excel
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".xls" || extension == ".xlsx")
            {
                preview = await GetExcelPreviewAsync(filePath, maxRows);
                _loggingService.LogInfo($"✅ Loaded {preview.Count} preview rows from Excel");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading file preview", ex);
        }

        return preview;
    }

    public async Task<List<ImportPreviewRow>> PreviewImportAsync(string filePath, ColumnMapping mapping, int maxRows = 10)
    {
        var preview = new List<ImportPreviewRow>();

        try
        {
            var rows = await ReadFileAsync(filePath, mapping.HasHeader, mapping.HeaderRowNumber);
            var existingTransactions = await _databaseService.GetAllTransactionsAsync();

            // Row number nel file originale inizia da HeaderRowNumber + 1
            var rowNumber = mapping.HasHeader ? mapping.HeaderRowNumber + 1 : 1;
            foreach (var row in rows.Take(maxRows))
            {
                var previewRow = ParseRow(row, mapping, rowNumber);

                // Check duplicate
                if (previewRow.IsValid && previewRow.Data.HasValue && previewRow.Importo.HasValue)
                {
                    var tempTransaction = new Transaction
                    {
                        Data = previewRow.Data.Value,
                        Importo = previewRow.Importo.Value,
                        Descrizione = previewRow.Descrizione
                    };
                    previewRow.IsDuplicate = IsDuplicate(tempTransaction, existingTransactions);
                }

                preview.Add(previewRow);
                rowNumber++;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error previewing import", ex);
        }

        return preview;
    }

    public async Task<ImportResult> ImportTransactionsAsync(string filePath, ColumnMapping mapping)
    {
        var result = new ImportResult();

        try
        {
            _loggingService.LogInfo($"Starting import from {Path.GetFileName(filePath)} (header row: {mapping.HeaderRowNumber})");

            var rows = await ReadFileAsync(filePath, mapping.HasHeader, mapping.HeaderRowNumber);
            result.TotalRows = rows.Count;

            var existingTransactions = await _databaseService.GetAllTransactionsAsync();

            // Row number nel file originale inizia da HeaderRowNumber + 1
            var rowNumber = mapping.HasHeader ? mapping.HeaderRowNumber + 1 : 1;
            foreach (var row in rows)
            {
                var previewRow = ParseRow(row, mapping, rowNumber);

                if (!previewRow.IsValid)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Riga {rowNumber}: {previewRow.ErrorMessage}");
                    rowNumber++;
                    continue;
                }

                var transaction = new Transaction
                {
                    Data = previewRow.Data!.Value,
                    Importo = previewRow.Importo!.Value,
                    Descrizione = previewRow.Descrizione,
                    Causale = previewRow.Causale
                };

                // Check duplicate
                if (IsDuplicate(transaction, existingTransactions))
                {
                    result.DuplicateCount++;
                    result.SkippedCount++;
                    rowNumber++;
                    continue;
                }

                // Insert
                await _databaseService.InsertTransactionAsync(transaction);
                existingTransactions.Add(transaction);
                result.ImportedCount++;
                rowNumber++;
            }

            result.Success = result.ErrorCount == 0 || result.ImportedCount > 0;
            _loggingService.LogInfo($"Import completed: {result.ImportedCount} imported, {result.SkippedCount} skipped, {result.ErrorCount} errors");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error importing transactions", ex);
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private ImportPreviewRow ParseRow(string[] row, ColumnMapping mapping, int rowNumber)
    {
        var preview = new ImportPreviewRow { RowNumber = rowNumber };

        try
        {
            // Parse Data
            if (mapping.DataColumn >= 0 && mapping.DataColumn < row.Length)
            {
                var dateStr = row[mapping.DataColumn];
                if (DateTime.TryParseExact(dateStr, mapping.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    preview.Data = date;
                }
                else if (DateTime.TryParse(dateStr, out date))
                {
                    preview.Data = date;
                }
                else
                {
                    preview.ErrorMessage = $"Data non valida: {dateStr}";
                    return preview;
                }
            }

            // Parse Importo
            if (mapping.ImportoColumn >= 0 && mapping.ImportoColumn < row.Length)
            {
                var importoStr = row[mapping.ImportoColumn]
                    .Replace("€", "")
                    .Replace("$", "")
                    .Replace(" ", "")
                    .Trim();

                // Smart decimal separator detection and handling
                importoStr = NormalizeDecimalString(importoStr, mapping.DecimalSeparator);

                if (decimal.TryParse(importoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var importo))
                {
                    preview.Importo = importo;
                }
                else
                {
                    preview.ErrorMessage = $"Importo non valido: {row[mapping.ImportoColumn]}";
                    return preview;
                }
            }

            // Parse Descrizione
            if (mapping.DescrizioneColumn >= 0 && mapping.DescrizioneColumn < row.Length)
            {
                preview.Descrizione = row[mapping.DescrizioneColumn].Trim();
            }

            // Parse Causale (opzionale)
            if (mapping.CausaleColumn >= 0 && mapping.CausaleColumn < row.Length)
            {
                preview.Causale = row[mapping.CausaleColumn].Trim();
            }

            preview.IsValid = preview.Data.HasValue && preview.Importo.HasValue && !string.IsNullOrEmpty(preview.Descrizione);
        }
        catch (Exception ex)
        {
            preview.ErrorMessage = ex.Message;
        }

        return preview;
    }

    #endregion

    #region Export

    public async Task<ExportResult> ExportToCsvAsync(List<Transaction> transactions, string filePath, ExportOptions options)
    {
        var result = new ExportResult { FilePath = filePath };

        try
        {
            _loggingService.LogInfo($"Exporting {transactions.Count} transactions to CSV");

            var sb = new StringBuilder();

            // Header
            if (options.IncludeHeader)
            {
                sb.AppendLine(string.Join(";", options.Columns));
            }

            // Data rows
            foreach (var t in transactions)
            {
                var values = new List<string>();

                foreach (var col in options.Columns)
                {
                    var value = col switch
                    {
                        "Data" => t.Data.ToString("dd/MM/yyyy"),
                        "Importo" => t.Importo.ToString("F2", CultureInfo.InvariantCulture),
                        "Descrizione" => EscapeCsv(t.Descrizione),
                        "Causale" => EscapeCsv(t.Causale ?? ""),
                        _ => ""
                    };
                    values.Add(value);
                }

                sb.AppendLine(string.Join(";", values));
                result.ExportedCount++;
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            result.Success = true;

            _loggingService.LogInfo($"CSV export completed: {filePath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting to CSV", ex);
            result.Success = false;
        }

        return result;
    }

    public async Task<ExportResult> ExportToExcelAsync(List<Transaction> transactions, string filePath, ExportOptions options)
    {
        // For now, export as CSV with .xlsx extension (simplified)
        // Full Excel support would require EPPlus NuGet package
        var result = new ExportResult { FilePath = filePath };

        try
        {
            _loggingService.LogInfo($"Exporting {transactions.Count} transactions to Excel (CSV format)");

            // Use CSV format but with Excel-compatible encoding
            var csvPath = filePath.Replace(".xlsx", ".csv");
            result = await ExportToCsvAsync(transactions, csvPath, options);
            result.FilePath = csvPath;

            _loggingService.LogInfo($"Excel export completed (as CSV): {csvPath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error exporting to Excel", ex);
            result.Success = false;
        }

        return result;
    }

    #endregion

    #region Excel Import Helpers

    /// <summary>
    /// Reads headers from Excel file (.xls or .xlsx)
    /// </summary>
    private async Task<List<string>> GetHeadersFromExcelAsync(string filePath, int headerRowNumber = 1)
    {
        var headers = new List<string>();

        await Task.Run(() =>
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);

                // Auto-detect format based on file extension
                using var reader = Path.GetExtension(filePath).ToLowerInvariant() == ".xls"
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false // We'll manually extract row
                    }
                });

                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    var headerRowIndex = headerRowNumber - 1; // 1-based → 0-based

                    if (headerRowIndex >= 0 && headerRowIndex < table.Rows.Count)
                    {
                        var headerRow = table.Rows[headerRowIndex];
                        for (int i = 0; i < headerRow.ItemArray.Length; i++)
                        {
                            headers.Add(headerRow.ItemArray[i]?.ToString() ?? $"Column{i}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading Excel headers: {ex.Message}", ex);
            }
        });

        return headers;
    }

    /// <summary>
    /// Reads all rows from Excel file (.xls or .xlsx)
    /// </summary>
    private async Task<List<string[]>> ReadExcelFileAsync(string filePath, bool hasHeader = true, int headerRowNumber = 1)
    {
        var rows = new List<string[]>();

        await Task.Run(() =>
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);

                // Auto-detect format based on file extension
                using var reader = Path.GetExtension(filePath).ToLowerInvariant() == ".xls"
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false // We'll handle headers manually
                    }
                });

                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    // Se hasHeader=true e headerRowNumber=10, i dati partono da riga 11 (indice 10)
                    var startRow = hasHeader ? headerRowNumber : 0; // 0-based index

                    _loggingService.LogInfo($"Reading Excel data starting from row {startRow + 1} (0-based index: {startRow})");

                    for (int i = startRow; i < table.Rows.Count; i++)
                    {
                        var row = table.Rows[i];
                        var cells = new string[row.ItemArray.Length];

                        for (int j = 0; j < row.ItemArray.Length; j++)
                        {
                            cells[j] = row.ItemArray[j]?.ToString() ?? "";
                        }

                        // Skip empty rows
                        if (cells.Any(c => !string.IsNullOrWhiteSpace(c)))
                        {
                            rows.Add(cells);
                        }
                    }

                    _loggingService.LogInfo($"Read {rows.Count} data rows from Excel (skipped first {startRow} rows)");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading Excel file: {ex.Message}", ex);
            }
        });

        return rows;
    }

    /// <summary>
    /// Reads first N rows from Excel for preview
    /// </summary>
    private async Task<List<FilePreviewRow>> GetExcelPreviewAsync(string filePath, int maxRows = 20)
    {
        var preview = new List<FilePreviewRow>();

        await Task.Run(() =>
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);

                using var reader = Path.GetExtension(filePath).ToLowerInvariant() == ".xls"
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    var rowsToRead = Math.Min(maxRows, table.Rows.Count);

                    for (int i = 0; i < rowsToRead; i++)
                    {
                        var row = table.Rows[i];
                        var content = string.Join(";", row.ItemArray.Select(cell => cell?.ToString() ?? ""));

                        preview.Add(new FilePreviewRow
                        {
                            RowNumber = i + 1,
                            Content = content
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading Excel preview: {ex.Message}", ex);
            }
        });

        return preview;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Parse CSV line respecting quoted fields (handles separators inside quotes)
    /// Example: "field1","field,with,commas","field3" -> ["field1", "field,with,commas", "field3"]
    /// </summary>
    private string[] ParseCsvLine(string line, char separator)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Handle double quotes "" as escaped quote
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == separator && !insideQuotes)
            {
                // End of field
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // Add last field
        fields.Add(currentField.ToString().Trim());

        return fields.ToArray();
    }

    /// <summary>
    /// Verifica se la transazione è un duplicato ESATTO (100% match)
    /// Criteri: stessa data + importo identico + descrizione identica
    /// </summary>
    public bool IsDuplicate(Transaction newTransaction, List<Transaction> existing)
    {
        foreach (var t in existing)
        {
            // 1. Stessa data
            if (t.Data.Date != newTransaction.Data.Date)
                continue;

            // 2. Importo IDENTICO (no tolleranza)
            if (t.Importo != newTransaction.Importo)
                continue;

            // 3. Descrizione IDENTICA (case-insensitive, trimmed)
            var desc1 = (t.Descrizione ?? "").Trim();
            var desc2 = (newTransaction.Descrizione ?? "").Trim();

            if (!desc1.Equals(desc2, StringComparison.OrdinalIgnoreCase))
                continue;

            // 4. Causale IDENTICA (se presente in entrambe)
            var caus1 = (t.Causale ?? "").Trim();
            var caus2 = (newTransaction.Causale ?? "").Trim();

            // Se entrambe hanno causale, devono essere identiche
            if (!string.IsNullOrEmpty(caus1) && !string.IsNullOrEmpty(caus2))
            {
                if (!caus1.Equals(caus2, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // Duplicato esatto trovato
            return true;
        }

        return false;
    }

    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();

        if (s1 == s2) return 1.0;

        var distance = LevenshteinDistance(s1, s2);
        var maxLen = Math.Max(s1.Length, s2.Length);

        return 1.0 - (double)distance / maxLen;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }

    /// <summary>
    /// Normalizes a decimal string to use '.' as decimal separator for parsing
    /// Supports all common formats: 1500.00, 1500,00, 1.500,00, 1,500.00
    /// </summary>
    private string NormalizeDecimalString(string input, string hintDecimalSeparator = "")
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0";

        // Count occurrences of . and ,
        int dotCount = input.Count(c => c == '.');
        int commaCount = input.Count(c => c == ',');
        int lastDotPos = input.LastIndexOf('.');
        int lastCommaPos = input.LastIndexOf(',');

        // If hint is provided and clear, use it
        if (!string.IsNullOrEmpty(hintDecimalSeparator))
        {
            if (hintDecimalSeparator == "," && commaCount > 0)
            {
                // Italian format: 1.500,00 -> 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
            else if (hintDecimalSeparator == "." && dotCount > 0)
            {
                // US format: 1,500.00 -> 1500.00
                return input.Replace(",", "");
            }
        }

        // Auto-detect based on position and count
        // Rule: the last separator with 1-2 digits after it is likely the decimal separator

        if (dotCount == 0 && commaCount == 0)
        {
            // No separators: "1500" -> "1500"
            return input;
        }
        else if (dotCount == 1 && commaCount == 0)
        {
            // Only dot: "1500.00" or "1.500" (ambiguous, assume decimal if <=2 digits after)
            int digitsAfterDot = input.Length - lastDotPos - 1;
            if (digitsAfterDot <= 2)
                return input; // Already correct: 1500.00
            else
                return input.Replace(".", ""); // Thousands separator: 1.500 -> 1500
        }
        else if (commaCount == 1 && dotCount == 0)
        {
            // Only comma: "1500,00" or "1,500" (ambiguous, assume decimal if <=2 digits after)
            int digitsAfterComma = input.Length - lastCommaPos - 1;
            if (digitsAfterComma <= 2)
                return input.Replace(",", "."); // Decimal: 1500,00 -> 1500.00
            else
                return input.Replace(",", ""); // Thousands: 1,500 -> 1500
        }
        else if (dotCount >= 1 && commaCount == 1)
        {
            // Both present: determine which is decimal
            if (lastCommaPos > lastDotPos)
            {
                // Italian: 1.500,00 -> 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // US: 1,500.00 -> 1500.00
                return input.Replace(",", "");
            }
        }
        else if (commaCount >= 1 && dotCount == 1)
        {
            // Both present: determine which is decimal
            if (lastDotPos > lastCommaPos)
            {
                // US: 1,500.00 -> 1500.00
                return input.Replace(",", "");
            }
            else
            {
                // Italian: 1.500,00 -> 1500.00
                return input.Replace(".", "").Replace(",", ".");
            }
        }
        else if (dotCount > 1)
        {
            // Multiple dots: thousands separators, look for comma as decimal
            // e.g., 1.234.567,89 -> 1234567.89
            return input.Replace(".", "").Replace(",", ".");
        }
        else if (commaCount > 1)
        {
            // Multiple commas: thousands separators, look for dot as decimal
            // e.g., 1,234,567.89 -> 1234567.89
            return input.Replace(",", "");
        }

        // Fallback: try to parse as-is with invariant culture
        return input.Replace(",", ".");
    }

    private char DetectSeparator(string line)
    {
        var separators = new[] { ';', ',', '\t', '|' };
        var maxCount = 0;
        var bestSep = ';';

        foreach (var sep in separators)
        {
            // Count separator occurrences OUTSIDE quotes
            var count = 0;
            var insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    // Handle escaped quotes ""
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++; // Skip next quote
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (line[i] == sep && !insideQuotes)
                {
                    count++;
                }
            }

            if (count > maxCount)
            {
                maxCount = count;
                bestSep = sep;
            }
        }

        return bestSep;
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    #endregion
}
