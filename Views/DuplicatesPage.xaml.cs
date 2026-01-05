using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class DuplicatesPage : ContentPage
{
    public DuplicatesPage(DuplicatesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
