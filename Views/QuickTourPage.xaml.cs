using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class QuickTourPage : ContentPage
{
    public QuickTourPage(QuickTourViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
