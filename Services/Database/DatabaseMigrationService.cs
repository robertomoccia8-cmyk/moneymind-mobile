using SQLite;
using MoneyMindApp.Services.Logging;

namespace MoneyMindApp.Services.Database;

/// <summary>
/// Implementation of database migration service
/// Handles schema versioning and upgrades
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly ILoggingService _loggingService;
    private const int CURRENT_VERSION = 1; // Update this when adding new migrations

    public DatabaseMigrationService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Get current database version from metadata table
    /// </summary>
    public async Task<int> GetDatabaseVersionAsync(string databasePath)
    {
        try
        {
            var connection = new SQLiteAsyncConnection(databasePath);

            // Check if metadata table exists
            var tableInfo = await connection.QueryAsync<TableInfo>(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='DatabaseMetadata'");

            if (tableInfo.Count == 0)
            {
                // No metadata table = version 0 (needs initialization)
                return 0;
            }

            // Get version from metadata
            var result = await connection.QueryAsync<MetadataValue>(
                "SELECT Value FROM DatabaseMetadata WHERE Key = 'Version'");

            if (result.Count > 0 && !string.IsNullOrEmpty(result[0].Value))
            {
                return int.Parse(result[0].Value);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error getting database version: {databasePath}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Check if migration is needed
    /// </summary>
    public async Task<bool> IsMigrationNeededAsync(string databasePath)
    {
        var currentVersion = await GetDatabaseVersionAsync(databasePath);
        return currentVersion < CURRENT_VERSION;
    }

    /// <summary>
    /// Migrate database to latest version
    /// </summary>
    public async Task<bool> MigrateDatabaseAsync(string databasePath)
    {
        try
        {
            _loggingService.LogInfo($"Starting database migration: {Path.GetFileName(databasePath)}");

            var connection = new SQLiteAsyncConnection(databasePath);
            var currentVersion = await GetDatabaseVersionAsync(databasePath);

            _loggingService.LogInfo($"Current version: {currentVersion}, Target version: {CURRENT_VERSION}");

            // Create metadata table if doesn't exist
            if (currentVersion == 0)
            {
                await InitializeDatabaseAsync(connection);
                currentVersion = 0;
            }

            // Apply migrations sequentially
            for (int version = currentVersion + 1; version <= CURRENT_VERSION; version++)
            {
                _loggingService.LogInfo($"Applying migration to version {version}");

                var success = await ApplyMigrationAsync(connection, version);

                if (!success)
                {
                    _loggingService.LogError($"Migration to version {version} failed!");
                    return false;
                }

                // Record migration history
                await RecordMigrationAsync(connection, version, $"Migration to version {version}", success: true);
            }

            _loggingService.LogInfo($"Database migration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Database migration failed: {databasePath}", ex);
            return false;
        }
    }

    /// <summary>
    /// Get migration history
    /// </summary>
    public async Task<List<MigrationHistory>> GetMigrationHistoryAsync(string databasePath)
    {
        var history = new List<MigrationHistory>();

        try
        {
            var connection = new SQLiteAsyncConnection(databasePath);

            // Check if history table exists
            var tableInfo = await connection.QueryAsync<TableInfo>(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='MigrationHistory'");

            if (tableInfo.Count == 0)
            {
                return history;
            }

            var results = await connection.Table<MigrationHistory>()
                .OrderByDescending(m => m.AppliedAt)
                .ToListAsync();

            history.AddRange(results);
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error getting migration history: {databasePath}", ex);
        }

        return history;
    }

    #region Private Methods

    /// <summary>
    /// Initialize new database with metadata tables
    /// </summary>
    private async Task InitializeDatabaseAsync(SQLiteAsyncConnection connection)
    {
        // Create metadata table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS DatabaseMetadata (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )");

        // Create migration history table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS MigrationHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Version INTEGER NOT NULL,
                Description TEXT,
                AppliedAt TEXT NOT NULL,
                Success INTEGER NOT NULL,
                ErrorMessage TEXT
            )");

        // Set initial version
        await connection.ExecuteAsync(
            "INSERT OR REPLACE INTO DatabaseMetadata (Key, Value) VALUES ('Version', '0')");

        _loggingService.LogInfo("Database metadata tables created");
    }

    /// <summary>
    /// Apply specific migration version
    /// </summary>
    private async Task<bool> ApplyMigrationAsync(SQLiteAsyncConnection connection, int version)
    {
        try
        {
            switch (version)
            {
                case 1:
                    // Migration 1: Create initial tables
                    await Migration_V1_InitialTablesAsync(connection);
                    break;

                // Add more migrations here as needed
                // case 2:
                //     await Migration_V2_AddNewColumnAsync(connection);
                //     break;

                default:
                    _loggingService.LogWarning($"No migration defined for version {version}");
                    return false;
            }

            // Update version in metadata
            await connection.ExecuteAsync(
                $"UPDATE DatabaseMetadata SET Value = '{version}' WHERE Key = 'Version'");

            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error applying migration version {version}", ex);
            return false;
        }
    }

    /// <summary>
    /// Record migration in history table
    /// </summary>
    private async Task RecordMigrationAsync(SQLiteAsyncConnection connection, int version, string description, bool success, string? errorMessage = null)
    {
        try
        {
            await connection.ExecuteAsync(@"
                INSERT INTO MigrationHistory (Version, Description, AppliedAt, Success, ErrorMessage)
                VALUES (?, ?, ?, ?, ?)",
                version, description, DateTime.Now.ToString("o"), success ? 1 : 0, errorMessage);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error recording migration", ex);
        }
    }

    #endregion

    #region Migrations

    /// <summary>
    /// Migration V1: Create initial tables (Transazioni)
    /// </summary>
    private async Task Migration_V1_InitialTablesAsync(SQLiteAsyncConnection connection)
    {
        _loggingService.LogInfo("Running Migration V1: Initial tables");

        // Create Transazioni table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Transazioni (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Data TEXT NOT NULL,
                Importo REAL NOT NULL,
                Descrizione TEXT NOT NULL,
                Causale TEXT,
                Note TEXT,
                AccountId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                ModifiedAt TEXT
            )");

        // Create indexes for performance
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IDX_Transazioni_Data ON Transazioni(Data)");

        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IDX_Transazioni_AccountId ON Transazioni(AccountId)");

        _loggingService.LogInfo("Migration V1 completed: Tables created");
    }

    // Add more migration methods here for future versions
    // Example:
    // private async Task Migration_V2_AddNewColumnAsync(SQLiteAsyncConnection connection)
    // {
    //     await connection.ExecuteAsync("ALTER TABLE Transazioni ADD COLUMN NewColumn TEXT");
    // }

    #endregion
}

// Helper classes for SQLite queries (avoid dynamic type issues on Android)
internal class TableInfo
{
    public string? name { get; set; }
}

internal class MetadataValue
{
    public string Value { get; set; } = "";
}
