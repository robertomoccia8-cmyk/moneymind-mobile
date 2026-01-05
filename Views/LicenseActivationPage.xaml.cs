using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class LicenseActivationPage : ContentPage
{
    public LicenseActivationPage(LicenseActivationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
