using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;

namespace MoneyMindApp.ViewModels;

public partial class AddAccountViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private string accountName = string.Empty;

    [ObservableProperty]
    private string selectedIcon = "💳";

    [ObservableProperty]
    private string selectedColor = "#512BD4";

    [ObservableProperty]
    private string initialBalance = "0";

    [ObservableProperty]
    private bool isSaving;

    public AddAccountViewModel(
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService)
    {
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
    }

    [RelayCommand]
    private void SelectIcon(string icon)
    {
        SelectedIcon = icon;
        _loggingService.LogDebug($"Icon selected: {icon}");
    }

    [RelayCommand]
    private void SelectColor(string color)
    {
        SelectedColor = color;
        _loggingService.LogDebug($"Color selected: {color}");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            // Validation
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                await Shell.Current.DisplayAlert("Errore", "Inserisci un nome per il conto", "OK");
                return;
            }

            // Parse initial balance
            if (!decimal.TryParse(InitialBalance.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedBalance))
            {
                parsedBalance = 0;
            }

            // Create account
            var account = new BankAccount
            {
                Nome = AccountName.Trim(),
                Icona = SelectedIcon,
                Colore = SelectedColor,
                SaldoIniziale = parsedBalance,
                CreatedAt = DateTime.Now
            };

            await _globalDatabaseService.InsertAccountAsync(account);

            _loggingService.LogInfo($"Account created: {account.Nome} with balance {account.SaldoIniziale:C2}");

            await Shell.Current.DisplayAlert("Successo", $"Conto '{account.Nome}' creato!", "OK");

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating account", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile creare il conto", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Conferma",
            "Vuoi annullare? I dati non salvati verranno persi.",
            "Annulla",
            "Torna Indietro");

        if (confirm)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
