namespace MoneyMindApp.Models;

/// <summary>
/// Statistics for a bank account in a given period
/// </summary>
public class AccountStatistics
{
    /// <summary>
    /// Total balance (SaldoIniziale + SUM(Importi))
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// Total income (positive transactions)
    /// </summary>
    public decimal Income { get; set; }

    /// <summary>
    /// Total expenses (absolute value of negative transactions)
    /// </summary>
    public decimal Expenses { get; set; }

    /// <summary>
    /// Savings (Income - Expenses)
    /// </summary>
    public decimal Savings { get; set; }

    /// <summary>
    /// Number of transactions in period
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Formatted total balance with currency
    /// </summary>
    public string FormattedTotalBalance => TotalBalance.ToString("C2");

    /// <summary>
    /// Formatted income with currency
    /// </summary>
    public string FormattedIncome => Income.ToString("C2");

    /// <summary>
    /// Formatted expenses with currency
    /// </summary>
    public string FormattedExpenses => Expenses.ToString("C2");

    /// <summary>
    /// Formatted savings with currency
    /// </summary>
    public string FormattedSavings => Savings.ToString("C2");

    /// <summary>
    /// Savings percentage of income (0-100)
    /// </summary>
    public double SavingsPercentage => Income > 0 ? (double)(Savings / Income * 100) : 0;

    /// <summary>
    /// Is savings positive?
    /// </summary>
    public bool HasPositiveSavings => Savings > 0;
}
