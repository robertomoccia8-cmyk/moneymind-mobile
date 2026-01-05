using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class AddAccountPage : ContentPage
{
    public AddAccountPage(AddAccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
