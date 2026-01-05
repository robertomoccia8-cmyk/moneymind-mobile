using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isInitialized = false;
    private int _lastLoadedAccountId = 0;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if account changed
        var currentAccountId = Preferences.Get("current_account_id", 0);
        if (currentAccountId != _lastLoadedAccountId)
        {
            _isInitialized = false; // Force reload if account changed
            _lastLoadedAccountId = currentAccountId;
        }

        // ✅ Initialize only on first appearance or account change
        if (!_isInitialized)
        {
            await _viewModel.InitializeAsync();
            _isInitialized = true;
        }
        // No need to refresh dashboard on every return - data persists
    }
}
