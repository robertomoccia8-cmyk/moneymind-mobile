using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class ImportValidationPage : ContentPage
{
    private readonly ImportValidationViewModel _viewModel;

    public ImportValidationPage(ImportValidationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
