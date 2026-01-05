using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel for Main Dashboard Page
/// Shows account statistics and recent transactions
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;
    private readonly ISalaryPeriodService _salaryPeriodService;

    [ObservableProperty]
    private AccountStatistics? statistics;

    [ObservableProperty]
    private ObservableCollection<Transaction> recentTransactions = new();

    [ObservableProperty]
    private BankAccount? currentAccount;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool valuesVisible = true;

    [ObservableProperty]
    private bool isMenuVisible = false;

    [ObservableProperty]
    private string welcomeMessage = "Benvenuto";

    [ObservableProperty]
    private string currentSalaryMonth = string.Empty;

    public MainViewModel(
        DatabaseService databaseService,
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService,
        ISalaryPeriodService salaryPeriodService)
    {
        _databaseService = databaseService;
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
        _salaryPeriodService = salaryPeriodService;

        // Set welcome message with time of day
        var hour = DateTime.Now.Hour;
        WelcomeMessage = hour < 12 ? "Buongiorno" :
                        hour < 18 ? "Buon pomeriggio" :
                        "Buonasera";
    }

    /// <summary>
    /// Initialize and load data
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            _loggingService.LogInfo("MainViewModel initializing...");

            // Initialize global database
            await _globalDatabaseService.InitializeAsync();

            // Get or create first account
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            if (accounts.Count == 0)
            {
                _loggingService.LogInfo("No accounts found, creating default account");
                await CreateDefaultAccountAsync();
                accounts = await _globalDatabaseService.GetAllAccountsAsync();
            }

            // Get current account from Preferences or use first
            var savedAccountId = Preferences.Get("current_account_id", 0);
            if (savedAccountId > 0)
            {
                CurrentAccount = accounts.FirstOrDefault(a => a.Id == savedAccountId);
            }

            // Fallback to first account if saved one not found
            if (CurrentAccount == null)
            {
                CurrentAccount = accounts.FirstOrDefault();
                if (CurrentAccount != null)
                {
                    Preferences.Set("current_account_id", CurrentAccount.Id);
                }
            }

            if (CurrentAccount != null)
            {
                _loggingService.LogInfo($"Loading account: {CurrentAccount.Nome} (ID: {CurrentAccount.Id})");

                // Initialize account database
                await _databaseService.InitializeAsync(CurrentAccount.Id);

                // Update last accessed
                await _globalDatabaseService.UpdateLastAccessedAsync(CurrentAccount.Id);

                // Load statistics and transactions
                await LoadDashboardDataAsync();
            }

            _loggingService.LogInfo("MainViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing MainViewModel", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load dashboard statistics and recent transactions
    /// </summary>
    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            IsLoading = true;

            if (CurrentAccount == null)
            {
                _loggingService.LogWarning("Cannot load dashboard data: no current account");
                return;
            }

            // Calculate salary period (mese stipendiale)
            var (startDate, endDate) = await _salaryPeriodService.GetCurrentPeriodAsync();

            // Calculate current salary month name (e.g., "Ottobre 2025")
            var italianCulture = new System.Globalization.CultureInfo("it-IT");
            CurrentSalaryMonth = startDate.ToString("MMMM yyyy", italianCulture);

            // Get statistics
            var (income, expenses, savings, count) = await _databaseService.GetStatisticsAsync(startDate, endDate);

            // Calculate total balance
            var totalBalance = await _databaseService.GetTotalBalanceAsync(CurrentAccount.SaldoIniziale);

            Statistics = new AccountStatistics
            {
                TotalBalance = totalBalance,
                Income = income,
                Expenses = expenses,
                Savings = savings,
                TransactionCount = count,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };

            // Load recent transactions (last 10)
            var allTransactions = await _databaseService.GetTransactionsAsync(startDate, endDate);
            RecentTransactions = new ObservableCollection<Transaction>(
                allTransactions.OrderByDescending(t => t.Data).Take(10)
            );

            _loggingService.LogInfo($"Dashboard loaded: {count} transactions, balance: {totalBalance:C2}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading dashboard data", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggle values visibility (show/hide amounts)
    /// </summary>
    [RelayCommand]
    private void ToggleValuesVisibility()
    {
        ValuesVisible = !ValuesVisible;
        IsMenuVisible = false; // Close menu after toggle
        _loggingService.LogDebug($"Values visibility toggled: {ValuesVisible}");
    }

    /// <summary>
    /// Toggle menu visibility
    /// </summary>
    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuVisible = !IsMenuVisible;
        _loggingService.LogDebug($"Menu visibility toggled: {IsMenuVisible}");
    }

    /// <summary>
    /// Close menu
    /// </summary>
    [RelayCommand]
    private void CloseMenu()
    {
        IsMenuVisible = false;
        _loggingService.LogDebug("Menu closed");
    }

    /// <summary>
    /// Navigate to transactions page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToTransactionsAsync()
    {
        await Shell.Current.GoToAsync("//transactions");
    }

    /// <summary>
    /// Navigate to account selection page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAccountSelectionAsync()
    {
        IsMenuVisible = false; // Close menu before navigation
        await Shell.Current.GoToAsync("//accounts");
    }

    /// <summary>
    /// Navigate to settings page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        await Shell.Current.GoToAsync("//settings");
    }

    /// <summary>
    /// Navigate to add transaction page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAddTransactionAsync()
    {
        await Shell.Current.GoToAsync("addtransaction");
    }

    /// <summary>
    /// Navigate to salary configuration page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToSalaryConfigAsync()
    {
        IsMenuVisible = false; // Close menu before navigation
        await Shell.Current.GoToAsync("salaryconfig");
    }

    /// <summary>
    /// Refresh dashboard (pull to refresh)
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Create default account if none exists
    /// </summary>
    private async Task CreateDefaultAccountAsync()
    {
        try
        {
            var defaultAccount = new BankAccount
            {
                Nome = "Conto Principale",
                Icona = "💳",
                Colore = "#512BD4",
                SaldoIniziale = 0m,
                CreatedAt = DateTime.Now
            };

            await _globalDatabaseService.InsertAccountAsync(defaultAccount);
            _loggingService.LogInfo($"Default account created: {defaultAccount.Nome}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating default account", ex);
        }
    }

    /// <summary>
    /// Get display text for value (visible or hidden)
    /// </summary>
    public string GetDisplayValue(string value)
    {
        return ValuesVisible ? value : "****";
    }
}
