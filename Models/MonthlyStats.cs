using System.Globalization;

namespace MoneyMindApp.Models;

/// <summary>
/// Monthly statistics for analytics charts
/// </summary>
public class MonthlyStats
{
    private static readonly CultureInfo ItalianCulture = new CultureInfo("it-IT");

    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Savings => Income - Expenses;
    public int TransactionCount { get; set; }

    /// <summary>
    /// Start date of the salary period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the salary period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Month name in Italian (e.g., "gennaio")
    /// </summary>
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM", ItalianCulture);

    /// <summary>
    /// Short month name (e.g., "gen")
    /// </summary>
    public string MonthShortName => new DateTime(Year, Month, 1).ToString("MMM", ItalianCulture);

    /// <summary>
    /// Formatted income (e.g., "€1.234,56")
    /// </summary>
    public string FormattedIncome => Income.ToString("C2", ItalianCulture);

    /// <summary>
    /// Formatted expenses (e.g., "€1.234,56")
    /// </summary>
    public string FormattedExpenses => Expenses.ToString("C2", ItalianCulture);

    /// <summary>
    /// Formatted savings (e.g., "€1.234,56")
    /// </summary>
    public string FormattedSavings => Savings.ToString("C2", ItalianCulture);

    /// <summary>
    /// True if savings are positive or zero
    /// </summary>
    public bool IsSavingsPositive => Savings >= 0;

    /// <summary>
    /// Formatted period (e.g., "dal 15 Dic 2025 al 22 Gen 2026")
    /// </summary>
    public string FormattedPeriod
    {
        get
        {
            if (StartDate == default || EndDate == default)
                return string.Empty;

            return $"dal {StartDate:dd MMM yyyy} al {EndDate:dd MMM yyyy}";
        }
    }
}
