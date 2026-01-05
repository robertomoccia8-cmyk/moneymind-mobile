using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class AccountSelectionPage : ContentPage
{
    private readonly AccountSelectionViewModel _viewModel;
    private bool _isInitialized = false;

    public AccountSelectionPage(AccountSelectionViewModel viewModel)
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
