using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

/// <summary>
/// ViewModel for Transactions List Page
/// Supports CRUD operations, search, filters
/// </summary>
public partial class TransactionsViewModel : ObservableObject, IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;
    private readonly ISalaryPeriodService _salaryPeriodService;

    // ✅ Performance optimization fields
    private System.Timers.Timer? _searchDebounceTimer;
    private CancellationTokenSource? _filterCancellationTokenSource;
    private Dictionary<string, List<TransactionGroup>> _groupingCache = new();

    // ✅ Infinite scroll / pagination fields
    private const int DefaultPageSize = 100; // Load 100 transactions at a time
    private int _pageSize = DefaultPageSize;
    private int _currentPage = 0;
    private List<Transaction> _allFilteredTransactions = new();

    [ObservableProperty]
    private ObservableCollection<Transaction> transactions = new();

    [ObservableProperty]
    private ObservableCollection<Transaction> filteredTransactions = new();

    [ObservableProperty]
    private Transaction? selectedTransaction;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private DateTime? startDate;

    [ObservableProperty]
    private DateTime? endDate;

    [ObservableProperty]
    private string minAmountText = string.Empty;

    [ObservableProperty]
    private string maxAmountText = string.Empty;

    [ObservableProperty]
    private int selectedTransactionType = 0; // 0 = All, 1 = Income, 2 = Expense

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoadingMore;

    [ObservableProperty]
    private bool isFiltersVisible;

    [ObservableProperty]
    private int currentAccountId;

    [ObservableProperty]
    private int activeFiltersCount;

    [ObservableProperty]
    private bool hasActiveFilters;

    [ObservableProperty]
    private bool isFabMenuOpen;

    [ObservableProperty]
    private ObservableCollection<TransactionGroup> groupedTransactions = new();

    [ObservableProperty]
    private TransactionGroupingMode groupingMode = TransactionGroupingMode.SolarMonth;

    // ✅ Multi-Select Mode Properties
    [ObservableProperty]
    private bool isMultiSelectMode;

    private ObservableCollection<Transaction> _selectedTransactions;
    public ObservableCollection<Transaction> SelectedTransactions
    {
        get => _selectedTransactions;
        set => SetProperty(ref _selectedTransactions, value);
    }

    public int SelectedCount => _selectedTransactions?.Count ?? 0;
    public string SelectionInfoText => $"{SelectedCount} selezionate";

    public string FabIcon => IsFabMenuOpen ? "✕" : "+";

    public TransactionsViewModel(
        DatabaseService databaseService,
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService,
        ISalaryPeriodService salaryPeriodService)
    {
        _databaseService = databaseService;
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
        _salaryPeriodService = salaryPeriodService;

        // ✅ Initialize multi-select collection
        _selectedTransactions = new ObservableCollection<Transaction>();

        // Subscribe to collection changes to update count properties
        _selectedTransactions.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectionInfoText));
        };

        // NO default date range - load ALL transactions by default
        // User can set filters manually if needed
        StartDate = null;
        EndDate = null;

        // Load grouping preference
        LoadGroupingPreference();

        // ✅ Subscribe to grouping mode changes from Settings
        MessagingCenter.Subscribe<SettingsViewModel, string>(this, "TransactionGroupingChanged", (sender, newGrouping) =>
        {
            _loggingService.LogInfo($"Received TransactionGroupingChanged: {newGrouping}");
            
            // Reload preference
            LoadGroupingPreference();
            
            // Clear grouping cache to force regeneration
            _groupingCache.Clear();
            
            // Re-group current transactions with new mode
            if (FilteredTransactions.Any())
            {
                _ = Task.Run(async () =>
                {
                    await GroupTransactionsAsync(FilteredTransactions.ToList());
                });
            }
        });
    }

    /// <summary>
    /// Load grouping mode from preferences
    /// </summary>
    private void LoadGroupingPreference()
    {
        var groupingSetting = Preferences.Get("transaction_grouping", "Mese Solare");
        GroupingMode = groupingSetting == "Mese Stipendiale"
            ? TransactionGroupingMode.SalaryPeriod
            : TransactionGroupingMode.SolarMonth;
    }

    /// <summary>
    /// Initialize and load transactions
    /// </summary>
    public async Task InitializeAsync(int accountId)
    {
        try
        {
            // Always reload transactions when appearing
            CurrentAccountId = accountId;
            IsLoading = true;

            // Reload grouping preference (might have changed in settings)
            LoadGroupingPreference();

            await _databaseService.InitializeAsync(accountId);
            await LoadTransactionsAsync();

            _loggingService.LogInfo($"TransactionsViewModel initialized for account {accountId}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing TransactionsViewModel", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load all transactions
    /// </summary>
    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        try
        {
            IsLoading = true;

            List<Transaction> loadedTransactions;

            if (StartDate.HasValue && EndDate.HasValue)
            {
                _loggingService.LogDebug($"Loading transactions from {StartDate:dd/MM/yyyy} to {EndDate:dd/MM/yyyy}");
                loadedTransactions = await _databaseService.GetTransactionsAsync(StartDate.Value, EndDate.Value);
            }
            else
            {
                _loggingService.LogDebug("Loading all transactions");
                loadedTransactions = await _databaseService.GetAllTransactionsAsync();
            }

            Transactions = new ObservableCollection<Transaction>(
                loadedTransactions.OrderByDescending(t => t.Data)
            );

            // Apply filters (now async and optimized)
            await ApplyFiltersAsync();

            _loggingService.LogInfo($"Loaded {Transactions.Count} transactions (filtered: {FilteredTransactions.Count})");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading transactions", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Apply search and all filters (OPTIMIZED - async with database-level filtering + pagination)
    /// </summary>
    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        try
        {
            // Cancel any previous filter operation
            _filterCancellationTokenSource?.Cancel();
            _filterCancellationTokenSource?.Dispose();
            _filterCancellationTokenSource = new CancellationTokenSource();
            var token = _filterCancellationTokenSource.Token;

            IsLoading = true;
            _currentPage = 0; // Reset to first page

            // Parse amount filters
            decimal? minAmount = null;
            decimal? maxAmount = null;

            if (!string.IsNullOrWhiteSpace(MinAmountText))
            {
                if (decimal.TryParse(MinAmountText.Replace(",", "."), out decimal min))
                    minAmount = min;
            }

            if (!string.IsNullOrWhiteSpace(MaxAmountText))
            {
                if (decimal.TryParse(MaxAmountText.Replace(",", "."), out decimal max))
                    maxAmount = max;
            }

            // ✅ Use optimized database query to get ALL filtered transactions
            var filtered = await _databaseService.GetTransactionsWithFiltersAsync(
                searchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                minAmount: minAmount,
                maxAmount: maxAmount,
                startDate: StartDate,
                endDate: EndDate,
                transactionType: SelectedTransactionType == 0 ? null : SelectedTransactionType
            );

            // Check if operation was cancelled
            if (token.IsCancellationRequested)
            {
                _loggingService.LogDebug("Filter operation cancelled");
                return;
            }

            // Store all filtered transactions for pagination
            _allFilteredTransactions = filtered;

            // ✅ Display only first page (improves initial load performance)
            var firstPage = _allFilteredTransactions.Take(_pageSize).ToList();

            // Update UI collections
            Transactions = new ObservableCollection<Transaction>(_allFilteredTransactions);
            FilteredTransactions = new ObservableCollection<Transaction>(firstPage);

            // ✅ Group only the displayed transactions (much faster!)
            await GroupTransactionsAsync(firstPage);

            // Update filter count
            UpdateActiveFiltersCount();

            _loggingService.LogDebug($"Filters applied: Showing {FilteredTransactions.Count}/{_allFilteredTransactions.Count} transactions (Page 1, Active filters: {ActiveFiltersCount})");
        }
        catch (OperationCanceledException)
        {
            _loggingService.LogDebug("Filter operation cancelled");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error applying filters", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Group transactions by month (solar or salary period) - OPTIMIZED with caching and background threading
    /// </summary>
    private async Task GroupTransactionsAsync(List<Transaction> transactions)
    {
        try
        {
            // Generate cache key based on transaction count, mode, and first/last transaction ids
            var cacheKey = $"{transactions.Count}_{GroupingMode}_{transactions.FirstOrDefault()?.Id ?? 0}_{transactions.LastOrDefault()?.Id ?? 0}";

            // ✅ Check cache first
            if (_groupingCache.TryGetValue(cacheKey, out var cachedGroups))
            {
                GroupedTransactions = new ObservableCollection<TransactionGroup>(cachedGroups);
                _loggingService.LogDebug($"Using cached grouping: {cachedGroups.Count} groups");
                return;
            }

            // ✅ Execute heavy grouping operation in background thread
            var groups = await Task.Run(async () =>
            {
                var result = new List<TransactionGroup>();

                if (GroupingMode == TransactionGroupingMode.SolarMonth)
                {
                    // Group by solar month (calendar month)
                    var monthGroups = transactions
                        .GroupBy(t => new { t.Data.Year, t.Data.Month })
                        .OrderByDescending(g => g.Key.Year)
                        .ThenByDescending(g => g.Key.Month);

                    foreach (var group in monthGroups)
                    {
                        var transactionGroup = TransactionGroup.CreateSolarMonth(
                            group.Key.Year,
                            group.Key.Month,
                            group.ToList());
                        result.Add(transactionGroup);
                    }
                }
                else // SalaryPeriod
                {
                    // Get unique months/years from transactions
                    var periods = await GetSalaryPeriodsForTransactionsAsync(transactions);

                    foreach (var period in periods)
                    {
                        var periodTransactions = transactions
                            .Where(t => t.Data >= period.Start && t.Data <= period.End)
                            .ToList();

                        if (periodTransactions.Any())
                        {
                            var transactionGroup = TransactionGroup.CreateSalaryPeriod(
                                period.Start,
                                period.End,
                                periodTransactions);
                            result.Add(transactionGroup);
                        }
                    }

                    // Sort by period start date descending
                    result = result.OrderByDescending(g => g.PeriodStart).ToList();
                }

                return result;
            });

            // ✅ Update UI on main thread
            GroupedTransactions = new ObservableCollection<TransactionGroup>(groups);

            // ✅ Save to cache
            _groupingCache[cacheKey] = groups;

            // Limit cache size (keep only last 5 entries to prevent memory bloat)
            if (_groupingCache.Count > 5)
            {
                var oldestKey = _groupingCache.Keys.First();
                _groupingCache.Remove(oldestKey);
            }

            _loggingService.LogDebug($"Created {groups.Count} transaction groups using {GroupingMode} mode");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error grouping transactions", ex);
            // Fallback: create empty groups to avoid crashes
            GroupedTransactions = new ObservableCollection<TransactionGroup>();
        }
    }

    /// <summary>
    /// Get all salary periods that contain the given transactions
    /// </summary>
    private async Task<List<(DateTime Start, DateTime End)>> GetSalaryPeriodsForTransactionsAsync(List<Transaction> transactions)
    {
        var periods = new List<(DateTime Start, DateTime End)>();
        var processedPeriods = new HashSet<string>();

        foreach (var transaction in transactions)
        {
            var period = await _salaryPeriodService.GetPeriodForDateAsync(transaction.Data);
            var periodKey = $"{period.Start:yyyyMMdd}_{period.End:yyyyMMdd}";

            if (!processedPeriods.Contains(periodKey))
            {
                processedPeriods.Add(periodKey);
                periods.Add(period);
            }
        }

        return periods.OrderByDescending(p => p.Start).ToList();
    }

    /// <summary>
    /// Update active filters count
    /// </summary>
    private void UpdateActiveFiltersCount()
    {
        int count = 0;

        if (!string.IsNullOrWhiteSpace(SearchText))
            count++;

        if (StartDate.HasValue)
            count++;

        if (EndDate.HasValue)
            count++;

        if (!string.IsNullOrWhiteSpace(MinAmountText))
            count++;

        if (!string.IsNullOrWhiteSpace(MaxAmountText))
            count++;

        if (SelectedTransactionType != 0)
            count++;

        ActiveFiltersCount = count;
        HasActiveFilters = count > 0;
    }

    /// <summary>
    /// Clear all filters
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        MinAmountText = string.Empty;
        MaxAmountText = string.Empty;
        SelectedTransactionType = 0;
        StartDate = null;  // Clear date filters - show ALL transactions
        EndDate = null;

        UpdateActiveFiltersCount();
        await LoadTransactionsAsync();

        _loggingService.LogInfo("All filters cleared - showing all transactions");
    }

    /// <summary>
    /// Toggle filters visibility
    /// </summary>
    [RelayCommand]
    private void ToggleFilters()
    {
        IsFiltersVisible = !IsFiltersVisible;
    }

    /// <summary>
    /// Delete transaction (with confirmation)
    /// </summary>
    [RelayCommand]
    private async Task DeleteTransactionAsync(Transaction transaction)
    {
        try
        {
            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Conferma Eliminazione",
                $"Vuoi eliminare la transazione '{transaction.Descrizione}' del {transaction.Data:dd/MM/yyyy}?",
                "Elimina",
                "Annulla");

            if (!confirm)
                return;

            await _databaseService.DeleteTransactionAsync(transaction.Id);

            Transactions.Remove(transaction);
            FilteredTransactions.Remove(transaction);

            // Update grouped transactions (refresh UI)
            await ApplyFiltersAsync();

            _loggingService.LogInfo($"Transaction deleted: {transaction.Id}");

            await Application.Current.MainPage.DisplayAlert("Successo", "Transazione eliminata", "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting transaction", ex);
            await Application.Current!.MainPage!.DisplayAlert("Errore", "Impossibile eliminare la transazione", "OK");
        }
    }

    /// <summary>
    /// Navigate to add transaction page
    /// </summary>
    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        CloseFabMenu();
        await Shell.Current.GoToAsync("addtransaction");
    }

    /// <summary>
    /// Navigate to edit transaction page
    /// </summary>
    [RelayCommand]
    private async Task EditTransactionAsync(Transaction transaction)
    {
        await Shell.Current.GoToAsync($"edittransaction?TransactionId={transaction.Id}");
    }

    /// <summary>
    /// Refresh transactions (pull to refresh)
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTransactionsAsync();
    }

    /// <summary>
    /// Toggle FAB menu open/close
    /// </summary>
    [RelayCommand]
    private void ToggleFabMenu()
    {
        IsFabMenuOpen = !IsFabMenuOpen;
        OnPropertyChanged(nameof(FabIcon));
    }

    /// <summary>
    /// Close FAB menu
    /// </summary>
    private void CloseFabMenu()
    {
        if (IsFabMenuOpen)
        {
            IsFabMenuOpen = false;
            OnPropertyChanged(nameof(FabIcon));
        }
    }

    [RelayCommand]
    private async Task GoToDuplicatesAsync()
    {
        CloseFabMenu();
        await Shell.Current.GoToAsync("duplicates");
    }

    [RelayCommand]
    private async Task GoToImportAsync()
    {
        CloseFabMenu();
        // Naviga al wizard Step 1: selezione configurazione
        await Shell.Current.GoToAsync("importConfigSelection");
    }

    [RelayCommand]
    private async Task GoToExportAsync()
    {
        CloseFabMenu();
        await Shell.Current.GoToAsync("export");
    }

    /// <summary>
    /// Load more transactions (infinite scroll)
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreTransactionsAsync()
    {
        // Don't load if already loading or no more transactions
        if (IsLoadingMore || FilteredTransactions.Count >= _allFilteredTransactions.Count)
            return;

        try
        {
            IsLoadingMore = true;
            _currentPage++;

            // Calculate which transactions to load
            var skip = _currentPage * _pageSize;
            var nextPage = _allFilteredTransactions.Skip(skip).Take(_pageSize).ToList();

            if (nextPage.Any())
            {
                // ✅ Add new transactions to the existing collection (infinite scroll)
                foreach (var transaction in nextPage)
                {
                    FilteredTransactions.Add(transaction);
                }

                // ✅ Regroup all currently displayed transactions
                await GroupTransactionsAsync(FilteredTransactions.ToList());

                _loggingService.LogDebug($"Loaded page {_currentPage + 1}: Showing {FilteredTransactions.Count}/{_allFilteredTransactions.Count} transactions");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading more transactions", ex);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    #region Multi-Select Commands

    /// <summary>
    /// Enable multi-select mode (called from long press or button)
    /// </summary>
    [RelayCommand]
    private void EnableMultiSelect(Transaction? transaction)
    {
        try
        {
            // ✅ Guard: Ensure collection is initialized
            if (_selectedTransactions == null)
            {
                _selectedTransactions = new ObservableCollection<Transaction>();
            }

            IsMultiSelectMode = true;

            // ✅ Create a copy to avoid "Collection was modified" exception
            var selectedCopy = _selectedTransactions.ToList();

            // Clear previous selections
            foreach (var t in selectedCopy)
            {
                t.IsSelected = false;
            }
            _selectedTransactions.Clear();

            // Add transaction to selection if provided (from long press)
            if (transaction != null)
            {
                transaction.IsSelected = true;
                _selectedTransactions.Add(transaction);
            }

            _loggingService?.LogDebug($"Multi-select mode enabled with {_selectedTransactions.Count} transaction(s)");
        }
        catch (Exception ex)
        {
            _loggingService?.LogError($"Error enabling multi-select: {ex.Message}");
            // Reset state on error
            IsMultiSelectMode = false;
            if (_selectedTransactions != null)
            {
                // ✅ Create a copy to avoid "Collection was modified" exception
                var selectedCopy = _selectedTransactions.ToList();
                foreach (var t in selectedCopy)
                {
                    t.IsSelected = false;
                }
                _selectedTransactions.Clear();
            }
        }
    }

    /// <summary>
    /// Toggle transaction selection
    /// </summary>
    [RelayCommand]
    private void ToggleTransactionSelection(Transaction transaction)
    {
        System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection CALLED - Transaction: {transaction?.Descrizione ?? "NULL"}");

        if (transaction == null)
        {
            System.Diagnostics.Debug.WriteLine("[VM] ToggleTransactionSelection - Transaction is NULL, returning");
            return;
        }

        var wasSelected = _selectedTransactions.Contains(transaction);
        System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection - Was selected: {wasSelected}, IsSelected property: {transaction.IsSelected}");

        if (wasSelected)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection - DESELECTING transaction");
            transaction.IsSelected = false;
            _selectedTransactions.Remove(transaction);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection - SELECTING transaction");
            transaction.IsSelected = true;
            _selectedTransactions.Add(transaction);
        }

        System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection - After toggle: IsSelected={transaction.IsSelected}, Count={_selectedTransactions.Count}");

        // Exit multi-select mode if no selections remain
        if (_selectedTransactions.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] ToggleTransactionSelection - No more selections, exiting multi-select mode");
            IsMultiSelectMode = false;
        }
    }

    /// <summary>
    /// Select all visible transactions
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        // ✅ Create a copy to avoid "Collection was modified" exception
        var selectedCopy = _selectedTransactions.ToList();

        // Clear previous selections
        foreach (var t in selectedCopy)
        {
            t.IsSelected = false;
        }
        _selectedTransactions.Clear();

        // Select all filtered transactions
        foreach (var transaction in FilteredTransactions)
        {
            transaction.IsSelected = true;
            _selectedTransactions.Add(transaction);
        }

        _loggingService?.LogDebug($"Selected all {_selectedTransactions.Count} transactions");
    }

    /// <summary>
    /// Clear selection and exit multi-select mode
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        // ✅ Create a copy to avoid "Collection was modified" exception
        // (setting IsSelected triggers checkbox binding which removes from collection)
        var selectedCopy = _selectedTransactions.ToList();

        // Clear IsSelected flag on all selected transactions
        foreach (var t in selectedCopy)
        {
            t.IsSelected = false;
        }

        _selectedTransactions.Clear();
        IsMultiSelectMode = false;

        _loggingService?.LogDebug("Multi-select mode disabled");
    }

    /// <summary>
    /// Delete all selected transactions
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (_selectedTransactions.Count == 0) return;

        // ✅ Ask for confirmation before deleting
        var count = _selectedTransactions.Count;
        bool confirm = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare {count} transazione/i selezionate?\n\nQuesta azione non può essere annullata.",
            "Elimina",
            "Annulla");

        if (!confirm) return;

        try
        {
            var transactionsToDelete = _selectedTransactions.ToList();

            // Delete each transaction
            foreach (var transaction in transactionsToDelete)
            {
                await _databaseService.DeleteTransactionAsync(transaction.Id);
            }

            _loggingService?.LogInfo($"Deleted {count} transactions");

            // Clear selection and exit multi-select mode
            ClearSelection();

            // Reload transactions
            await LoadTransactionsAsync();

            await Shell.Current.DisplayAlert(
                "Eliminazione completata",
                $"{count} transazione/i eliminate con successo",
                "OK");
        }
        catch (Exception ex)
        {
            _loggingService?.LogError("Error deleting selected transactions", ex);
            await Shell.Current.DisplayAlert(
                "Errore",
                "Si è verificato un errore durante l'eliminazione delle transazioni",
                "OK");
        }
    }

    #endregion

    /// <summary>
    /// Watch for search text changes - OPTIMIZED with debouncing (300ms)
    /// Prevents lag by waiting for user to finish typing before filtering
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        // Cancel and dispose previous timer
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Dispose();

        // Create new timer that fires after 300ms of inactivity
        _searchDebounceTimer = new System.Timers.Timer(600);
        _searchDebounceTimer.AutoReset = false;
        _searchDebounceTimer.Elapsed += async (s, e) =>
        {
            // Execute on main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ApplyFiltersAsync();
            });
        };
        _searchDebounceTimer.Start();

        _loggingService.LogDebug($"Search text changed: '{value}' (debouncing 600ms)");
    }

    /// <summary>
    /// Watch for amount text changes
    /// </summary>
    partial void OnMinAmountTextChanged(string value)
    {
        UpdateActiveFiltersCount();
    }

    partial void OnMaxAmountTextChanged(string value)
    {
        UpdateActiveFiltersCount();
    }

    /// <summary>
    /// Watch for transaction type changes
    /// </summary>
    partial void OnSelectedTransactionTypeChanged(int value)
    {
        _ = ApplyFiltersAsync();
    }

    /// <summary>
    /// Watch for date changes
    /// </summary>
    partial void OnStartDateChanged(DateTime? value)
    {
        if (value.HasValue && EndDate.HasValue && value.Value <= EndDate.Value)
        {
            UpdateActiveFiltersCount();
            _ = ApplyFiltersAsync(); // Use ApplyFiltersAsync instead of reloading all
        }
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        if (value.HasValue && StartDate.HasValue && value.Value >= StartDate.Value)
        {
            UpdateActiveFiltersCount();
            _ = ApplyFiltersAsync(); // Use ApplyFiltersAsync instead of reloading all
        }
    }

    /// <summary>
    /// Dispose resources - cleanup timers and cancellation tokens
    /// </summary>
    public void Dispose()
    {
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Dispose();
        _filterCancellationTokenSource?.Cancel();
        _filterCancellationTokenSource?.Dispose();
        _groupingCache?.Clear();
        
        _loggingService.LogDebug("TransactionsViewModel disposed");
    }
}
