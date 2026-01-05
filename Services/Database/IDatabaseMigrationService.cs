namespace MoneyMindApp.Services.Database;

/// <summary>
/// Service for database schema migrations and versioning
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// Get current database version
    /// </summary>
    Task<int> GetDatabaseVersionAsync(string databasePath);

    /// <summary>
    /// Migrate database to latest version
    /// </summary>
    Task<bool> MigrateDatabaseAsync(string databasePath);

    /// <summary>
    /// Check if migration is needed
    /// </summary>
    Task<bool> IsMigrationNeededAsync(string databasePath);

    /// <summary>
    /// Get migration history
    /// </summary>
    Task<List<MigrationHistory>> GetMigrationHistoryAsync(string databasePath);
}

/// <summary>
/// Migration history record
/// </summary>
public class MigrationHistory
{
    public int Version { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
