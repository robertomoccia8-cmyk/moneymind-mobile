using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class EditTransactionPage : ContentPage
{
    public EditTransactionPage(EditTransactionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
