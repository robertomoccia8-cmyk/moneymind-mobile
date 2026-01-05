using SQLite;

namespace MoneyMindApp.Models;

/// <summary>
/// Bank account model (ContoCorrente)
/// Ported from VB.NET desktop version
/// </summary>
[Table("ContiCorrenti")]
public class BankAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Icona { get; set; } // Emoji or path

    public string? Colore { get; set; } // Hex color (#RRGGBB)

    public decimal SaldoIniziale { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Computed property: current balance (calculated from transactions)
    /// This is set dynamically by the AccountService
    /// </summary>
    [Ignore]
    public decimal SaldoCorrente { get; set; }

    /// <summary>
    /// Computed property: database filename for this account
    /// </summary>
    [Ignore]
    public string DatabaseFileName => $"MoneyMind_Conto_{Id:D3}.db";
}
