// FILE: Models/Sync/SyncModels.cs

namespace MoneyMindApp.Models.Sync;

/// <summary>
/// Modalità di sincronizzazione
/// </summary>
public enum SyncMode
{
    /// <summary>Cancella tutto sulla destinazione e copia dalla sorgente</summary>
    Replace,
    /// <summary>Mantiene esistenti e aggiunge solo non-duplicati</summary>
    Merge,
    /// <summary>Copia solo transazioni più recenti dell'ultima sulla destinazione</summary>
    NewOnly,
    /// <summary>Crea un nuovo account con i dati dalla sorgente</summary>
    CreateNew
}

/// <summary>
/// Direzione sincronizzazione
/// </summary>
public enum SyncDirection
{
    DesktopToMobile,
    MobileToDesktop
}

/// <summary>
/// Transazione in formato sync (solo campi core, NO MacroCategoria/Categoria)
/// </summary>
public class SyncTransaction
{
    public string Data { get; set; } = string.Empty;  // yyyy-MM-dd
    public decimal Importo { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public string Causale { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Conto in formato sync
/// </summary>
public class SyncAccount
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal SaldoIniziale { get; set; }
    public string? Icona { get; set; }
    public string? Colore { get; set; }
    public int TransactionCount { get; set; }
    public string? LatestTransactionDate { get; set; }  // yyyy-MM-dd
    public DateTime? LatestModifiedAt { get; set; }
    public string? DatabaseFile { get; set; }
    public List<SyncTransaction> Transactions { get; set; } = new();

    // Solo per Desktop (statistiche classificazione)
    public int ClassifiedCount { get; set; }
    public int UniqueMacroCategories { get; set; }

    /// <summary>
    /// ID dell'account di destinazione per il mapping (null = CreateNew)
    /// Esempio: Desktop account ID=7 → Mobile account ID=4
    /// </summary>
    public int? TargetAccountId { get; set; }
}

/// <summary>
/// Richiesta preparazione sync
/// </summary>
public class SyncPrepareRequest
{
    public SyncDirection Direction { get; set; }
    public SyncMode Mode { get; set; }
    public List<int>? AccountIds { get; set; }  // null = tutti
    public List<SyncAccount> SourceAccounts { get; set; } = new();
}

/// <summary>
/// Risposta preparazione sync con confronto
/// </summary>
public class SyncPrepareResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool BackupCreated { get; set; }
    public string? BackupPath { get; set; }
    public List<SyncComparison> Comparisons { get; set; } = new();
    public bool RequiresConfirmation { get; set; }
    public bool HasClassificationWarning { get; set; }
    public int TotalClassifiedTransactions { get; set; }
}

/// <summary>
/// Confronto per singolo conto
/// </summary>
public class SyncComparison
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int SourceTransactionCount { get; set; }
    public string? SourceLatestDate { get; set; }
    public int DestTransactionCount { get; set; }
    public string? DestLatestDate { get; set; }
    public int DestClassifiedCount { get; set; }
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }
}

/// <summary>
/// Richiesta esecuzione sync
/// </summary>
public class SyncExecuteRequest
{
    public SyncDirection Direction { get; set; }
    public SyncMode Mode { get; set; }
    public bool Confirmed { get; set; }
    public List<SyncAccount> Accounts { get; set; } = new();
}

/// <summary>
/// Risultato sync per conto
/// </summary>
public class SyncAccountResult
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int PreviousTransactionCount { get; set; }
    public int NewTransactionCount { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int NewOnlyAdded { get; set; }
    public string Status { get; set; } = string.Empty;  // replaced | merged | new_only | created | error | source_only
    public string? ErrorMessage { get; set; }
    public SyncAccount? AccountData { get; set; }  // Used for MobileToDesktop: contains account data to create on Desktop
}

/// <summary>
/// Risposta esecuzione sync
/// </summary>
public class SyncExecuteResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<SyncAccountResult> Results { get; set; } = new();
    public string? Message { get; set; }
    public int TotalTransactionsProcessed { get; set; }
    public int TotalDuplicatesSkipped { get; set; }
    public int TotalNewAdded { get; set; }
}

/// <summary>
/// Info backup per JSON
/// </summary>
public class BackupInfo
{
    public DateTime CreatedAt { get; set; }
    public string Reason { get; set; } = string.Empty;  // pre_sync, manual
    public string? SyncDirection { get; set; }
    public List<BackupAccountInfo> AccountsBackedUp { get; set; } = new();
    public string AppVersion { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}

public class BackupAccountInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public string? LatestTransaction { get; set; }
}
