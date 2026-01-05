using CommunityToolkit.Mvvm.ComponentModel;

namespace MoneyMindApp.Models;

/// <summary>
/// Model per la gestione dei filtri avanzati delle transazioni
/// </summary>
public partial class TransactionFilters : ObservableObject
{
    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private DateTime? startDate;

    [ObservableProperty]
    private DateTime? endDate;

    [ObservableProperty]
    private decimal? minAmount;

    [ObservableProperty]
    private decimal? maxAmount;

    [ObservableProperty]
    private TransactionType transactionType = TransactionType.All;

    /// <summary>
    /// Conta quanti filtri sono attivi (diversi dai valori di default)
    /// </summary>
    public int ActiveFiltersCount
    {
        get
        {
            int count = 0;

            if (!string.IsNullOrWhiteSpace(SearchText))
                count++;

            if (StartDate.HasValue)
                count++;

            if (EndDate.HasValue)
                count++;

            if (MinAmount.HasValue)
                count++;

            if (MaxAmount.HasValue)
                count++;

            if (TransactionType != TransactionType.All)
                count++;

            return count;
        }
    }

    /// <summary>
    /// Resetta tutti i filtri ai valori di default
    /// </summary>
    public void Reset()
    {
        SearchText = null;
        StartDate = null;
        EndDate = null;
        MinAmount = null;
        MaxAmount = null;
        TransactionType = TransactionType.All;

        // Notifica che tutti i filtri sono cambiati
        OnPropertyChanged(nameof(ActiveFiltersCount));
    }

    /// <summary>
    /// Clona i filtri correnti
    /// </summary>
    public TransactionFilters Clone()
    {
        return new TransactionFilters
        {
            SearchText = SearchText,
            StartDate = StartDate,
            EndDate = EndDate,
            MinAmount = MinAmount,
            MaxAmount = MaxAmount,
            TransactionType = TransactionType
        };
    }

    /// <summary>
    /// Verifica se ci sono filtri attivi
    /// </summary>
    public bool HasActiveFilters => ActiveFiltersCount > 0;
}

/// <summary>
/// Enum per il tipo di transazione da filtrare
/// </summary>
public enum TransactionType
{
    All,        // Tutte le transazioni
    Income,     // Solo entrate (importo > 0)
    Expense     // Solo uscite (importo < 0)
}
