using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class BiometricSetupPage : ContentPage
{
    public BiometricSetupPage(BiometricSetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
