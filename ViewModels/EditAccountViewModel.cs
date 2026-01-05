using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;

namespace MoneyMindApp.ViewModels;

[QueryProperty(nameof(AccountId), nameof(AccountId))]
public partial class EditAccountViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;
    private BankAccount? _originalAccount;

    [ObservableProperty]
    private int accountId;

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

    public EditAccountViewModel(
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService)
    {
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
    }

    partial void OnAccountIdChanged(int value)
    {
        if (value > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadAccountAsync(value));
        }
    }

    private async Task LoadAccountAsync(int id)
    {
        try
        {
            _originalAccount = await _globalDatabaseService.GetAccountByIdAsync(id);

            if (_originalAccount != null)
            {
                AccountName = _originalAccount.Nome;
                SelectedIcon = _originalAccount.Icona;
                SelectedColor = _originalAccount.Colore;
                InitialBalance = _originalAccount.SaldoIniziale.ToString("0.00", CultureInfo.InvariantCulture);

                _loggingService.LogInfo($"Loaded account for edit: ID {id}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error loading account {id}", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile caricare il conto", "OK");
        }
    }

    [RelayCommand]
    private void SelectIcon(string icon)
    {
        SelectedIcon = icon;
    }

    [RelayCommand]
    private void SelectColor(string color)
    {
        SelectedColor = color;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (_originalAccount == null)
            {
                await Shell.Current.DisplayAlert("Errore", "Conto non trovato", "OK");
                return;
            }

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

            // Update account
            _originalAccount.Nome = AccountName.Trim();
            _originalAccount.Icona = SelectedIcon;
            _originalAccount.Colore = SelectedColor;
            _originalAccount.SaldoIniziale = parsedBalance;

            await _globalDatabaseService.UpdateAccountAsync(_originalAccount);

            _loggingService.LogInfo($"Account updated: {_originalAccount.Nome}");

            await Shell.Current.DisplayAlert("Successo", "Conto aggiornato!", "OK");

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error updating account", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile aggiornare il conto", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        try
        {
            if (_originalAccount == null)
                return;

            // Check if it's the last account
            var accounts = await _globalDatabaseService.GetAllAccountsAsync();
            if (accounts.Count <= 1)
            {
                await Shell.Current.DisplayAlert("Errore", "Non puoi eliminare l'ultimo conto", "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma Eliminazione",
                $"Vuoi eliminare il conto '{_originalAccount.Nome}'?\n\nATTENZIONE: Tutte le transazioni associate verranno perse!",
                "Elimina",
                "Annulla");

            if (!confirm)
                return;

            IsSaving = true;

            await _globalDatabaseService.DeleteAccountAsync(_originalAccount.Id);

            _loggingService.LogInfo($"Account deleted: ID {_originalAccount.Id}");

            await Shell.Current.DisplayAlert("Successo", "Conto eliminato", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting account", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile eliminare il conto", "OK");
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
            "Vuoi annullare? Le modifiche non salvate verranno perse.",
            "Annulla",
            "Torna Indietro");

        if (confirm)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
