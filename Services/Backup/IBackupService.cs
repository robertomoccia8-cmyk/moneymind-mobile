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

    /// <summary>
    /// Ottiene il percorso base dei backup
    /// </summary>
    string GetBackupBasePath();
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? Error { get; set; }
    public List<string> FilesBackedUp { get; set; } = new();
    public long TotalSizeBytes { get; set; }
}
