using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Backup;
using MoneyMindApp.Models;
using MoneyMindApp.Models.Sync;
using MoneyMindApp.Helpers;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace MoneyMindApp.Services.Sync;

/// <summary>
/// Implementation of WiFi sync service using embedded HTTP server
/// Mobile device acts as server, desktop app connects as client
/// Supports: WiFi, Mobile Hotspot
/// Sync modes: Replace, Merge, NewOnly
/// </summary>
public class WiFiSyncService : IWiFiSyncService
{
    private readonly ILoggingService _loggingService;
    private readonly DatabaseService _databaseService;
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly IBackupService _backupService;
    private IWebHost? _webHost;
    private bool _isRunning = false;
    private int _currentPort = 8765;

    public bool IsServerRunning => _isRunning;

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

    /// <summary>
    /// Start HTTP server for WiFi sync
    /// </summary>
    public async Task<bool> StartServerAsync(int port = 8765)
    {
        if (_isRunning)
        {
            _loggingService.LogWarning("WiFi sync server already running");
            return true;
        }

        try
        {
            _currentPort = port;

            _loggingService.LogInfo($"Starting WiFi sync server on port {port}");

            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, port);
                })
                .Configure(app =>
                {
                    app.Run(async context => await HandleRequestAsync(context));
                })
                .Build();

            await _webHost.StartAsync();

            _isRunning = true;

            var ipAddress = await GetDeviceIPAddressAsync();
            _loggingService.LogInfo($"WiFi sync server started successfully at http://{ipAddress}:{port}");

            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to start WiFi sync server", ex);
            return false;
        }
    }

    /// <summary>
    /// Stop HTTP server
    /// </summary>
    public async Task StopServerAsync()
    {
        if (!_isRunning || _webHost == null)
        {
            return;
        }

        try
        {
            _loggingService.LogInfo("Stopping WiFi sync server");

            await _webHost.StopAsync();
            _webHost.Dispose();
            _webHost = null;

            _isRunning = false;

            _loggingService.LogInfo("WiFi sync server stopped");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error stopping WiFi sync server", ex);
        }
    }

    /// <summary>
    /// Get device IP address (WiFi or Hotspot)
    /// </summary>
    public async Task<string?> GetDeviceIPAddressAsync()
    {
        try
        {
            // Try to get WiFi IP first
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(ip));

            if (ipAddress != null)
            {
                _loggingService.LogDebug($"Device IP address: {ipAddress}");
                return ipAddress.ToString();
            }

            // Fallback: try to get from network interfaces
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    var properties = ni.GetIPProperties();
                    var address = properties.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                           !IPAddress.IsLoopback(a.Address));

                    if (address != null)
                    {
                        return address.Address.ToString();
                    }
                }
            }

            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error getting device IP address", ex);
            return null;
        }
    }

    /// <summary>
    /// Get sync statistics
    /// </summary>
    public async Task<SyncStatistics> GetSyncStatisticsAsync()
    {
        await Task.CompletedTask;

        var lastSyncTimeStr = Preferences.Get("last_sync_time", string.Empty);
        DateTime? lastSyncTime = null;
        if (!string.IsNullOrEmpty(lastSyncTimeStr) && DateTime.TryParse(lastSyncTimeStr, out var parsedTime))
        {
            lastSyncTime = parsedTime;
        }

        return new SyncStatistics
        {
            LastSyncTime = lastSyncTime,
            TransactionsSent = Preferences.Get("transactions_sent", 0),
            TransactionsReceived = Preferences.Get("transactions_received", 0),
            Success = true
        };
    }

    #region HTTP Request Handlers

    /// <summary>
    /// Handle incoming HTTP requests
    /// </summary>
    private async Task HandleRequestAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        _loggingService.LogDebug($"WiFi sync request: {context.Request.Method} {path}");

        try
        {
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
                    await HandleGetTransactionsForAccountAsync(context);
                    break;

                case "/transactions":
                    if (context.Request.Method == "GET")
                        await HandleGetTransactionsAsync(context);
                    else if (context.Request.Method == "POST")
                        await HandlePostTransactionsAsync(context);
                    break;

                case "/sync/prepare":
                    if (context.Request.Method == "POST")
                        await HandleSyncPrepareAsync(context);
                    else
                    {
                        context.Response.StatusCode = 405;
                        await context.Response.WriteAsync("Method not allowed");
                    }
                    break;

                case "/sync/execute":
                    if (context.Request.Method == "POST")
                        await HandleSyncExecuteAsync(context);
                    else
                    {
                        context.Response.StatusCode = 405;
                        await context.Response.WriteAsync("Method not allowed");
                    }
                    break;

                default:
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Endpoint not found");
                    break;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error handling request {path}", ex);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle /ping endpoint (check if server is alive)
    /// </summary>
    private async Task HandlePingAsync(HttpContext context)
    {
        var response = new
        {
            success = true,
            status = "ok",
            timestamp = DateTime.Now,
            device = DeviceInfo.Model,
            platform = DeviceInfo.Platform.ToString()
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }

    /// <summary>
    /// Handle /info endpoint (device and app info)
    /// </summary>
    private async Task HandleInfoAsync(HttpContext context)
    {
        var response = new
        {
            appName = "MoneyMind",
            appVersion = AppInfo.VersionString,
            device = DeviceInfo.Model,
            platform = DeviceInfo.Platform.ToString(),
            osVersion = DeviceInfo.VersionString,
            serverPort = _currentPort,
            isRunning = _isRunning
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }

    /// <summary>
    /// Handle GET /accounts - Return all accounts with transaction info
    /// </summary>
    private async Task HandleGetAccountsAsync(HttpContext context)
    {
        try
        {
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            var syncAccounts = new List<SyncAccount>();

            foreach (var account in accounts)
            {
                // Create a separate DatabaseService instance for each account to avoid conflicts
                // with the app's current account database connection
                var accountDbService = new DatabaseService(_loggingService, new DatabaseMigrationService(_loggingService));
                await accountDbService.InitializeAsync(account.Id);
                var transactions = await accountDbService.GetAllTransactionsAsync();
                var latestDate = transactions.Any()
                    ? transactions.Max(t => t.Data).ToString("yyyy-MM-dd")
                    : null;
                var latestModified = transactions.Any()
                    ? transactions.Max(t => t.ModifiedAt ?? t.CreatedAt)
                    : (DateTime?)null;

                syncAccounts.Add(new SyncAccount
                {
                    Id = account.Id,
                    Nome = account.Nome,
                    SaldoIniziale = account.SaldoIniziale,
                    Icona = account.Icona,
                    Colore = account.Colore,
                    TransactionCount = transactions.Count,
                    LatestTransactionDate = latestDate,
                    LatestModifiedAt = latestModified,
                    DatabaseFile = $"MoneyMind_Conto_{account.Id:D3}.db"
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

    /// <summary>
    /// Handle GET /transactions/{accountId} - Return transactions for specific account
    /// </summary>
    private async Task HandleGetTransactionsForAccountAsync(HttpContext context)
    {
        try
        {
            var path = context.Request.Path.Value ?? "";
            var accountIdStr = path.Split('/').LastOrDefault();

            if (!int.TryParse(accountIdStr, out var accountId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = "Invalid account ID" }));
                return;
            }

            // Create separate instance to avoid conflicts
            var accountDbService = new DatabaseService(_loggingService, new DatabaseMigrationService(_loggingService));
            await accountDbService.InitializeAsync(accountId);
            var transactions = await accountDbService.GetAllTransactionsAsync();

            var syncTransactions = transactions.Select(SyncHelper.ToSyncTransaction).ToList();

            var response = new
            {
                success = true,
                accountId = accountId,
                transactions = syncTransactions,
                count = syncTransactions.Count
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error in GET /transactions/{accountId}", ex);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
        }
    }

    /// <summary>
    /// Handle GET /transactions (send all transactions to desktop)
    /// </summary>
    private async Task HandleGetTransactionsAsync(HttpContext context)
    {
        try
        {
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            var allTransactions = new List<object>();

            foreach (var account in accounts)
            {
                // Create separate instance to avoid conflicts
                var accountDbService = new DatabaseService(_loggingService, new DatabaseMigrationService(_loggingService));
                await accountDbService.InitializeAsync(account.Id);
                var transactions = await accountDbService.GetAllTransactionsAsync();

                foreach (var t in transactions)
                {
                    allTransactions.Add(new
                    {
                        accountId = account.Id,
                        accountName = account.Nome,
                        transaction = SyncHelper.ToSyncTransaction(t)
                    });
                }
            }

            var response = new
            {
                success = true,
                transactions = allTransactions,
                count = allTransactions.Count
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error in GET /transactions", ex);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
        }
    }

    /// <summary>
    /// Handle POST /transactions (receive transactions from desktop)
    /// </summary>
    private async Task HandlePostTransactionsAsync(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        _loggingService.LogInfo($"Received transactions from desktop: {body.Length} bytes");

        var response = new
        {
            success = true,
            imported = 0,
            message = "Use /sync/execute endpoint for full sync functionality"
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }

    /// <summary>
    /// Handle POST /sync/prepare - Prepare sync and create backup
    /// </summary>
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
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = "Invalid request body" }));
                return;
            }

            _loggingService.LogInfo($"Sync prepare request: Direction={request.Direction}, Mode={request.Mode}, Accounts={request.SourceAccounts?.Count ?? 0}");

            // Crea backup automatico
            var backupResult = await _backupService.CreateBackupAsync(
                "pre_sync",
                request.Direction.ToString());

            // Confronta dati
            var comparisons = new List<SyncComparison>();
            var mobileAccounts = await _globalDatabaseService.GetAllAccountsAsync();
            var hasClassificationWarning = false;
            var totalClassifiedTransactions = 0;

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

                // Check for classification warning (Mobile→Desktop + Replace mode)
                if (request.Direction == SyncDirection.MobileToDesktop &&
                    request.Mode == SyncMode.Replace &&
                    sourceAccount.ClassifiedCount > 0)
                {
                    hasClassificationWarning = true;
                    totalClassifiedTransactions += sourceAccount.ClassifiedCount;
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
                    DestClassifiedCount = sourceAccount.ClassifiedCount,
                    HasWarning = warning != null || (request.Direction == SyncDirection.MobileToDesktop && request.Mode == SyncMode.Replace && sourceAccount.ClassifiedCount > 0),
                    WarningMessage = warning ?? (sourceAccount.ClassifiedCount > 0 && request.Direction == SyncDirection.MobileToDesktop && request.Mode == SyncMode.Replace
                        ? $"Desktop ha {sourceAccount.ClassifiedCount} transazioni classificate che verranno perse!"
                        : null)
                });
            }

            var response = new SyncPrepareResponse
            {
                Success = true,
                BackupCreated = backupResult.Success,
                BackupPath = backupResult.BackupPath,
                Comparisons = comparisons,
                RequiresConfirmation = comparisons.Any(c => c.HasWarning),
                HasClassificationWarning = hasClassificationWarning,
                TotalClassifiedTransactions = totalClassifiedTransactions
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

    /// <summary>
    /// Handle POST /sync/execute - Execute sync with selected mode
    /// </summary>
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

            _loggingService.LogInfo($"Sync execute request: Direction={request.Direction}, Mode={request.Mode}, Accounts={request.Accounts?.Count ?? 0}");

            // Log TargetAccountId per debug
            if (request.Accounts != null && request.Accounts.Count > 0)
            {
                foreach (var acc in request.Accounts)
                {
                    _loggingService.LogInfo($"Received Account: ID={acc.Id}, Name={acc.Nome}, TargetAccountId={acc.TargetAccountId?.ToString() ?? "NULL"}");
                }
            }

            var results = new List<SyncAccountResult>();
            int totalProcessed = 0, totalDuplicates = 0, totalNew = 0;

            foreach (var accountData in request.Accounts)
            {
                var result = await ProcessAccountSyncAsync(accountData, request.Mode, request.Direction);
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
            Preferences.Set("transactions_received", Preferences.Get("transactions_received", 0) + totalNew);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

            _loggingService.LogInfo($"Sync completed: {results.Count} accounts, {totalNew} new transactions, {totalDuplicates} duplicates skipped");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error in POST /sync/execute", ex);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { success = false, error = ex.Message }));
        }
    }

    #endregion

    #region Sync Processing

    /// <summary>
    /// Process sync for a single account
    /// </summary>
    private async Task<SyncAccountResult> ProcessAccountSyncAsync(SyncAccount accountData, SyncMode mode, SyncDirection direction)
    {
        // Log entry point per debug
        _loggingService.LogInfo($"ProcessAccountSyncAsync - Start: ID={accountData.Id}, Name={accountData.Nome}, Mode={mode}, Direction={direction}, TargetAccountId={accountData.TargetAccountId?.ToString() ?? "NULL"}");

        var result = new SyncAccountResult
        {
            AccountId = accountData.Id,
            AccountName = accountData.Nome
        };

        try
        {
            BankAccount account;

            // Modalità CreateNew: crea un nuovo account SOLO se la destinazione è questo dispositivo (Mobile)
            // Se Direction = MobileToDesktop, il Mobile è la SORGENTE, quindi NON deve creare nulla localmente
            if ((mode == SyncMode.CreateNew || accountData.TargetAccountId == null) && direction == SyncDirection.DesktopToMobile)
            {
                _loggingService.LogInfo($"CreateNew mode (DesktopToMobile): creating new account '{accountData.Nome}' on Mobile");

                account = new BankAccount
                {
                    Nome = accountData.Nome,
                    SaldoIniziale = accountData.SaldoIniziale,
                    Icona = accountData.Icona ?? "💳",
                    Colore = accountData.Colore ?? "#6750A4",
                    CreatedAt = DateTime.Now
                };
                await _globalDatabaseService.InsertAccountAsync(account);
                result.Status = "created";
                _loggingService.LogInfo($"Created new account: {account.Nome} (ID: {account.Id})");
            }
            else if (mode == SyncMode.CreateNew && direction == SyncDirection.MobileToDesktop)
            {
                // MobileToDesktop + CreateNew: il Mobile è la SORGENTE, non deve creare nulla localmente
                // Invece, legge i propri dati e li restituisce al Desktop che li creerà localmente
                _loggingService.LogInfo($"CreateNew mode (MobileToDesktop): Mobile is SOURCE, reading data to send to Desktop.");

                // Leggi account Mobile
                var mobileAccount = await _globalDatabaseService.GetAccountByIdAsync(accountData.Id);
                if (mobileAccount == null)
                {
                    result.Status = "error";
                    result.ErrorMessage = $"Mobile account ID={accountData.Id} not found";
                    _loggingService.LogError($"Mobile account {accountData.Id} not found", null);
                    return result;
                }

                // Leggi transazioni
                await _databaseService.InitializeAsync(mobileAccount.Id);
                var transactions = await _databaseService.GetAllTransactionsAsync();

                // Converti a SyncTransaction
                var syncTransactions = transactions.Select(t => new SyncTransaction
                {
                    Data = t.Data.ToString("yyyy-MM-dd"),
                    Importo = t.Importo,
                    Descrizione = t.Descrizione,
                    Causale = t.Causale ?? string.Empty,
                    CreatedAt = t.CreatedAt,
                    ModifiedAt = t.ModifiedAt
                }).ToList();

                // Popola AccountData per restituirlo al Desktop
                result.AccountData = new SyncAccount
                {
                    Id = mobileAccount.Id,
                    Nome = mobileAccount.Nome,
                    SaldoIniziale = mobileAccount.SaldoIniziale,
                    Icona = mobileAccount.Icona,
                    Colore = mobileAccount.Colore,
                    TransactionCount = syncTransactions.Count,
                    Transactions = syncTransactions
                };

                result.Status = "source_only";
                result.NewTransactionCount = syncTransactions.Count;
                _loggingService.LogInfo($"Prepared account data for Desktop: {mobileAccount.Nome}, {syncTransactions.Count} transactions");
                return result;
            }
            else if (direction == SyncDirection.MobileToDesktop)
            {
                // MobileToDesktop (Replace/Merge/NewOnly): Mobile è SORGENTE, legge i suoi dati e li restituisce al Desktop
                _loggingService.LogInfo($"MobileToDesktop mode: Mobile is SOURCE, reading data to send to Desktop (Mode={mode})");

                // Leggi account Mobile (SOURCE)
                var mobileAccount = await _globalDatabaseService.GetAccountByIdAsync(accountData.Id);
                if (mobileAccount == null)
                {
                    result.Status = "error";
                    result.ErrorMessage = $"Mobile account ID={accountData.Id} not found";
                    _loggingService.LogError($"Mobile account {accountData.Id} not found", null);
                    return result;
                }

                // Leggi transazioni Mobile
                await _databaseService.InitializeAsync(mobileAccount.Id);
                var mobileTransactions = await _databaseService.GetAllTransactionsAsync();

                // Converti a SyncTransaction
                var syncTransactions = mobileTransactions.Select(t => new SyncTransaction
                {
                    Data = t.Data.ToString("yyyy-MM-dd"),
                    Importo = t.Importo,
                    Descrizione = t.Descrizione,
                    Causale = t.Causale ?? string.Empty,
                    CreatedAt = t.CreatedAt,
                    ModifiedAt = t.ModifiedAt
                }).ToList();

                // Popola AccountData con TUTTE le transazioni del Mobile
                result.AccountData = new SyncAccount
                {
                    Id = mobileAccount.Id,
                    Nome = mobileAccount.Nome,
                    SaldoIniziale = mobileAccount.SaldoIniziale,
                    Icona = mobileAccount.Icona,
                    Colore = mobileAccount.Colore,
                    TransactionCount = syncTransactions.Count,
                    Transactions = syncTransactions,
                    TargetAccountId = accountData.TargetAccountId  // Passa il mapping al Desktop
                };

                result.Status = "source_only";
                result.NewTransactionCount = syncTransactions.Count;
                _loggingService.LogInfo($"Prepared {syncTransactions.Count} transactions from Mobile account '{mobileAccount.Nome}' to send to Desktop");
                return result;
            }
            else
            {
                // DesktopToMobile: Desktop è SORGENTE, Mobile è DESTINAZIONE
                // Usa TargetAccountId per trovare l'account di destinazione sul Mobile
                _loggingService.LogInfo($"DesktopToMobile: Mapping Source ID={accountData.Id} → Target ID={accountData.TargetAccountId}");

                account = await _globalDatabaseService.GetAccountByIdAsync(accountData.TargetAccountId.Value);

                if (account == null)
                {
                    result.Status = "error";
                    result.ErrorMessage = $"Target account ID={accountData.TargetAccountId} not found";
                    _loggingService.LogError($"Target account {accountData.TargetAccountId} not found for mapping", null);
                    return result;
                }

                _loggingService.LogInfo($"Found target account: {account.Nome} (ID: {account.Id})");

                await _databaseService.InitializeAsync(account.Id);
                var existingTransactions = await _databaseService.GetAllTransactionsAsync();
                result.PreviousTransactionCount = existingTransactions.Count;

                switch (mode)
                {
                    case SyncMode.CreateNew:
                        // Per CreateNew, inserisci tutte le transazioni (già creato il conto sopra)
                        await ExecuteReplaceAsync(accountData, account.Id);
                        result.NewTransactionCount = accountData.Transactions.Count;
                        break;

                    case SyncMode.Replace:
                        await ExecuteReplaceAsync(accountData, account.Id);
                        result.NewTransactionCount = accountData.Transactions.Count;
                        if (string.IsNullOrEmpty(result.Status))
                            result.Status = "replaced";
                        break;

                    case SyncMode.Merge:
                        var mergeResult = await ExecuteMergeAsync(accountData, account.Id, existingTransactions);
                        result.NewTransactionCount = existingTransactions.Count + mergeResult.added;
                        result.DuplicatesSkipped = mergeResult.skipped;
                        if (string.IsNullOrEmpty(result.Status))
                            result.Status = "merged";
                        break;

                    case SyncMode.NewOnly:
                        var newOnlyResult = await ExecuteNewOnlyAsync(accountData, account.Id, existingTransactions);
                        result.NewTransactionCount = existingTransactions.Count + newOnlyResult;
                        result.NewOnlyAdded = newOnlyResult;
                        if (string.IsNullOrEmpty(result.Status))
                            result.Status = "new_only";
                        break;
                }
            }

            _loggingService.LogInfo($"Account {account.Nome} sync completed: mode={mode}, prev={result.PreviousTransactionCount}, new={result.NewTransactionCount}, skipped={result.DuplicatesSkipped}");
        }
        catch (Exception ex)
        {
            result.Status = "error";
            result.ErrorMessage = ex.Message;
            _loggingService.LogError($"Error processing account {accountData.Id}", ex);
        }

        return result;
    }

    /// <summary>
    /// Execute REPLACE mode - Delete all and import from source
    /// </summary>
    private async Task ExecuteReplaceAsync(SyncAccount accountData, int accountId)
    {
        // Cancella tutte le transazioni esistenti
        var existing = await _databaseService.GetAllTransactionsAsync();
        foreach (var t in existing)
        {
            await _databaseService.DeleteTransactionAsync(t.Id);
        }

        _loggingService.LogInfo($"Deleted {existing.Count} existing transactions for REPLACE mode");

        // Inserisce tutte le nuove
        foreach (var syncTx in accountData.Transactions)
        {
            var transaction = SyncHelper.ToTransaction(syncTx, accountId);
            await _databaseService.InsertTransactionAsync(transaction);
        }

        _loggingService.LogInfo($"Inserted {accountData.Transactions.Count} transactions");
    }

    /// <summary>
    /// Execute MERGE mode - Add only non-duplicates
    /// </summary>
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

        _loggingService.LogInfo($"MERGE completed: {added} added, {skipped} duplicates skipped");
        return (added, skipped);
    }

    /// <summary>
    /// Execute NEWONLY mode - Add only transactions newer than latest existing
    /// </summary>
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

        _loggingService.LogInfo($"NEWONLY completed: {newTransactions.Count} transactions added (cutoff: {latestDate?.ToString("yyyy-MM-dd") ?? "none"})");
        return newTransactions.Count;
    }

    /// <summary>
    /// Generate human-readable sync message
    /// </summary>
    private string GenerateSyncMessage(SyncMode mode, List<SyncAccountResult> results)
    {
        var totalAccounts = results.Count;
        var successCount = results.Count(r => r.Status != "error");
        var errorCount = results.Count(r => r.Status == "error");

        var message = mode switch
        {
            SyncMode.Replace => $"Sostituite transazioni in {successCount}/{totalAccounts} conti",
            SyncMode.Merge => $"Uniti {results.Sum(r => r.NewTransactionCount - r.PreviousTransactionCount)} transazioni, {results.Sum(r => r.DuplicatesSkipped)} duplicati saltati",
            SyncMode.NewOnly => $"Aggiunte {results.Sum(r => r.NewOnlyAdded)} nuove transazioni",
            _ => "Sync completato"
        };

        if (errorCount > 0)
        {
            message += $" ({errorCount} errori)";
        }

        return message;
    }

    #endregion
}
