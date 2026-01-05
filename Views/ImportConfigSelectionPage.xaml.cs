using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class ImportConfigSelectionPage : ContentPage
{
    private readonly ImportConfigSelectionViewModel _viewModel;
    private bool _isInitialized = false;

    public ImportConfigSelectionPage(ImportConfigSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // ⚡ Fire-and-forget background loading to keep animation smooth
        if (!_isInitialized)
        {
            _isInitialized = true;

            // Load in background - page appears immediately, configs load async
            _ = Task.Run(async () =>
            {
                await Task.Delay(400); // Wait for animation to fully complete
                await _viewModel.InitializeAsync();
            });
        }
    }
}
