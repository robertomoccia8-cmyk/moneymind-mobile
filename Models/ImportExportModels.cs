namespace MoneyMindApp.Models;

/// <summary>
/// Mapping colonne per import CSV/Excel
/// </summary>
public class ColumnMapping
{
    public int DataColumn { get; set; } = -1;
    public int ImportoColumn { get; set; } = -1;
    public int DescrizioneColumn { get; set; } = -1;
    public int CausaleColumn { get; set; } = -1;

    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string DecimalSeparator { get; set; } = ",";
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// Numero riga che contiene le intestazioni (1-based)
    /// Es. Se il file ha loghi/info nelle prime 9 righe, e header a riga 10, HeaderRowNumber = 10
    /// I dati inizieranno dalla riga HeaderRowNumber + 1
    /// </summary>
    public int HeaderRowNumber { get; set; } = 1;

    public bool IsValid => DataColumn >= 0 && ImportoColumn >= 0 && DescrizioneColumn >= 0;
}

/// <summary>
/// Risultato operazione import
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message => Success
        ? $"✅ Importate {ImportedCount} transazioni ({SkippedCount} saltate, {DuplicateCount} duplicati)"
        : $"❌ Errore import: {Errors.FirstOrDefault()}";
}

/// <summary>
/// Riga preview per import
/// </summary>
public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public DateTime? Data { get; set; }
    public decimal? Importo { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public string Causale { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsDuplicate { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public string DataFormatted => Data?.ToString("dd/MM/yyyy") ?? "N/A";
    public string ImportoFormatted => Importo?.ToString("C2") ?? "N/A";
    public string StatusIcon => IsDuplicate ? "⚠️" : (IsValid ? "✅" : "❌");
}

/// <summary>
/// Opzioni export
/// </summary>
public class ExportOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public bool IncludeHeader { get; set; } = true;
    public List<string> Columns { get; set; } = new() { "Data", "Importo", "Descrizione", "Causale" };
}

public enum ExportFormat
{
    Csv,
    Excel
}

/// <summary>
/// Risultato export
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
    public string Message => Success
        ? $"✅ Esportate {ExportedCount} transazioni"
        : "❌ Errore durante l'export";
}

/// <summary>
/// Riga raw del file per anteprima (mostra contenuto grezzo con numero riga)
/// Usato nello step di selezione riga header
/// </summary>
public class FilePreviewRow
{
    public int RowNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsSelected { get; set; }

    public string RowNumberFormatted => $"{RowNumber:D3}";
    public string ContentTruncated => Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;
}

/// <summary>
/// Riga validata per step validation (step 4)
/// Contiene dati raw (string) + flag validazione + messaggio errore
/// Usata per mostrare preview verde/rosso prima dell'import finale
/// </summary>
public class TransactionValidationRow
{
    public int RowNumber { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Importo { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string Causale { get; set; } = string.Empty;
    public bool HasErrors { get; set; }
    public bool IsDuplicate { get; set; }  // Added for duplicate detection
    public string ErrorMessage { get; set; } = string.Empty;

    // UI Properties
    public Color RowColor => IsDuplicate ? Colors.LightYellow : (HasErrors ? Colors.LightCoral : Colors.LightGreen);
    public string StatusIcon => IsDuplicate ? "⚠️" : (HasErrors ? "❌" : "✅");
    public string RowNumberFormatted => $"Riga {RowNumber}";

    // Display error/duplicate message in UI
    public string DisplayMessage => IsDuplicate ? "⚠️ Duplicato - Già presente nel database" : ErrorMessage;
}
