using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private bool _isInitialized = false;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // ✅ Initialize only on first appearance
        if (!_isInitialized)
        {
            await _viewModel.InitializeAsync();
            _isInitialized = true;
        }
    }
}
