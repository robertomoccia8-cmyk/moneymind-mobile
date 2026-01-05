using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class EditAccountPage : ContentPage
{
    public EditAccountPage(EditAccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
