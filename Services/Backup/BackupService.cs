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

    public string GetBackupBasePath() => _backupBasePath;

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
