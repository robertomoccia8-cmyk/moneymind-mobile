using SQLite;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Logging;

namespace MoneyMindApp.Services.Database;

/// <summary>
/// Service for managing account-specific databases (MoneyMind_Conto_XXX.db)
/// Each account has its own database file with transactions
/// </summary>
public class DatabaseService
{
    private readonly ILoggingService _loggingService;
    private readonly IDatabaseMigrationService _migrationService;
    private SQLiteAsyncConnection? _connection;
    private int _currentAccountId;
    private string _databasePath = string.Empty;

    public DatabaseService(ILoggingService loggingService, IDatabaseMigrationService migrationService)
    {
        _loggingService = loggingService;
        _migrationService = migrationService;
    }

    /// <summary>
    /// Initialize database for specific account
    /// </summary>
    public async Task InitializeAsync(int accountId)
    {
        try
        {
            _currentAccountId = accountId;
            _databasePath = GetDatabasePath(accountId);

            _loggingService.LogInfo($"Initializing database for account {accountId}: {Path.GetFileName(_databasePath)}");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if migration is needed
            if (await _migrationService.IsMigrationNeededAsync(_databasePath))
            {
                _loggingService.LogInfo("Database migration needed");
                await _migrationService.MigrateDatabaseAsync(_databasePath);
            }

            // Open connection
            _connection = new SQLiteAsyncConnection(_databasePath);

            // Ensure tables exist (for new databases)
            await _connection.CreateTableAsync<Transaction>();

            // ✅ Create indexes for performance optimization
            // These indexes dramatically improve query speed for filtering and searching
            await _connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_transaction_data ON Transazioni(Data DESC)");
            await _connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_transaction_importo ON Transazioni(Importo)");
            await _connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_transaction_descrizione ON Transazioni(Descrizione COLLATE NOCASE)");

            _loggingService.LogInfo($"Database initialized successfully for account {accountId}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error initializing database for account {accountId}", ex);
            throw;
        }
    }

    /// <summary>
    /// Get database path for account
    /// </summary>
    private string GetDatabasePath(int accountId)
    {
        var appDataPath = DeviceInfo.Platform == DevicePlatform.WinUI
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyMind")
            : FileSystem.AppDataDirectory;

        return Path.Combine(appDataPath, $"MoneyMind_Conto_{accountId:D3}.db");
    }

    #region Transaction Operations

    /// <summary>
    /// Get all transactions for date range
    /// </summary>
    public async Task<List<Transaction>> GetTransactionsAsync(DateTime startDate, DateTime endDate)
    {
        EnsureInitialized();

        return await _connection!.Table<Transaction>()
            .Where(t => t.Data >= startDate && t.Data <= endDate)
            .OrderByDescending(t => t.Data)
            .ToListAsync();
    }

    /// <summary>
    /// Get all transactions
    /// </summary>
    public async Task<List<Transaction>> GetAllTransactionsAsync()
    {
        EnsureInitialized();

        return await _connection!.Table<Transaction>()
            .OrderByDescending(t => t.Data)
            .ToListAsync();
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        EnsureInitialized();

        return await _connection!.Table<Transaction>()
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Insert new transaction
    /// </summary>
    public async Task<int> InsertTransactionAsync(Transaction transaction)
    {
        EnsureInitialized();

        transaction.AccountId = _currentAccountId;
        transaction.CreatedAt = DateTime.Now;

        var result = await _connection!.InsertAsync(transaction);
        _loggingService.LogInfo($"Transaction inserted: {transaction.Descrizione} ({transaction.Importo:C2})");

        return result;
    }

    /// <summary>
    /// Update existing transaction
    /// </summary>
    public async Task<int> UpdateTransactionAsync(Transaction transaction)
    {
        EnsureInitialized();

        transaction.ModifiedAt = DateTime.Now;

        var result = await _connection!.UpdateAsync(transaction);
        _loggingService.LogInfo($"Transaction updated: ID {transaction.Id}");

        return result;
    }

    /// <summary>
    /// Delete transaction
    /// </summary>
    public async Task<int> DeleteTransactionAsync(int id)
    {
        EnsureInitialized();

        var result = await _connection!.DeleteAsync<Transaction>(id);
        _loggingService.LogInfo($"Transaction deleted: ID {id}");

        return result;
    }

    /// <summary>
    /// Get transactions with filters applied at DATABASE level (OPTIMIZED)
    /// This is much faster than loading all transactions and filtering in memory
    /// </summary>
    public async Task<List<Transaction>> GetTransactionsWithFiltersAsync(
        string? searchText = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? transactionType = null) // 0=All, 1=Income, 2=Expense
    {
        EnsureInitialized();

        // Start with base query
        var query = _connection!.Table<Transaction>();

        // Apply date range filter (SQL WHERE)
        if (startDate.HasValue && endDate.HasValue)
        {
            query = query.Where(t => t.Data >= startDate.Value && t.Data <= endDate.Value);
        }
        else if (startDate.HasValue)
        {
            query = query.Where(t => t.Data >= startDate.Value);
        }
        else if (endDate.HasValue)
        {
            query = query.Where(t => t.Data <= endDate.Value);
        }

        // Apply amount range filters (SQL WHERE)
        if (minAmount.HasValue)
        {
            query = query.Where(t => t.Importo >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(t => t.Importo <= maxAmount.Value);
        }

        // Apply transaction type filter (SQL WHERE)
        if (transactionType.HasValue)
        {
            switch (transactionType.Value)
            {
                case 1: // Income only
                    query = query.Where(t => t.Importo > 0);
                    break;
                case 2: // Expense only
                    query = query.Where(t => t.Importo < 0);
                    break;
                // case 0 or default: All transactions (no filter)
            }
        }

        // Get results ordered by date
        var results = await query.OrderByDescending(t => t.Data).ToListAsync();

        // Apply search text filter (has to be done in-memory due to SQLite-net limitations with Contains)
        // But this is much faster since we've already reduced the dataset with SQL filters
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            results = results
                .Where(t => t.Descrizione.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           (t.Causale?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return results;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get total balance (SaldoIniziale + SUM(Importi))
    /// </summary>
    public async Task<decimal> GetTotalBalanceAsync(decimal saldoIniziale)
    {
        EnsureInitialized();

        var transactions = await GetAllTransactionsAsync();
        var totalImporti = transactions.Sum(t => t.Importo);

        return saldoIniziale + totalImporti;
    }

    /// <summary>
    /// Get statistics for date range
    /// </summary>
    public async Task<(decimal Income, decimal Expenses, decimal Savings, int Count)> GetStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        EnsureInitialized();

        var transactions = await GetTransactionsAsync(startDate, endDate);

        var income = transactions.Where(t => t.Importo > 0).Sum(t => t.Importo);
        var expenses = Math.Abs(transactions.Where(t => t.Importo < 0).Sum(t => t.Importo));
        var savings = transactions.Sum(t => t.Importo);
        var count = transactions.Count;

        return (income, expenses, savings, count);
    }

    #endregion

    /// <summary>
    /// Ensure database is initialized
    /// </summary>
    private void EnsureInitialized()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        }
    }

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

            _loggingService.LogInfo($"Running VACUUM on database (size before: {sizeBefore / 1024.0:F1} KB)");

            await _connection!.ExecuteAsync("VACUUM");

            sizeBeforeFile.Refresh();
            var sizeAfter = sizeBeforeFile.Length;
            var savedBytes = sizeBefore - sizeAfter;

            _loggingService.LogInfo($"VACUUM completed (size after: {sizeAfter / 1024.0:F1} KB, saved: {savedBytes / 1024.0:F1} KB)");

            return savedBytes;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error running VACUUM", ex);
            throw;
        }
    }

    /// <summary>
    /// Close database connection
    /// </summary>
    public async Task CloseAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection = null;
            _loggingService.LogInfo($"Database closed for account {_currentAccountId}");
        }
    }
}
