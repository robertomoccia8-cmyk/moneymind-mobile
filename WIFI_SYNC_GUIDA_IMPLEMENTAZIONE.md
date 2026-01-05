# WiFi Sync - Guida Implementazione Passo-Passo

> **Segui questa guida nell'ordine indicato. Non saltare passaggi!**
>
> **Riferimento Tecnico**: `WIFI_SYNC_IMPLEMENTATION.md` (v3)
> **Data**: 24 Novembre 2025

---

## PANORAMICA FASI

| Fase | Descrizione | File da Creare | Tempo Stimato |
|------|-------------|----------------|---------------|
| 1 | Modelli Sync | 1 file | 30 min |
| 2 | Helper Duplicati | 1 file | 20 min |
| 3 | Backup Service | 2 file | 45 min |
| 4 | WiFiSyncService Update | 1 file (modifica) | 2 ore |
| 5 | UI Mobile | 2 file | 1.5 ore |
| 6 | Test Mobile | - | 30 min |
| 7 | Desktop Client | 2 file | 1.5 ore |
| 8 | Desktop Dialog | 2 file | 2 ore |
| 9 | Test End-to-End | - | 1 ora |

**Tempo Totale**: ~10 ore

---

## FASE 1: MODELLI SYNC (Mobile)

### Step 1.1: Creare cartella Models/Sync
```
MoneyMindApp/
└── Models/
    └── Sync/           ← CREARE
        └── SyncModels.cs
```

### Step 1.2: Creare file `Models/Sync/SyncModels.cs`

```csharp
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
    NewOnly
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
    public string Status { get; set; } = string.Empty;  // replaced | merged | new_only | created | error
    public string? ErrorMessage { get; set; }
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
```

### Step 1.3: Verifica Compilazione
```bash
cd C:\Users\rober\Documents\MoneyMindApp
dotnet build
```

**Checkpoint**: ✅ Nessun errore di compilazione

---

## FASE 2: HELPER DUPLICATI (Mobile)

### Step 2.1: Creare cartella Helpers (se non esiste)
```
MoneyMindApp/
└── Helpers/           ← CREARE se non esiste
    └── SyncHelper.cs
```

### Step 2.2: Creare file `Helpers/SyncHelper.cs`

```csharp
// FILE: Helpers/SyncHelper.cs

using MoneyMindApp.Models;
using MoneyMindApp.Models.Sync;

namespace MoneyMindApp.Helpers;

/// <summary>
/// Helper per operazioni di sincronizzazione
/// </summary>
public static class SyncHelper
{
    /// <summary>
    /// Verifica se due transazioni sono duplicate
    /// CRITERIO: Data identica + Descrizione identica (case-insensitive, trimmed)
    /// </summary>
    public static bool IsDuplicate(SyncTransaction source, Transaction dest)
    {
        // Parse data sorgente
        if (!DateTime.TryParse(source.Data, out var sourceDate))
            return false;

        // Confronta data (solo giorno)
        if (sourceDate.Date != dest.Data.Date)
            return false;

        // Confronta descrizione (case-insensitive, trimmed)
        var sourceDesc = (source.Descrizione ?? "").Trim();
        var destDesc = (dest.Descrizione ?? "").Trim();

        return sourceDesc.Equals(destDesc, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se due transazioni sono duplicate (entrambe SyncTransaction)
    /// </summary>
    public static bool IsDuplicate(SyncTransaction source, SyncTransaction dest)
    {
        // Confronta data
        if (source.Data != dest.Data)
            return false;

        // Confronta descrizione (case-insensitive, trimmed)
        var sourceDesc = (source.Descrizione ?? "").Trim();
        var destDesc = (dest.Descrizione ?? "").Trim();

        return sourceDesc.Equals(destDesc, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converte Transaction in SyncTransaction
    /// </summary>
    public static SyncTransaction ToSyncTransaction(Transaction t)
    {
        return new SyncTransaction
        {
            Data = t.Data.ToString("yyyy-MM-dd"),
            Importo = t.Importo,
            Descrizione = t.Descrizione,
            Causale = t.Causale ?? string.Empty,
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt
        };
    }

    /// <summary>
    /// Converte SyncTransaction in Transaction
    /// </summary>
    public static Transaction ToTransaction(SyncTransaction st, int accountId)
    {
        return new Transaction
        {
            Data = DateTime.Parse(st.Data),
            Importo = st.Importo,
            Descrizione = st.Descrizione,
            Causale = string.IsNullOrEmpty(st.Causale) ? null : st.Causale,
            AccountId = accountId,
            CreatedAt = st.CreatedAt ?? DateTime.Now,
            ModifiedAt = st.ModifiedAt
        };
    }

    /// <summary>
    /// Trova l'ultima data transazione in una lista
    /// </summary>
    public static DateTime? GetLatestTransactionDate(List<Transaction> transactions)
    {
        if (transactions == null || transactions.Count == 0)
            return null;

        return transactions.Max(t => t.Data);
    }

    /// <summary>
    /// Filtra transazioni più recenti di una data
    /// </summary>
    public static List<SyncTransaction> FilterNewerThan(
        List<SyncTransaction> transactions,
        DateTime? cutoffDate)
    {
        if (cutoffDate == null)
            return transactions;

        return transactions
            .Where(t => DateTime.TryParse(t.Data, out var date) && date > cutoffDate.Value)
            .ToList();
    }

    /// <summary>
    /// Genera messaggio di warning per confronto
    /// </summary>
    public static string? GenerateWarningMessage(
        int sourceCount,
        string? sourceLatestDate,
        int destCount,
        string? destLatestDate)
    {
        var warnings = new List<string>();

        // Confronta conteggio
        if (destCount > sourceCount)
        {
            warnings.Add($"La destinazione ha {destCount - sourceCount} transazioni in più");
        }

        // Confronta date
        if (!string.IsNullOrEmpty(sourceLatestDate) && !string.IsNullOrEmpty(destLatestDate))
        {
            if (DateTime.TryParse(sourceLatestDate, out var srcDate) &&
                DateTime.TryParse(destLatestDate, out var dstDate))
            {
                if (dstDate > srcDate)
                {
                    warnings.Add($"La destinazione ha dati più recenti ({dstDate:dd/MM/yyyy} vs {srcDate:dd/MM/yyyy})");
                }
            }
        }

        return warnings.Count > 0 ? string.Join(". ", warnings) : null;
    }
}
```

### Step 2.3: Verifica Compilazione
```bash
dotnet build
```

**Checkpoint**: ✅ Nessun errore di compilazione

---

## FASE 3: BACKUP SERVICE (Mobile)

### Step 3.1: Creare cartella Services/Backup
```
MoneyMindApp/
└── Services/
    └── Backup/        ← CREARE
        ├── IBackupService.cs
        └── BackupService.cs
```

### Step 3.2: Creare file `Services/Backup/IBackupService.cs`

```csharp
// FILE: Services/Backup/IBackupService.cs

using MoneyMindApp.Models.Sync;

namespace MoneyMindApp.Services.Backup;

public interface IBackupService
{
    /// <summary>
    /// Crea backup di tutti i database (pre-sync)
    /// </summary>
    Task<BackupResult> CreateBackupAsync(string reason, string? syncDirection = null);

    /// <summary>
    /// Crea backup di conti specifici
    /// </summary>
    Task<BackupResult> CreateBackupAsync(List<int> accountIds, string reason, string? syncDirection = null);

    /// <summary>
    /// Lista backup disponibili
    /// </summary>
    Task<List<BackupInfo>> GetBackupsAsync();

    /// <summary>
    /// Ripristina backup
    /// </summary>
    Task<bool> RestoreBackupAsync(string backupPath);

    /// <summary>
    /// Elimina backup vecchi (mantiene ultimi N)
    /// </summary>
    Task<int> CleanupOldBackupsAsync(int keepCount = 5);
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? Error { get; set; }
    public List<string> FilesBackedUp { get; set; } = new();
    public long TotalSizeBytes { get; set; }
}
```

### Step 3.3: Creare file `Services/Backup/BackupService.cs`

```csharp
// FILE: Services/Backup/BackupService.cs

using MoneyMindApp.Models.Sync;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using Newtonsoft.Json;

namespace MoneyMindApp.Services.Backup;

public class BackupService : IBackupService
{
    private readonly GlobalDatabaseService _globalDb;
    private readonly ILoggingService _logger;
    private readonly string _backupBasePath;

    public BackupService(GlobalDatabaseService globalDb, ILoggingService logger)
    {
        _globalDb = globalDb;
        _logger = logger;

        // Path backup: AppData/MoneyMind/backups/
        var appDataPath = FileSystem.AppDataDirectory;
        _backupBasePath = Path.Combine(appDataPath, "backups");
    }

    public async Task<BackupResult> CreateBackupAsync(string reason, string? syncDirection = null)
    {
        // Backup di tutti i conti
        var accounts = await _globalDb.GetAllAccountsAsync();
        var accountIds = accounts.Select(a => a.Id).ToList();
        return await CreateBackupAsync(accountIds, reason, syncDirection);
    }

    public async Task<BackupResult> CreateBackupAsync(
        List<int> accountIds,
        string reason,
        string? syncDirection = null)
    {
        var result = new BackupResult();

        try
        {
            // Crea cartella backup con timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFolder = Path.Combine(_backupBasePath, $"MoneyMind_Backup_{timestamp}");
            Directory.CreateDirectory(backupFolder);

            _logger.LogInfo($"Creating backup in: {backupFolder}");

            var appDataPath = FileSystem.AppDataDirectory;
            var backupAccountInfos = new List<BackupAccountInfo>();

            // 1. Backup Global DB
            var globalDbSource = Path.Combine(appDataPath, "MoneyMind_Global.db");
            if (File.Exists(globalDbSource))
            {
                var globalDbDest = Path.Combine(backupFolder, "MoneyMind_Global.db");
                File.Copy(globalDbSource, globalDbDest, true);
                result.FilesBackedUp.Add("MoneyMind_Global.db");
                result.TotalSizeBytes += new FileInfo(globalDbDest).Length;
            }

            // 2. Backup DB per ogni conto
            foreach (var accountId in accountIds)
            {
                var accountDbName = $"MoneyMind_Conto_{accountId:D3}.db";
                var accountDbSource = Path.Combine(appDataPath, accountDbName);

                if (File.Exists(accountDbSource))
                {
                    var accountDbDest = Path.Combine(backupFolder, accountDbName);
                    File.Copy(accountDbSource, accountDbDest, true);
                    result.FilesBackedUp.Add(accountDbName);
                    result.TotalSizeBytes += new FileInfo(accountDbDest).Length;

                    // Info per backup_info.json
                    var account = await _globalDb.GetAccountByIdAsync(accountId);
                    if (account != null)
                    {
                        backupAccountInfos.Add(new BackupAccountInfo
                        {
                            Id = accountId,
                            Name = account.Nome,
                            TransactionCount = 0, // TODO: contare se necessario
                            LatestTransaction = null
                        });
                    }
                }
            }

            // 3. Crea backup_info.json
            var backupInfo = new BackupInfo
            {
                CreatedAt = DateTime.Now,
                Reason = reason,
                SyncDirection = syncDirection,
                AccountsBackedUp = backupAccountInfos,
                AppVersion = AppInfo.VersionString,
                Platform = DeviceInfo.Platform.ToString()
            };

            var infoJson = JsonConvert.SerializeObject(backupInfo, Formatting.Indented);
            var infoPath = Path.Combine(backupFolder, "backup_info.json");
            await File.WriteAllTextAsync(infoPath, infoJson);

            result.Success = true;
            result.BackupPath = backupFolder;

            _logger.LogInfo($"Backup created successfully: {result.FilesBackedUp.Count} files, {result.TotalSizeBytes / 1024.0:F1} KB");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating backup", ex);
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    public async Task<List<BackupInfo>> GetBackupsAsync()
    {
        var backups = new List<BackupInfo>();

        try
        {
            if (!Directory.Exists(_backupBasePath))
                return backups;

            var backupFolders = Directory.GetDirectories(_backupBasePath)
                .OrderByDescending(f => f);

            foreach (var folder in backupFolders)
            {
                var infoPath = Path.Combine(folder, "backup_info.json");
                if (File.Exists(infoPath))
                {
                    var json = await File.ReadAllTextAsync(infoPath);
                    var info = JsonConvert.DeserializeObject<BackupInfo>(json);
                    if (info != null)
                    {
                        backups.Add(info);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error listing backups", ex);
        }

        return backups;
    }

    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        try
        {
            if (!Directory.Exists(backupPath))
            {
                _logger.LogError($"Backup path not found: {backupPath}");
                return false;
            }

            var appDataPath = FileSystem.AppDataDirectory;
            var files = Directory.GetFiles(backupPath, "*.db");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destPath = Path.Combine(appDataPath, fileName);

                // Sovrascrivi file esistente
                File.Copy(file, destPath, true);
                _logger.LogInfo($"Restored: {fileName}");
            }

            await Task.CompletedTask;
            _logger.LogInfo($"Backup restored from: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error restoring backup", ex);
            return false;
        }
    }

    public async Task<int> CleanupOldBackupsAsync(int keepCount = 5)
    {
        var deletedCount = 0;

        try
        {
            if (!Directory.Exists(_backupBasePath))
                return 0;

            var backupFolders = Directory.GetDirectories(_backupBasePath)
                .OrderByDescending(f => f)
                .ToList();

            // Mantieni solo gli ultimi N
            var foldersToDelete = backupFolders.Skip(keepCount).ToList();

            foreach (var folder in foldersToDelete)
            {
                Directory.Delete(folder, true);
                deletedCount++;
                _logger.LogInfo($"Deleted old backup: {Path.GetFileName(folder)}");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error cleaning up backups", ex);
        }

        return deletedCount;
    }
}
```

### Step 3.4: Registrare in DI (MauiProgram.cs)

Aggiungere in `MauiProgram.cs`:
```csharp
// Nella sezione services
builder.Services.AddSingleton<IBackupService, BackupService>();
```

### Step 3.5: Verifica Compilazione
```bash
dotnet build
```

**Checkpoint**: ✅ Nessun errore di compilazione

---

## FASE 4: WIFI SYNC SERVICE UPDATE (Mobile)

### Step 4.1: Leggere file esistente
```
Services/Sync/WiFiSyncService.cs
```

### Step 4.2: Aggiungere dipendenze al costruttore

```csharp
private readonly ILoggingService _loggingService;
private readonly DatabaseService _databaseService;
private readonly GlobalDatabaseService _globalDatabaseService;
private readonly IBackupService _backupService;

public WiFiSyncService(
    ILoggingService loggingService,
    DatabaseService databaseService,
    GlobalDatabaseService globalDatabaseService,
    IBackupService backupService)
{
    _loggingService = loggingService;
    _databaseService = databaseService;
    _globalDatabaseService = globalDatabaseService;
    _backupService = backupService;
}
```

### Step 4.3: Aggiungere handler nel metodo HandleRequestAsync

```csharp
switch (path)
{
    case "/ping":
        await HandlePingAsync(context);
        break;
    case "/info":
        await HandleInfoAsync(context);
        break;
    case "/accounts":
        await HandleGetAccountsAsync(context);
        break;
    case var p when p.StartsWith("/transactions/"):
        await HandleGetTransactionsAsync(context);
        break;
    case "/sync/prepare":
        if (context.Request.Method == "POST")
            await HandleSyncPrepareAsync(context);
        break;
    case "/sync/execute":
        if (context.Request.Method == "POST")
            await HandleSyncExecuteAsync(context);
        break;
    default:
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Endpoint not found");
        break;
}
```

### Step 4.4: Implementare HandleGetAccountsAsync

```csharp
private async Task HandleGetAccountsAsync(HttpContext context)
{
    try
    {
        var accounts = await _globalDatabaseService.GetAllAccountsAsync();
        var syncAccounts = new List<SyncAccount>();

        foreach (var account in accounts)
        {
            await _databaseService.InitializeAsync(account.Id);
            var transactions = await _databaseService.GetAllTransactionsAsync();
            var latestDate = transactions.Any()
                ? transactions.Max(t => t.Data).ToString("yyyy-MM-dd")
                : null;

            syncAccounts.Add(new SyncAccount
            {
                Id = account.Id,
                Nome = account.Nome,
                SaldoIniziale = account.SaldoIniziale,
                Icona = account.Icona,
                Colore = account.Colore,
                TransactionCount = transactions.Count,
                LatestTransactionDate = latestDate,
                DatabaseFile = account.DatabaseFileName
            });
        }

        var response = new { success = true, accounts = syncAccounts };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
    catch (Exception ex)
    {
        _loggingService.LogError("Error in GET /accounts", ex);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
    }
}
```

### Step 4.5: Implementare HandleSyncPrepareAsync

```csharp
private async Task HandleSyncPrepareAsync(HttpContext context)
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<SyncPrepareRequest>(body);

        if (request == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = "Invalid request" }));
            return;
        }

        // Crea backup
        var backupResult = await _backupService.CreateBackupAsync(
            "pre_sync",
            request.Direction.ToString());

        // Confronta dati
        var comparisons = new List<SyncComparison>();
        var mobileAccounts = await _globalDatabaseService.GetAllAccountsAsync();

        foreach (var sourceAccount in request.SourceAccounts)
        {
            var mobileAccount = mobileAccounts.FirstOrDefault(a => a.Id == sourceAccount.Id);

            int destCount = 0;
            string? destLatestDate = null;

            if (mobileAccount != null)
            {
                await _databaseService.InitializeAsync(mobileAccount.Id);
                var destTransactions = await _databaseService.GetAllTransactionsAsync();
                destCount = destTransactions.Count;
                destLatestDate = destTransactions.Any()
                    ? destTransactions.Max(t => t.Data).ToString("yyyy-MM-dd")
                    : null;
            }

            var warning = SyncHelper.GenerateWarningMessage(
                sourceAccount.TransactionCount,
                sourceAccount.LatestTransactionDate,
                destCount,
                destLatestDate);

            comparisons.Add(new SyncComparison
            {
                AccountId = sourceAccount.Id,
                AccountName = sourceAccount.Nome,
                SourceTransactionCount = sourceAccount.TransactionCount,
                SourceLatestDate = sourceAccount.LatestTransactionDate,
                DestTransactionCount = destCount,
                DestLatestDate = destLatestDate,
                HasWarning = warning != null,
                WarningMessage = warning
            });
        }

        var response = new SyncPrepareResponse
        {
            Success = true,
            BackupCreated = backupResult.Success,
            BackupPath = backupResult.BackupPath,
            Comparisons = comparisons,
            RequiresConfirmation = comparisons.Any(c => c.HasWarning)
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
    catch (Exception ex)
    {
        _loggingService.LogError("Error in POST /sync/prepare", ex);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
    }
}
```

### Step 4.6: Implementare HandleSyncExecuteAsync (con 3 modalità)

```csharp
private async Task HandleSyncExecuteAsync(HttpContext context)
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<SyncExecuteRequest>(body);

        if (request == null || !request.Confirmed)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = "Confirmation required" }));
            return;
        }

        var results = new List<SyncAccountResult>();
        int totalProcessed = 0, totalDuplicates = 0, totalNew = 0;

        foreach (var accountData in request.Accounts)
        {
            var result = await ProcessAccountSyncAsync(accountData, request.Mode);
            results.Add(result);

            totalProcessed += result.NewTransactionCount;
            totalDuplicates += result.DuplicatesSkipped;
            totalNew += result.NewOnlyAdded;
        }

        var response = new SyncExecuteResponse
        {
            Success = true,
            Results = results,
            TotalTransactionsProcessed = totalProcessed,
            TotalDuplicatesSkipped = totalDuplicates,
            TotalNewAdded = totalNew,
            Message = GenerateSyncMessage(request.Mode, results)
        };

        // Salva statistiche
        Preferences.Set("last_sync_time", DateTime.Now.ToString("o"));
        Preferences.Set("last_sync_direction", request.Direction.ToString());

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
    catch (Exception ex)
    {
        _loggingService.LogError("Error in POST /sync/execute", ex);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
    }
}

private async Task<SyncAccountResult> ProcessAccountSyncAsync(SyncAccount accountData, SyncMode mode)
{
    var result = new SyncAccountResult
    {
        AccountId = accountData.Id,
        AccountName = accountData.Nome
    };

    try
    {
        // Trova o crea il conto
        var account = await _globalDatabaseService.GetAccountByIdAsync(accountData.Id);
        if (account == null)
        {
            account = new BankAccount
            {
                Nome = accountData.Nome,
                SaldoIniziale = accountData.SaldoIniziale,
                Icona = accountData.Icona,
                Colore = accountData.Colore,
                CreatedAt = DateTime.Now
            };
            await _globalDatabaseService.InsertAccountAsync(account);
            result.Status = "created";
        }

        await _databaseService.InitializeAsync(account.Id);
        var existingTransactions = await _databaseService.GetAllTransactionsAsync();
        result.PreviousTransactionCount = existingTransactions.Count;

        switch (mode)
        {
            case SyncMode.Replace:
                await ExecuteReplaceAsync(accountData, account.Id);
                result.NewTransactionCount = accountData.Transactions.Count;
                result.Status = "replaced";
                break;

            case SyncMode.Merge:
                var mergeResult = await ExecuteMergeAsync(accountData, account.Id, existingTransactions);
                result.NewTransactionCount = existingTransactions.Count + mergeResult.added;
                result.DuplicatesSkipped = mergeResult.skipped;
                result.Status = "merged";
                break;

            case SyncMode.NewOnly:
                var newOnlyResult = await ExecuteNewOnlyAsync(accountData, account.Id, existingTransactions);
                result.NewTransactionCount = existingTransactions.Count + newOnlyResult;
                result.NewOnlyAdded = newOnlyResult;
                result.Status = "new_only";
                break;
        }
    }
    catch (Exception ex)
    {
        result.Status = "error";
        result.ErrorMessage = ex.Message;
        _loggingService.LogError($"Error processing account {accountData.Id}", ex);
    }

    return result;
}

private async Task ExecuteReplaceAsync(SyncAccount accountData, int accountId)
{
    // Cancella tutte le transazioni esistenti
    await _databaseService.DeleteAllTransactionsAsync();

    // Inserisce tutte le nuove
    foreach (var syncTx in accountData.Transactions)
    {
        var transaction = SyncHelper.ToTransaction(syncTx, accountId);
        await _databaseService.InsertTransactionAsync(transaction);
    }
}

private async Task<(int added, int skipped)> ExecuteMergeAsync(
    SyncAccount accountData,
    int accountId,
    List<Transaction> existingTransactions)
{
    int added = 0, skipped = 0;

    foreach (var syncTx in accountData.Transactions)
    {
        // Verifica se è duplicato
        bool isDuplicate = existingTransactions.Any(t => SyncHelper.IsDuplicate(syncTx, t));

        if (isDuplicate)
        {
            skipped++;
        }
        else
        {
            var transaction = SyncHelper.ToTransaction(syncTx, accountId);
            await _databaseService.InsertTransactionAsync(transaction);
            added++;
        }
    }

    return (added, skipped);
}

private async Task<int> ExecuteNewOnlyAsync(
    SyncAccount accountData,
    int accountId,
    List<Transaction> existingTransactions)
{
    // Trova ultima data esistente
    var latestDate = SyncHelper.GetLatestTransactionDate(existingTransactions);

    // Filtra solo transazioni più recenti
    var newTransactions = SyncHelper.FilterNewerThan(accountData.Transactions, latestDate);

    foreach (var syncTx in newTransactions)
    {
        var transaction = SyncHelper.ToTransaction(syncTx, accountId);
        await _databaseService.InsertTransactionAsync(transaction);
    }

    return newTransactions.Count;
}

private string GenerateSyncMessage(SyncMode mode, List<SyncAccountResult> results)
{
    var totalAccounts = results.Count;
    var successCount = results.Count(r => r.Status != "error");

    return mode switch
    {
        SyncMode.Replace => $"Sostituite transazioni in {successCount}/{totalAccounts} conti",
        SyncMode.Merge => $"Uniti {results.Sum(r => r.NewTransactionCount - r.PreviousTransactionCount)} transazioni, {results.Sum(r => r.DuplicatesSkipped)} duplicati saltati",
        SyncMode.NewOnly => $"Aggiunte {results.Sum(r => r.NewOnlyAdded)} nuove transazioni",
        _ => "Sync completato"
    };
}
```

### Step 4.7: Aggiungere metodo DeleteAllTransactionsAsync a DatabaseService

In `Services/Database/DatabaseService.cs`, aggiungere:

```csharp
/// <summary>
/// Delete all transactions (for sync REPLACE mode)
/// </summary>
public async Task DeleteAllTransactionsAsync()
{
    EnsureInitialized();
    await _connection!.DeleteAllAsync<Transaction>();
    _loggingService.LogInfo($"All transactions deleted for account {_currentAccountId}");
}
```

### Step 4.8: Verifica Compilazione e aggiorna DI
```bash
dotnet build
```

**Checkpoint**: ✅ Nessun errore di compilazione

---

## FASE 5: UI MOBILE (WiFiSyncPage)

### Step 5.1: Creare `Views/WiFiSyncPage.xaml`

[Vedere WIFI_SYNC_IMPLEMENTATION.md sezione 7 per layout]

### Step 5.2: Creare `ViewModels/WiFiSyncViewModel.cs`

[Implementare ViewModel con comandi per Start/Stop server]

### Step 5.3: Registrare route in AppShell.xaml.cs

```csharp
Routing.RegisterRoute("wifisync", typeof(WiFiSyncPage));
```

### Step 5.4: Aggiungere navigazione da SettingsPage

---

## FASE 6: TEST MOBILE

### Test da eseguire su emulatore Android:

1. [ ] Avviare server WiFi Sync
2. [ ] Verificare IP mostrato
3. [ ] Testare endpoint /ping da browser: `http://[IP]:8765/ping`
4. [ ] Testare endpoint /accounts
5. [ ] Verificare backup creato in /backups/

---

## FASE 7-9: DESKTOP (da definire dopo completamento Mobile)

[Le fasi Desktop verranno dettagliate dopo il completamento e test delle fasi Mobile]

---

## NOTE IMPORTANTI

### Ricorda sempre:
1. **BACKUP PRIMA DI TUTTO**: Ogni sync crea backup automatico
2. **3 MODALITÀ**: Replace, Merge, NewOnly
3. **CRITERIO DUPLICATO**: Data + Descrizione (case-insensitive)
4. **NO CLASSIFICAZIONI**: MacroCategoria/Categoria non vengono trasferite
5. **TEST INCREMENTALI**: Testa dopo ogni fase prima di procedere

---

**Fine Guida - WIFI_SYNC_GUIDA_IMPLEMENTAZIONE.md**
