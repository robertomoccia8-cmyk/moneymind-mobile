using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class WiFiSyncPage : ContentPage
{
    private readonly WiFiSyncViewModel _viewModel;
    private bool _isInitialized = false;

    public WiFiSyncPage(WiFiSyncViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Load sync data only on first appearance
        if (!_isInitialized)
        {
            await _viewModel.InitializeAsync();
            _isInitialized = true;
        }
    }
}
