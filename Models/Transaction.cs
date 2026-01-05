using SQLite;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MoneyMindApp.Models;

/// <summary>
/// Transaction model (Transazione)
/// Ported from VB.NET desktop version
/// </summary>
[Table("Transazioni")]
public class Transaction : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime Data { get; set; }

    public decimal Importo { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public string? Causale { get; set; }

    public string? Note { get; set; }

    [Indexed]
    public int AccountId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Computed property: is this an income transaction?
    /// </summary>
    [Ignore]
    public bool IsIncome => Importo > 0;

    /// <summary>
    /// Computed property: is this an expense transaction?
    /// </summary>
    [Ignore]
    public bool IsExpense => Importo < 0;

    /// <summary>
    /// Computed property: absolute amount for display
    /// </summary>
    [Ignore]
    public decimal AbsoluteAmount => Math.Abs(Importo);

    /// <summary>
    /// Formatted amount with currency symbol
    /// </summary>
    [Ignore]
    public string FormattedAmount => Importo.ToString("C2");

    /// <summary>
    /// Formatted date in Italian (dd MMM yyyy)
    /// </summary>
    [Ignore]
    public string FormattedDate => Data.ToString("dd MMM yyyy", new CultureInfo("it-IT"));

    /// <summary>
    /// Formatted date short (dd/MM/yyyy)
    /// </summary>
    [Ignore]
    public string FormattedDateShort => Data.ToString("dd/MM/yyyy");

    /// <summary>
    /// Day of week in Italian
    /// </summary>
    [Ignore]
    public string DayOfWeek => Data.ToString("dddd", new CultureInfo("it-IT"));

    /// <summary>
    /// Multi-select state (not persisted to database)
    /// </summary>
    private bool _isSelected;

    [Ignore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            System.Diagnostics.Debug.WriteLine($"[MODEL] Transaction.IsSelected setter - OLD={_isSelected}, NEW={value}, Descrizione={Descrizione}");

            if (_isSelected != value)
            {
                _isSelected = value;
                System.Diagnostics.Debug.WriteLine($"[MODEL] Transaction.IsSelected - Value changed, calling OnPropertyChanged()");
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"[MODEL] Transaction.IsSelected - OnPropertyChanged() completed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MODEL] Transaction.IsSelected - Value unchanged, skipping OnPropertyChanged()");
            }
        }
    }
}
