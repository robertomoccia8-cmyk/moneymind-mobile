using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MoneyMindApp.ViewModels;

public partial class QuickTourViewModel : ObservableObject
{
    [RelayCommand]
    private async Task FinishOnboardingAsync()
    {
        Preferences.Set("onboarding_completed", true);
        await Shell.Current.GoToAsync("//main");
    }
}
