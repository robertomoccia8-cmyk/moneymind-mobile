namespace MoneyMindApp.Services.Sync;

/// <summary>
/// Service for WiFi synchronization between mobile and desktop
/// Mobile acts as HTTP server, desktop as client
/// </summary>
public interface IWiFiSyncService
{
    /// <summary>
    /// Start HTTP server for WiFi sync
    /// </summary>
    Task<bool> StartServerAsync(int port = 8765);

    /// <summary>
    /// Stop HTTP server
    /// </summary>
    Task StopServerAsync();

    /// <summary>
    /// Check if server is running
    /// </summary>
    bool IsServerRunning { get; }

    /// <summary>
    /// Get device IP address for WiFi sync
    /// </summary>
    Task<string?> GetDeviceIPAddressAsync();

    /// <summary>
    /// Get sync statistics
    /// </summary>
    Task<SyncStatistics> GetSyncStatisticsAsync();
}

/// <summary>
/// Sync statistics model
/// </summary>
public class SyncStatistics
{
    public DateTime? LastSyncTime { get; set; }
    public int TransactionsSent { get; set; }
    public int TransactionsReceived { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
