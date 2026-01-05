using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class CreateAccountPage : ContentPage
{
    public CreateAccountPage(CreateAccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
