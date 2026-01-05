using System.Globalization;

namespace MoneyMindApp.Models;

/// <summary>
/// Preview of upcoming salary payment date
/// </summary>
public class PaymentPreview
{
    private static readonly CultureInfo ItalianCulture = new CultureInfo("it-IT");

    public DateTime Day { get; set; }
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Formatted day number (e.g., "27")
    /// </summary>
    public string FormattedDay => Day.ToString("dd", ItalianCulture);

    /// <summary>
    /// Formatted month and year (e.g., "ottobre 2025")
    /// </summary>
    public string FormattedMonthYear => Day.ToString("MMMM yyyy", ItalianCulture);

    /// <summary>
    /// Formatted day of week (e.g., "lunedì")
    /// </summary>
    public string FormattedDayOfWeek => Day.ToString("dddd", ItalianCulture);
}
