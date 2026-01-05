using MoneyMindApp.ViewModels;
using MoneyMindApp.Models;
using System.Timers;

namespace MoneyMindApp.Views;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;
    private bool _isInitialized = false;
    private int _lastLoadedAccountId = 0;

    // Long press detection
    private System.Timers.Timer? _longPressTimer;
    private Transaction? _pressedTransaction;
    private const int LongPressDuration = 500; // 500ms for long press

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Get current account ID from Preferences
            var currentAccountId = Preferences.Get("current_account_id", 0);

            // If no account is set, wait for MainPage to initialize it (first launch race condition)
            if (currentAccountId == 0)
            {
                System.Diagnostics.Debug.WriteLine("[TransactionsPage] No account ID found, waiting for MainPage initialization...");

                // Retry up to 10 times (5 seconds total) waiting for MainPage to create account
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(500);
                    currentAccountId = Preferences.Get("current_account_id", 0);
                    if (currentAccountId > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TransactionsPage] Account ID found after {i + 1} retries: {currentAccountId}");
                        break;
                    }
                }

                // If still no account after retries, exit
                if (currentAccountId == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TransactionsPage] No account ID found after retries, exiting");
                    return;
                }
            }

            // Check if account changed
            if (currentAccountId != _lastLoadedAccountId)
            {
                _isInitialized = false; // Force reload if account changed
                _lastLoadedAccountId = currentAccountId;
            }

            // ✅ Initialize only on first appearance or account change
            if (!_isInitialized)
            {
                await _viewModel.InitializeAsync(currentAccountId);
                _isInitialized = true;
            }
            else
            {
                // ✅ Always refresh data when returning to page (e.g., after import)
                // Use the command which is public, not the private async method
                if (_viewModel.LoadTransactionsCommand.CanExecute(null))
                {
                    await _viewModel.LoadTransactionsCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TransactionsPage] OnAppearing error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[TransactionsPage] Stack trace: {ex.StackTrace}");
            // Swallow exception to prevent tab switching
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // ⚡ Close any open modals when leaving tab to prevent lag on return
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Pop all modals in the navigation stack
                while (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync(false); // false = no animation
                }
            }
            catch { }
        });
    }

    // Long press gesture handlers
    private void OnTransactionLongPressed(object sender, PointerEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not Transaction transaction)
            return;

        _pressedTransaction = transaction;

        // Start long press timer
        _longPressTimer = new System.Timers.Timer(LongPressDuration);
        _longPressTimer.Elapsed += OnLongPressTimerElapsed;
        _longPressTimer.AutoReset = false;
        _longPressTimer.Start();
    }

    private void OnTransactionPointerReleased(object sender, PointerEventArgs e)
    {
        // If released before timer, it's a tap (cancel long press)
        CancelLongPress();
    }

    private void OnTransactionPointerExited(object sender, PointerEventArgs e)
    {
        // If pointer exits, cancel long press
        CancelLongPress();
    }

    private void OnLongPressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_pressedTransaction != null)
            {
                // Trigger haptic feedback
                try
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                }
                catch { }

                // Enable multi-select mode with this transaction
                _viewModel.EnableMultiSelectCommand.Execute(_pressedTransaction);

                _pressedTransaction = null;
            }

            _longPressTimer?.Dispose();
            _longPressTimer = null;
        });
    }

    private void CancelLongPress()
    {
        if (_longPressTimer != null)
        {
            _longPressTimer.Stop();
            _longPressTimer.Dispose();
            _longPressTimer = null;
        }
        _pressedTransaction = null;
    }

    // Tap gesture handler
    private void OnTransactionTapped(object sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("========================================");
        System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] OnTransactionTapped CALLED!");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Sender type: {sender?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] EventArgs type: {e?.GetType().Name ?? "NULL"}");

        // Cancel long press if tap was released quickly
        if (_longPressTimer != null)
        {
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] Cancelling long press timer");
            _longPressTimer.Stop();
            _longPressTimer.Dispose();
            _longPressTimer = null;
            _pressedTransaction = null;
        }

        if (sender is not Frame frame)
        {
            System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] ERROR: Sender is not Frame, it's {sender?.GetType().Name ?? "NULL"}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Frame found, BindingContext type: {frame.BindingContext?.GetType().Name ?? "NULL"}");

        if (frame.BindingContext is not Transaction transaction)
        {
            System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] ERROR: BindingContext is not Transaction");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Transaction found: {transaction.Descrizione}");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] MultiSelect mode: {_viewModel.IsMultiSelectMode}");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Transaction.IsSelected: {transaction.IsSelected}");

        if (_viewModel.IsMultiSelectMode)
        {
            // In multi-select mode: toggle selection
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] IN MULTI-SELECT MODE - Calling ToggleTransactionSelectionCommand");
            _viewModel.ToggleTransactionSelectionCommand.Execute(transaction);
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] ToggleTransactionSelectionCommand.Execute completed");
        }
        else
        {
            // Normal mode: edit transaction
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] IN NORMAL MODE - Calling EditTransactionCommand");
            _viewModel.EditTransactionCommand.Execute(transaction);
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] EditTransactionCommand.Execute completed");
        }

        System.Diagnostics.Debug.WriteLine("========================================");
    }

    /// <summary>
    /// Handle CheckBox checked/unchecked event
    /// Updates the SelectedTransactions collection when user clicks the checkbox
    /// </summary>
    private void OnCheckBoxChanged(object sender, CheckedChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("========================================");
        System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] OnCheckBoxChanged CALLED!");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] New value: {e.Value}");

        if (sender is not CheckBox checkBox)
        {
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] ERROR: Sender is not CheckBox");
            return;
        }

        if (checkBox.BindingContext is not Transaction transaction)
        {
            System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] ERROR: BindingContext is not Transaction");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Transaction: {transaction.Descrizione}");
        System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] IsSelected before: {transaction.IsSelected}");

        // The binding (TwoWay by default) has already updated transaction.IsSelected
        // Now we need to sync the SelectedTransactions collection
        if (e.Value) // Checked
        {
            // Add to selection if not already present
            if (!_viewModel.SelectedTransactions.Contains(transaction))
            {
                _viewModel.SelectedTransactions.Add(transaction);
                System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Added to selection. Total: {_viewModel.SelectedTransactions.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] Already in selection");
            }
        }
        else // Unchecked
        {
            // Remove from selection
            if (_viewModel.SelectedTransactions.Contains(transaction))
            {
                _viewModel.SelectedTransactions.Remove(transaction);
                System.Diagnostics.Debug.WriteLine($"[CODE-BEHIND] Removed from selection. Total: {_viewModel.SelectedTransactions.Count}");
            }

            // Exit multi-select mode if no selections remain
            if (_viewModel.SelectedTransactions.Count == 0)
            {
                _viewModel.IsMultiSelectMode = false;
                System.Diagnostics.Debug.WriteLine("[CODE-BEHIND] No more selections - exiting multi-select mode");
            }
        }

        System.Diagnostics.Debug.WriteLine("========================================");
    }

}
