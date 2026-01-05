using MoneyMindApp.ViewModels;

namespace MoneyMindApp.Views;

public partial class ImportHeaderSelectionPage : ContentPage
{
    public ImportHeaderSelectionPage(ImportHeaderSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
