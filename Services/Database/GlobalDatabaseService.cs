using SQLite;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Logging;

namespace MoneyMindApp.Services.Database;

/// <summary>
/// Service for managing global database (MoneyMind_Global.db)
/// Contains: BankAccounts, Settings, API Keys, etc.
/// </summary>
public class GlobalDatabaseService
{
    private readonly ILoggingService _loggingService;
    private readonly IDatabaseMigrationService _migrationService;
    private SQLiteAsyncConnection? _connection;
    private string _databasePath = string.Empty;

    public GlobalDatabaseService(ILoggingService loggingService, IDatabaseMigrationService migrationService)
    {
        _loggingService = loggingService;
        _migrationService = migrationService;
    }

    /// <summary>
    /// Initialize global database
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _databasePath = GetDatabasePath();

            _loggingService.LogInfo($"Initializing global database: {Path.GetFileName(_databasePath)}");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if migration is needed
            if (await _migrationService.IsMigrationNeededAsync(_databasePath))
            {
                _loggingService.LogInfo("Global database migration needed");
                await _migrationService.MigrateDatabaseAsync(_databasePath);
            }

            // Open connection
            _connection = new SQLiteAsyncConnection(_databasePath);

            // Ensure tables exist
            try
            {
                var result1 = await _connection.CreateTableAsync<BankAccount>();
                _loggingService.LogInfo($"BankAccount table creation result: {result1}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error creating BankAccount table", ex);
                // Try manual creation
                await _connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS ContiCorrenti (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nome TEXT NOT NULL,
                        Icona TEXT,
                        Colore TEXT,
                        SaldoIniziale REAL NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        LastAccessedAt TEXT
                    )");
                _loggingService.LogInfo("BankAccount table created manually");
            }

            try
            {
                var result2 = await _connection.CreateTableAsync<AppSetting>();
                _loggingService.LogInfo($"AppSetting table creation result: {result2}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error creating AppSetting table", ex);
                // Try manual creation
                await _connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS AppSettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    )");
                _loggingService.LogInfo("AppSetting table created manually");
            }

            // Create SalaryExceptions table
            try
            {
                var result3 = await _connection.CreateTableAsync<SalaryException>();
                _loggingService.LogInfo($"SalaryException table creation result: {result3}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error creating SalaryException table", ex);
                // Try manual creation
                await _connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS SalaryExceptions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Mese INTEGER NOT NULL,
                        Anno INTEGER NOT NULL,
                        IsPermanent INTEGER NOT NULL DEFAULT 0,
                        GiornoAlternativo INTEGER NOT NULL,
                        Nota TEXT,
                        CreatedAt TEXT NOT NULL
                    )");
                _loggingService.LogInfo("SalaryException table created manually");
            }

            // Migration: Add IsPermanent column if it doesn't exist
            try
            {
                var tableInfo = await _connection.QueryAsync<dynamic>("PRAGMA table_info(SalaryExceptions)");
                var hasIsPermanent = tableInfo.Any(col => col.name == "IsPermanent");

                if (!hasIsPermanent)
                {
                    await _connection.ExecuteAsync("ALTER TABLE SalaryExceptions ADD COLUMN IsPermanent INTEGER NOT NULL DEFAULT 0");
                    _loggingService.LogInfo("Added IsPermanent column to SalaryExceptions table");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Could not check/add IsPermanent column: {ex.Message}");
            }

            _loggingService.LogInfo("Global database initialized successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing global database", ex);
            throw;
        }
    }

    /// <summary>
    /// Get global database path
    /// </summary>
    private string GetDatabasePath()
    {
        var appDataPath = DeviceInfo.Platform == DevicePlatform.WinUI
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyMind")
            : FileSystem.AppDataDirectory;

        return Path.Combine(appDataPath, "MoneyMind_Global.db");
    }

    #region Bank Account Operations

    /// <summary>
    /// Get all bank accounts
    /// </summary>
    public async Task<List<BankAccount>> GetAllAccountsAsync()
    {
        EnsureInitialized();

        return await _connection!.Table<BankAccount>()
            .OrderBy(a => a.Nome)
            .ToListAsync();
    }

    /// <summary>
    /// Get bank account by ID
    /// </summary>
    public async Task<BankAccount?> GetAccountByIdAsync(int id)
    {
        EnsureInitialized();

        return await _connection!.Table<BankAccount>()
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Insert new bank account
    /// </summary>
    public async Task<int> InsertAccountAsync(BankAccount account)
    {
        EnsureInitialized();

        account.CreatedAt = DateTime.Now;

        var result = await _connection!.InsertAsync(account);
        _loggingService.LogInfo($"Bank account created: {account.Nome} (ID: {account.Id})");

        return result;
    }

    /// <summary>
    /// Update bank account
    /// </summary>
    public async Task<int> UpdateAccountAsync(BankAccount account)
    {
        EnsureInitialized();

        var result = await _connection!.UpdateAsync(account);
        _loggingService.LogInfo($"Bank account updated: {account.Nome} (ID: {account.Id})");

        return result;
    }

    /// <summary>
    /// Delete bank account
    /// </summary>
    public async Task<int> DeleteAccountAsync(int id)
    {
        EnsureInitialized();

        var result = await _connection!.DeleteAsync<BankAccount>(id);
        _loggingService.LogInfo($"Bank account deleted: ID {id}");

        // TODO: Delete corresponding account database file

        return result;
    }

    /// <summary>
    /// Update last accessed timestamp for account
    /// </summary>
    public async Task UpdateLastAccessedAsync(int accountId)
    {
        EnsureInitialized();

        var account = await GetAccountByIdAsync(accountId);
        if (account != null)
        {
            account.LastAccessedAt = DateTime.Now;
            await UpdateAccountAsync(account);
        }
    }

    #endregion

    #region Settings Operations

    /// <summary>
    /// Get setting value by key
    /// </summary>
    public async Task<string?> GetSettingAsync(string key)
    {
        EnsureInitialized();

        var setting = await _connection!.Table<AppSetting>()
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync();

        return setting?.Value;
    }

    /// <summary>
    /// Set setting value
    /// </summary>
    public async Task SetSettingAsync(string key, string value)
    {
        EnsureInitialized();

        var existing = await _connection!.Table<AppSetting>()
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Value = value;
            existing.ModifiedAt = DateTime.Now;
            await _connection.UpdateAsync(existing);
        }
        else
        {
            var newSetting = new AppSetting
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.Now
            };
            await _connection.InsertAsync(newSetting);
        }

        _loggingService.LogDebug($"Setting updated: {key} = {value}");
    }

    /// <summary>
    /// Save setting (alias for SetSettingAsync)
    /// </summary>
    public async Task SaveSettingAsync(string key, string value)
    {
        await SetSettingAsync(key, value);
    }

    /// <summary>
    /// Get all settings
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        EnsureInitialized();

        var settings = await _connection!.Table<AppSetting>().ToListAsync();

        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    #endregion

    #region Salary Exceptions Operations

    /// <summary>
    /// Get all salary exceptions
    /// </summary>
    public async Task<List<SalaryException>> GetAllSalaryExceptionsAsync()
    {
        EnsureInitialized();

        return await _connection!.Table<SalaryException>()
            .OrderByDescending(e => e.Anno)
            .ThenByDescending(e => e.Mese)
            .ToListAsync();
    }

    /// <summary>
    /// Get salary exception for a specific month/year
    /// </summary>
    public async Task<SalaryException?> GetSalaryExceptionAsync(int mese, int anno)
    {
        EnsureInitialized();

        return await _connection!.Table<SalaryException>()
            .Where(e => e.Mese == mese && e.Anno == anno)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Insert new salary exception
    /// Handles both specific year exceptions and permanent exceptions
    /// </summary>
    public async Task<int> InsertSalaryExceptionAsync(SalaryException exception)
    {
        EnsureInitialized();

        // Check if exception already exists
        SalaryException? existing;
        if (exception.IsPermanent)
        {
            // For permanent exceptions, check if permanent exception exists for this month
            existing = await _connection!.Table<SalaryException>()
                .Where(e => e.Mese == exception.Mese && e.IsPermanent)
                .FirstOrDefaultAsync();
        }
        else
        {
            // For specific year exceptions, check month + year
            existing = await GetSalaryExceptionAsync(exception.Mese, exception.Anno);
        }

        if (existing != null)
        {
            // Update existing instead of inserting duplicate
            existing.GiornoAlternativo = exception.GiornoAlternativo;
            existing.Nota = exception.Nota;
            existing.IsPermanent = exception.IsPermanent;
            existing.Anno = exception.Anno;
            return await _connection!.UpdateAsync(existing);
        }

        exception.CreatedAt = DateTime.Now;
        var result = await _connection!.InsertAsync(exception);
        _loggingService.LogInfo($"Salary exception created: {exception.MeseNome} {exception.AnnoDisplay} → Day {exception.GiornoAlternativo}");

        return result;
    }

    /// <summary>
    /// Update salary exception
    /// </summary>
    public async Task<int> UpdateSalaryExceptionAsync(SalaryException exception)
    {
        EnsureInitialized();

        var result = await _connection!.UpdateAsync(exception);
        _loggingService.LogInfo($"Salary exception updated: {exception.MeseNome} {exception.Anno} → Day {exception.GiornoAlternativo}");

        return result;
    }

    /// <summary>
    /// Delete salary exception
    /// </summary>
    public async Task<int> DeleteSalaryExceptionAsync(int id)
    {
        EnsureInitialized();

        var result = await _connection!.DeleteAsync<SalaryException>(id);
        _loggingService.LogInfo($"Salary exception deleted: ID {id}");

        return result;
    }

    /// <summary>
    /// Delete salary exception by month/year
    /// </summary>
    public async Task<int> DeleteSalaryExceptionAsync(int mese, int anno)
    {
        EnsureInitialized();

        var exception = await GetSalaryExceptionAsync(mese, anno);
        if (exception != null)
        {
            return await DeleteSalaryExceptionAsync(exception.Id);
        }

        return 0;
    }

    #endregion

    /// <summary>
    /// Ensure database is initialized
    /// </summary>
    private void EnsureInitialized()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Global database not initialized. Call InitializeAsync first.");
        }
    }

    /// <summary>
    /// Close database connection
    /// </summary>
    /// <summary>
    /// Optimize database by running VACUUM
    /// Reclaims unused space and reorganizes data for better performance
    /// </summary>
    public async Task<long> VacuumDatabaseAsync()
    {
        EnsureInitialized();

        try
        {
            var sizeBeforeFile = new FileInfo(_databasePath);
            var sizeBefore = sizeBeforeFile.Length;

            _loggingService.LogInfo($"Running VACUUM on global database (size before: {sizeBefore / 1024.0:F1} KB)");

            await _connection!.ExecuteAsync("VACUUM");

            sizeBeforeFile.Refresh();
            var sizeAfter = sizeBeforeFile.Length;
            var savedBytes = sizeBefore - sizeAfter;

            _loggingService.LogInfo($"VACUUM completed on global database (size after: {sizeAfter / 1024.0:F1} KB, saved: {savedBytes / 1024.0:F1} KB)");

            return savedBytes;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error running VACUUM on global database", ex);
            throw;
        }
    }

    public async Task CloseAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection = null;
            _loggingService.LogInfo("Global database closed");
        }
    }
}
