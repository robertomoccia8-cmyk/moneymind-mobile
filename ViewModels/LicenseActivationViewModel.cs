using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Services.License;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.Database;

namespace MoneyMindApp.ViewModels;

public partial class LicenseActivationViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly ILoggingService _loggingService;
    private readonly GlobalDatabaseService _globalDatabaseService;

    [ObservableProperty]
    private string licenseKey = string.Empty;

    [ObservableProperty]
    private bool isActivating = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError = false;

    public LicenseActivationViewModel(
        ILicenseService licenseService, 
        ILoggingService loggingService,
        GlobalDatabaseService globalDatabaseService)
    {
        _licenseService = licenseService;
        _loggingService = loggingService;
        _globalDatabaseService = globalDatabaseService;
    }

    [RelayCommand]
    private async Task ActivateLicenseAsync()
    {
        // Validazione
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            ShowError("Inserisci una Beta Key valida");
            return;
        }

        IsActivating = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            _loggingService.LogInfo($"Attempting license activation with key: {LicenseKey.Substring(0, Math.Min(8, LicenseKey.Length))}...");

            var (success, message, license) = await _licenseService.ActivateLicenseAsync(LicenseKey.Trim());

            if (success && license != null)
            {
                _loggingService.LogInfo($"License activated successfully: {license.Email}, Subscription: {license.Subscription}");

                // Mark onboarding as completed
                Preferences.Set("onboarding_completed", true);

                // ✅ CHECK IF ACCOUNTS EXIST - Same logic as App.xaml.cs
                try
                {
                    await _globalDatabaseService.InitializeAsync();
                    var existingAccounts = await _globalDatabaseService.GetAllAccountsAsync();

                    if (existingAccounts.Count > 0)
                    {
                        _loggingService.LogInfo($"Found {existingAccounts.Count} existing accounts after license activation - skipping account creation");

                        // Set current account if not set
                        var currentAccountId = Preferences.Get("current_account_id", 0);
                        if (currentAccountId == 0)
                        {
                            var firstAccount = existingAccounts.OrderByDescending(a => a.LastAccessedAt).FirstOrDefault();
                            if (firstAccount != null)
                            {
                                Preferences.Set("current_account_id", firstAccount.Id);
                                _loggingService.LogInfo($"Set current account to: {firstAccount.Nome} (ID: {firstAccount.Id})");
                            }
                        }

                        // Skip account creation, go to main
                        await Shell.Current.GoToAsync("//main");
                    }
                    else
                    {
                        _loggingService.LogInfo("No existing accounts found - navigating to account creation");
                        // Go to account creation
                        await Shell.Current.GoToAsync("onboarding/account");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Error checking existing accounts after license activation", ex);
                    // Fallback: go to account creation
                    await Shell.Current.GoToAsync("onboarding/account");
                }
            }
            else
            {
                _loggingService.LogWarning($"License activation failed: {message}");
                ShowError(CustomizeErrorMessage(message));
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error during license activation: {ex.Message}");
            ShowError($"Si è verificato un errore:\n{ex.Message}\n\nVerifica la connessione internet e riprova.");
        }
        finally
        {
            IsActivating = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private string CustomizeErrorMessage(string originalMessage)
    {
        if (originalMessage.Contains("already activated"))
            return "Questa Beta Key è già stata attivata su un altro dispositivo.\n\nOgni Beta Key può essere usata su un solo dispositivo.\n\nSe hai cambiato dispositivo, contatta roberto.moccia8@gmail.com";

        if (originalMessage.Contains("Invalid beta key"))
            return "Beta Key non valida.\n\nVerifica di aver inserito correttamente la chiave ricevuta via email.";

        if (originalMessage.Contains("revoked") || originalMessage.Contains("expired"))
            return "Questa Beta Key è stata revocata o è scaduta.\n\nContatta roberto.moccia8@gmail.com per assistenza.";

        return originalMessage;
    }
}
