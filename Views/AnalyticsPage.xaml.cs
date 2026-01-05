using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class AnalyticsPage : ContentPage
{
    private bool _isInitialized = false;

    public AnalyticsPage(AnalyticsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Initialize only on first appearance - charts are expensive to generate
        if (!_isInitialized)
        {
            await ((AnalyticsViewModel)BindingContext).InitializeAsync();
            _isInitialized = true;
        }
        // Charts and data persist - no need to regenerate on every return
    }

    private void OnYearTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is int selectedYear)
        {
            var viewModel = (AnalyticsViewModel)BindingContext;
            if (viewModel.AvailableYears.Contains(selectedYear))
            {
                viewModel.SelectedYear = selectedYear;
            }
        }
    }
}
