using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class ExportPage : ContentPage
{
    private readonly ExportViewModel _viewModel;
    private bool _isInitialized = false;

    public ExportPage(ExportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Load preview only on first appearance to avoid lag
        if (!_isInitialized)
        {
            await _viewModel.InitializeAsync();
            _isInitialized = true;
        }
    }
}
