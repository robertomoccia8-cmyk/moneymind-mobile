using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;

namespace MoneyMindApp.ViewModels;

[QueryProperty(nameof(TransactionId), nameof(TransactionId))]
public partial class EditTransactionViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private readonly IAnalyticsService _analyticsService;
    private Transaction? _originalTransaction;

    [ObservableProperty]
    private int transactionId;

    [ObservableProperty]
    private DateTime transactionDate = DateTime.Now;

    [ObservableProperty]
    private string amount = string.Empty;

    [ObservableProperty]
    private bool isIncome = false;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string reason = string.Empty;

    [ObservableProperty]
    private bool isSaving;

    public EditTransactionViewModel(
        DatabaseService databaseService,
        ILoggingService loggingService,
        IAnalyticsService analyticsService)
    {
        _databaseService = databaseService;
        _loggingService = loggingService;
        _analyticsService = analyticsService;
    }

    partial void OnTransactionIdChanged(int value)
    {
        if (value > 0)
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadTransactionAsync(value));
        }
    }

    private async Task LoadTransactionAsync(int id)
    {
        try
        {
            _originalTransaction = await _databaseService.GetTransactionByIdAsync(id);

            if (_originalTransaction != null)
            {
                TransactionDate = _originalTransaction.Data;
                Amount = Math.Abs(_originalTransaction.Importo).ToString("0.00", CultureInfo.InvariantCulture);
                IsIncome = _originalTransaction.Importo > 0;
                Description = _originalTransaction.Descrizione;
                Reason = _originalTransaction.Causale ?? string.Empty;

                _loggingService.LogInfo($"Loaded transaction for edit: ID {id}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error loading transaction {id}", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile caricare la transazione", "OK");
        }
    }

    [RelayCommand]
    private void SetIncome()
    {
        IsIncome = true;
    }

    [RelayCommand]
    private void SetExpense()
    {
        IsIncome = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (_originalTransaction == null)
            {
                await Shell.Current.DisplayAlert("Errore", "Transazione non trovata", "OK");
                return;
            }

            // Validation
            if (string.IsNullOrWhiteSpace(Amount))
            {
                await Shell.Current.DisplayAlert("Errore", "Inserisci un importo", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                await Shell.Current.DisplayAlert("Errore", "Inserisci una descrizione", "OK");
                return;
            }

            // Parse amount
            if (!decimal.TryParse(Amount.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount))
            {
                await Shell.Current.DisplayAlert("Errore", "Importo non valido", "OK");
                return;
            }

            if (parsedAmount <= 0)
            {
                await Shell.Current.DisplayAlert("Errore", "L'importo deve essere maggiore di zero", "OK");
                return;
            }

            // Update transaction
            _originalTransaction.Data = TransactionDate;
            _originalTransaction.Importo = IsIncome ? parsedAmount : -parsedAmount;
            _originalTransaction.Descrizione = Description.Trim();
            _originalTransaction.Causale = string.IsNullOrWhiteSpace(Reason) ? string.Empty : Reason.Trim();

            await _databaseService.UpdateTransactionAsync(_originalTransaction);

            // Clear analytics cache
            if (_analyticsService is AnalyticsService analyticsService)
            {
                analyticsService.ClearCache();
            }

            _loggingService.LogInfo($"Transaction updated: {_originalTransaction.Descrizione} - {_originalTransaction.Importo:C2}");

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error updating transaction", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile aggiornare la transazione", "OK");
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
            if (_originalTransaction == null)
                return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma Eliminazione",
                $"Vuoi eliminare '{_originalTransaction.Descrizione}'?",
                "Elimina",
                "Annulla");

            if (!confirm)
                return;

            IsSaving = true;

            await _databaseService.DeleteTransactionAsync(_originalTransaction.Id);

            // Clear analytics cache
            if (_analyticsService is AnalyticsService analyticsService)
            {
                analyticsService.ClearCache();
            }

            _loggingService.LogInfo($"Transaction deleted: ID {_originalTransaction.Id}");

            await Shell.Current.DisplayAlert("Successo", "Transazione eliminata", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting transaction", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile eliminare la transazione", "OK");
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
