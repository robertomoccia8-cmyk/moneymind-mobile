using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class SalaryConfigPage : ContentPage
{
    private bool _isInitialized = false;

    public SalaryConfigPage(SalaryConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Load config only on first appearance
        if (!_isInitialized)
        {
            await ((SalaryConfigViewModel)BindingContext).InitializeAsync();
            _isInitialized = true;
        }
    }
}
