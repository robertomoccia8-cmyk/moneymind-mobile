using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;

namespace MoneyMindApp.ViewModels;

public partial class CreateAccountViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private string accountName = string.Empty;

    [ObservableProperty]
    private decimal initialBalance = 0m;

    [ObservableProperty]
    private string selectedIcon = "💳";

    [ObservableProperty]
    private string selectedColor = "#512BD4";

    [ObservableProperty]
    private bool isCreating = false;

    public List<string> AvailableIcons { get; } = new()
    {
        "💳", "🏦", "💰", "💵", "💶", "💷", "💴", "🪙", "💸", "🏧"
    };

    public List<string> AvailableColors { get; } = new()
    {
        "#512BD4", "#2196F3", "#4CAF50", "#FF9800", "#F44336", 
        "#9C27B0", "#00BCD4", "#CDDC39", "#FF5722", "#607D8B"
    };

    public CreateAccountViewModel(
        GlobalDatabaseService globalDatabaseService,
        DatabaseService databaseService,
        ILoggingService loggingService)
    {
        _globalDatabaseService = globalDatabaseService;
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        try
        {
            // ✅ VALIDATION
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                await Shell.Current.DisplayAlert(
                    "Nome Richiesto",
                    "Inserisci un nome per il conto.",
                    "OK");
                return;
            }

            if (AccountName.Length < 2)
            {
                await Shell.Current.DisplayAlert(
                    "Nome Troppo Corto",
                    "Il nome del conto deve essere di almeno 2 caratteri.",
                    "OK");
                return;
            }

            if (AccountName.Length > 50)
            {
                await Shell.Current.DisplayAlert(
                    "Nome Troppo Lungo",
                    "Il nome del conto non può superare 50 caratteri.",
                    "OK");
                return;
            }

            // Validate balance (optional but good practice)
            if (InitialBalance < -999999999 || InitialBalance > 999999999)
            {
                await Shell.Current.DisplayAlert(
                    "Saldo Non Valido",
                    "Il saldo iniziale deve essere tra -999,999,999 e 999,999,999.",
                    "OK");
                return;
            }

            IsCreating = true;

            // Initialize global database
            await _globalDatabaseService.InitializeAsync();

            // Create account
            var newAccount = new BankAccount
            {
                Nome = AccountName.Trim(),
                Icona = SelectedIcon,
                Colore = SelectedColor,
                SaldoIniziale = InitialBalance,
                CreatedAt = DateTime.Now
            };

            await _globalDatabaseService.InsertAccountAsync(newAccount);

            // Get the created account to get its ID
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            var createdAccount = accounts.FirstOrDefault(a => a.Nome == newAccount.Nome);

            if (createdAccount != null)
            {
                // Set as current account
                Preferences.Set("current_account_id", createdAccount.Id);

                // Initialize account database
                await _databaseService.InitializeAsync(createdAccount.Id);

                _loggingService.LogInfo($"Account created successfully: {createdAccount.Nome} (ID: {createdAccount.Id})");

                // Navigate to next onboarding step
                await Shell.Current.GoToAsync("onboarding/biometric");
            }
            else
            {
                throw new Exception("Account created but not found in database");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating account", ex);
            await Shell.Current.DisplayAlert(
                "Errore",
                $"Impossibile creare il conto: {ex.Message}",
                "OK");
        }
        finally
        {
            IsCreating = false;
        }
    }
}
