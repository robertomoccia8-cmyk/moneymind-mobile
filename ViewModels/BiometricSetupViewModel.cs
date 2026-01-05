using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Services.Security;

namespace MoneyMindApp.ViewModels;

public partial class BiometricSetupViewModel : ObservableObject
{
    private readonly IBiometricAuthService _biometricService;

    public BiometricSetupViewModel(IBiometricAuthService biometricService)
    {
        _biometricService = biometricService;
    }

    [RelayCommand]
    private async Task EnableBiometricAsync()
    {
        var available = await _biometricService.IsAvailableAsync();
        if (available)
        {
            Preferences.Set("biometric_enabled", true);
        }
        await Shell.Current.GoToAsync("onboarding/tour");
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        await Shell.Current.GoToAsync("onboarding/tour");
    }
}
