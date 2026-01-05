using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class AddTransactionPage : ContentPage
{
    public AddTransactionPage(AddTransactionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
