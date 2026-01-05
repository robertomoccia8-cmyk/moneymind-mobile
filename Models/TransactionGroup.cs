using System.Collections.ObjectModel;

namespace MoneyMindApp.Models;

/// <summary>
/// Groups transactions by month for display with headers
/// </summary>
public class TransactionGroup : ObservableCollection<Transaction>
{
    /// <summary>
    /// Display name for the group header (e.g., "Novembre 2025")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Short name for compact display (e.g., "Nov 2025")
    /// </summary>
    public string ShortName { get; private set; }

    /// <summary>
    /// Year of the group
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Month of the group (1-12) for solar months, or start month for salary periods
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Period start date (useful for salary periods)
    /// </summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>
    /// Period end date (useful for salary periods)
    /// </summary>
    public DateTime PeriodEnd { get; private set; }

    /// <summary>
    /// Total income for this period
    /// </summary>
    public decimal TotalIncome => this.Where(t => t.Importo > 0).Sum(t => t.Importo);

    /// <summary>
    /// Total expenses for this period (absolute value)
    /// </summary>
    public decimal TotalExpenses => Math.Abs(this.Where(t => t.Importo < 0).Sum(t => t.Importo));

    /// <summary>
    /// Net balance for this period
    /// </summary>
    public decimal NetBalance => TotalIncome - TotalExpenses;

    /// <summary>
    /// Formatted income string
    /// </summary>
    public string FormattedIncome => $"+{TotalIncome:N2} €";

    /// <summary>
    /// Formatted expenses string
    /// </summary>
    public string FormattedExpenses => $"-{TotalExpenses:N2} €";

    /// <summary>
    /// Formatted net balance string
    /// </summary>
    public string FormattedNetBalance => NetBalance >= 0 ? $"+{NetBalance:N2} €" : $"{NetBalance:N2} €";

    /// <summary>
    /// Color indicator for net balance
    /// </summary>
    public bool IsPositiveBalance => NetBalance >= 0;

    /// <summary>
    /// Number of transactions in this group
    /// </summary>
    public int TransactionCount => Count;

    public TransactionGroup(string name, string shortName, int year, int month, DateTime periodStart, DateTime periodEnd, IEnumerable<Transaction> transactions)
        : base(transactions.OrderByDescending(t => t.Data))
    {
        Name = name;
        ShortName = shortName;
        Year = year;
        Month = month;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    /// <summary>
    /// Create a group for a solar month
    /// </summary>
    public static TransactionGroup CreateSolarMonth(int year, int month, IEnumerable<Transaction> transactions)
    {
        var monthName = GetItalianMonthName(month);
        var name = $"{monthName} {year}";
        var shortName = $"{monthName.Substring(0, 3)} {year}";
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return new TransactionGroup(name, shortName, year, month, periodStart, periodEnd, transactions);
    }

    /// <summary>
    /// Create a group for a salary period
    /// </summary>
    public static TransactionGroup CreateSalaryPeriod(DateTime periodStart, DateTime periodEnd, IEnumerable<Transaction> transactions)
    {
        // The salary period is named after the starting month
        var monthName = GetItalianMonthName(periodStart.Month);
        var name = $"{monthName} {periodStart.Year}";
        var shortName = $"{periodStart:dd/MM} - {periodEnd:dd/MM}";

        return new TransactionGroup(name, shortName, periodStart.Year, periodStart.Month, periodStart, periodEnd, transactions);
    }

    private static string GetItalianMonthName(int month)
    {
        return month switch
        {
            1 => "Gennaio",
            2 => "Febbraio",
            3 => "Marzo",
            4 => "Aprile",
            5 => "Maggio",
            6 => "Giugno",
            7 => "Luglio",
            8 => "Agosto",
            9 => "Settembre",
            10 => "Ottobre",
            11 => "Novembre",
            12 => "Dicembre",
            _ => "Sconosciuto"
        };
    }
}

/// <summary>
/// Enum for transaction grouping mode
/// </summary>
public enum TransactionGroupingMode
{
    /// <summary>
    /// Group by calendar month (1st to end of month)
    /// </summary>
    SolarMonth,

    /// <summary>
    /// Group by salary period (salary day to salary day)
    /// </summary>
    SalaryPeriod
}
