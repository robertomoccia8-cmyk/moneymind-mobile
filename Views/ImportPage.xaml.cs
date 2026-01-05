using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class ImportPage : ContentPage
{
    private readonly ImportViewModel _viewModel;

    public ImportPage(ImportViewModel viewModel)
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
