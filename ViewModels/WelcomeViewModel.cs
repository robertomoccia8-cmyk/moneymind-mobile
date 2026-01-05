using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MoneyMindApp.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    [RelayCommand]
    private async Task NavigateToLicense()
    {
        await Shell.Current.GoToAsync("onboarding/license");
    }

    [RelayCommand]
    private async Task SkipOnboarding()
    {
        // Mark onboarding as completed
        Preferences.Set("onboarding_completed", true);
        // Navigate to main tab (absolute route works for TabBar items)
        await Shell.Current.GoToAsync("//main");
    }
}
