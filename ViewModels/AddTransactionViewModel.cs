using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Globalization;

namespace MoneyMindApp.ViewModels;

public partial class AddTransactionViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;
    private readonly IAnalyticsService _analyticsService;

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

    public AddTransactionViewModel(
        DatabaseService databaseService,
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService,
        IAnalyticsService analyticsService)
    {
        _databaseService = databaseService;
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
        _analyticsService = analyticsService;
    }

    [RelayCommand]
    private void SetIncome()
    {
        IsIncome = true;
        _loggingService.LogDebug("Transaction type set to Income");
    }

    [RelayCommand]
    private void SetExpense()
    {
        IsIncome = false;
        _loggingService.LogDebug("Transaction type set to Expense");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

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

            // Create transaction
            var transaction = new Transaction
            {
                Data = TransactionDate,
                Importo = IsIncome ? parsedAmount : -parsedAmount,
                Descrizione = Description.Trim(),
                Causale = string.IsNullOrWhiteSpace(Reason) ? string.Empty : Reason.Trim(),
                CreatedAt = DateTime.Now
            };

            // Save to database
            await _databaseService.InsertTransactionAsync(transaction);

            // Clear analytics cache
            if (_analyticsService is AnalyticsService analyticsService)
            {
                analyticsService.ClearCache();
            }

            _loggingService.LogInfo($"Transaction saved: {transaction.Descrizione} - {transaction.Importo:C2}");

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error saving transaction", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile salvare la transazione", "OK");
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
