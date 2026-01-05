using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel for Account Selection Page
/// Shows all accounts with balance and allows switching
/// </summary>
public partial class AccountSelectionViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ObservableCollection<BankAccount> accounts = new();

    [ObservableProperty]
    private BankAccount? selectedAccount;

    [ObservableProperty]
    private bool isLoading;

    private bool _isInitialized;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    public AccountSelectionViewModel(
        GlobalDatabaseService globalDatabaseService,
        DatabaseService databaseService,
        ILoggingService loggingService)
    {
        _globalDatabaseService = globalDatabaseService;
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Initialize and load accounts (called on first load or when data needs refresh)
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Guard against multiple simultaneous calls
            if (IsLoading)
            {
                _loggingService.LogDebug("InitializeAsync already in progress, skipping");
                return;
            }

            IsLoading = true;

            await _globalDatabaseService.InitializeAsync();
            await LoadAccountsAsync();

            var currentAccountId = Preferences.Get("current_account_id", 0);
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == currentAccountId);

            _isInitialized = true;
            _loggingService.LogInfo("AccountSelectionViewModel initialized");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing AccountSelectionViewModel", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load all accounts with calculated balances
    /// </summary>
    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        // Use semaphore to prevent concurrent executions
        if (!await _loadSemaphore.WaitAsync(0))
        {
            _loggingService.LogDebug("LoadAccountsAsync already running, skipping duplicate call");
            return;
        }

        try
        {
            IsLoading = true;

            _loggingService.LogDebug("LoadAccountsAsync starting - clearing existing accounts");

            // Clear existing accounts first to avoid showing stale data
            Accounts.Clear();

            var loadedAccounts = await _globalDatabaseService.GetAllAccountsAsync();

            _loggingService.LogDebug($"Found {loadedAccounts.Count} accounts in database");

            // Calculate current balance for each account
            // IMPORTANT: Create SEPARATE DatabaseService instance for each account
            // to avoid race conditions and state conflicts with the shared singleton
            foreach (var account in loadedAccounts)
            {
                // Create separate instance to avoid conflicts (same pattern as WiFiSyncService)
                var accountDbService = new DatabaseService(_loggingService, new DatabaseMigrationService(_loggingService));
                await accountDbService.InitializeAsync(account.Id);
                account.SaldoCorrente = await accountDbService.GetTotalBalanceAsync(account.SaldoIniziale);

                _loggingService.LogDebug($"Account {account.Nome} (ID={account.Id}): SaldoIniziale={account.SaldoIniziale:C2}, SaldoCorrente={account.SaldoCorrente:C2}");

                // Add account to collection AFTER balance is calculated
                Accounts.Add(account);
            }

            _loggingService.LogInfo($"Loaded {Accounts.Count} accounts with calculated balances");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading accounts", ex);
        }
        finally
        {
            IsLoading = false;
            _loadSemaphore.Release();
        }
    }

    /// <summary>
    /// Select account and navigate back to dashboard
    /// </summary>
    [RelayCommand]
    private async Task SelectAccountAsync(BankAccount account)
    {
        try
        {
            var previousAccountId = Preferences.Get("current_account_id", 0);
            
            SelectedAccount = account;

            // Save current account ID to preferences
            Preferences.Set("current_account_id", account.Id);

            // Update last accessed timestamp
            await _globalDatabaseService.UpdateLastAccessedAsync(account.Id);

            _loggingService.LogInfo($"Account selected: {account.Nome} (ID: {account.Id})");

            // ✅ Notify ALL ViewModels that account changed
            if (previousAccountId != account.Id)
            {
                MessagingCenter.Send(this, "AccountChanged", account.Id);
                _loggingService.LogDebug($"Sent AccountChanged message: {account.Id}");
            }

            // Navigate back to main page
            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error selecting account", ex);
        }
    }

    /// <summary>
    /// Navigate to add account page
    /// </summary>
    [RelayCommand]
    private async Task AddAccountAsync()
    {
        await Shell.Current.GoToAsync("addaccount");
    }

    /// <summary>
    /// Edit account
    /// </summary>
    [RelayCommand]
    private async Task EditAccountAsync(BankAccount account)
    {
        await Shell.Current.GoToAsync($"editaccount?AccountId={account.Id}");
    }

    /// <summary>
    /// Delete account (with confirmation)
    /// </summary>
    [RelayCommand]
    private async Task DeleteAccountAsync(BankAccount account)
    {
        try
        {
            // Don't allow deleting last account
            if (Accounts.Count == 1)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Impossibile Eliminare",
                    "Non puoi eliminare l'ultimo conto. Crea prima un nuovo conto.",
                    "OK");
                return;
            }

            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Conferma Eliminazione",
                $"Vuoi eliminare il conto '{account.Nome}'? Tutti i dati associati saranno persi.",
                "Elimina",
                "Annulla");

            if (!confirm)
                return;

            await _globalDatabaseService.DeleteAccountAsync(account.Id);

            Accounts.Remove(account);

            // If deleted account was selected, select another
            if (SelectedAccount?.Id == account.Id)
            {
                var newAccount = Accounts.FirstOrDefault();
                if (newAccount != null)
                {
                    await SelectAccountAsync(newAccount);
                }
            }

            _loggingService.LogInfo($"Account deleted: {account.Nome} (ID: {account.Id})");

            await Application.Current.MainPage.DisplayAlert("Successo", "Conto eliminato", "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting account", ex);
            await Application.Current!.MainPage!.DisplayAlert("Errore", "Impossibile eliminare il conto", "OK");
        }
    }

    /// <summary>
    /// Refresh accounts (pull to refresh)
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAccountsAsync();
    }
}
