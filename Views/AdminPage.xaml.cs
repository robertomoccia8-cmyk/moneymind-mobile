using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class AdminPage : ContentPage
{
    private readonly AdminViewModel _viewModel;

    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if user is admin before allowing access
        bool isAdmin = Preferences.Get("is_admin", false);

        if (!isAdmin)
        {
            await DisplayAlert(
                "Accesso Negato",
                "Solo gli amministratori possono accedere a questa pagina.",
                "OK");

            await Shell.Current.GoToAsync("//main");
            return;
        }

        await _viewModel.InitializeAsync();
    }
}
