using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;

namespace MoneyMindApp.Services;

/// <summary>
/// Service for calculating salary periods (mese stipendiale)
/// Period = from salary day to salary day next month
/// Supports monthly exceptions (e.g., December → day 15 instead of default)
/// </summary>
public class SalaryPeriodService : ISalaryPeriodService
{
    private readonly GlobalDatabaseService _globalDb;
    private const int DEFAULT_SALARY_DAY = 27; // Default if not configured

    // Cache to avoid repeated DB reads
    private int? _cachedSalaryDay;
    private string? _cachedWeekendHandling;
    private List<SalaryException>? _cachedExceptions;

    public SalaryPeriodService(GlobalDatabaseService globalDb)
    {
        _globalDb = globalDb;
    }

    /// <summary>
    /// Invalidate cached settings (call after saving new configuration)
    /// </summary>
    public void InvalidateCache()
    {
        _cachedSalaryDay = null;
        _cachedWeekendHandling = null;
        _cachedExceptions = null;
    }

    /// <summary>
    /// Get current salary period
    /// </summary>
    public async Task<(DateTime Start, DateTime End)> GetCurrentPeriodAsync()
    {
        return await GetPeriodForDateAsync(DateTime.Now);
    }

    /// <summary>
    /// Get salary period for a specific date
    /// </summary>
    public async Task<(DateTime Start, DateTime End)> GetPeriodForDateAsync(DateTime date)
    {
        string weekendHandling = await GetConfiguredWeekendHandlingAsync();

        // Get salary day for current month (may have exception)
        int currentMonthSalaryDay = await GetPaymentDayForMonthAsync(date.Year, date.Month);

        DateTime periodStart;
        DateTime periodEnd;

        // IMPORTANT: Apply weekend handling to payment dates FIRST, then calculate period boundaries
        // Period boundaries must be calculated from the adjusted payment dates

        // If today is before salary day, period is last month → this month
        if (date.Day < currentMonthSalaryDay)
        {
            // Get last month's salary day
            var lastMonth = date.AddMonths(-1);
            int lastMonthSalaryDay = await GetPaymentDayForMonthAsync(lastMonth.Year, lastMonth.Month);

            // Calculate actual payment dates with weekend handling
            DateTime lastPaymentDate = CreateSafeDate(lastMonth.Year, lastMonth.Month, lastMonthSalaryDay);
            lastPaymentDate = ApplyWeekendHandling(lastPaymentDate, weekendHandling);

            DateTime currentPaymentDate = CreateSafeDate(date.Year, date.Month, currentMonthSalaryDay);
            currentPaymentDate = ApplyWeekendHandling(currentPaymentDate, weekendHandling);

            // Period = from last payment date to day before current payment date
            periodStart = lastPaymentDate;
            periodEnd = currentPaymentDate.AddDays(-1);
        }
        // If today is after/on salary day, period is this month → next month
        else
        {
            // Get next month's salary day
            var nextMonth = date.AddMonths(1);
            int nextMonthSalaryDay = await GetPaymentDayForMonthAsync(nextMonth.Year, nextMonth.Month);

            // Calculate actual payment dates with weekend handling
            DateTime currentPaymentDate = CreateSafeDate(date.Year, date.Month, currentMonthSalaryDay);
            currentPaymentDate = ApplyWeekendHandling(currentPaymentDate, weekendHandling);

            DateTime nextPaymentDate = CreateSafeDate(nextMonth.Year, nextMonth.Month, nextMonthSalaryDay);
            nextPaymentDate = ApplyWeekendHandling(nextPaymentDate, weekendHandling);

            // Period = from current payment date to day before next payment date
            periodStart = currentPaymentDate;
            periodEnd = nextPaymentDate.AddDays(-1);
        }

        return (periodStart, periodEnd);
    }

    /// <summary>
    /// Get salary period for a specific month/year
    /// This returns the period that START in that month (e.g., November 2025 → Nov 21 to Dec 20)
    /// </summary>
    public async Task<(DateTime Start, DateTime End)> GetSalaryPeriodForMonthAsync(int year, int month)
    {
        string weekendHandling = await GetConfiguredWeekendHandlingAsync();

        // Get salary day for this month
        int salaryDay = await GetPaymentDayForMonthAsync(year, month);

        // Get salary day for next month
        var nextMonth = new DateTime(year, month, 1).AddMonths(1);
        int nextMonthSalaryDay = await GetPaymentDayForMonthAsync(nextMonth.Year, nextMonth.Month);

        // IMPORTANT: Apply weekend handling to payment dates FIRST, then calculate period boundaries
        // E.g., if salary day 23 falls on Saturday (Aug 23 2025), it gets moved to Friday (Aug 22)
        // Period must start on Aug 22 (adjusted payment date) and end on day before next payment

        // Calculate actual payment dates with weekend handling
        DateTime currentPaymentDate = CreateSafeDate(year, month, salaryDay);
        currentPaymentDate = ApplyWeekendHandling(currentPaymentDate, weekendHandling);

        DateTime nextPaymentDate = CreateSafeDate(nextMonth.Year, nextMonth.Month, nextMonthSalaryDay);
        nextPaymentDate = ApplyWeekendHandling(nextPaymentDate, weekendHandling);

        // Period = from current payment date to day before next payment date
        DateTime periodStart = currentPaymentDate;
        DateTime periodEnd = nextPaymentDate.AddDays(-1);

        return (periodStart, periodEnd);
    }

    /// <summary>
    /// Get salary period for a month where salary is PAID (not where period starts)
    /// E.g., "January 2026" returns the period ending with January 2026 salary payment
    /// This is used for Analytics to show stats grouped by payment month
    /// </summary>
    public async Task<(DateTime Start, DateTime End)> GetSalaryPeriodForPaymentMonthAsync(int year, int month)
    {
        string weekendHandling = await GetConfiguredWeekendHandlingAsync();

        // Get payment day for current month (with exceptions)
        int currentMonthPaymentDay = await GetPaymentDayForMonthAsync(year, month);

        // Get payment day for previous month
        var previousMonth = new DateTime(year, month, 1).AddMonths(-1);
        int previousMonthPaymentDay = await GetPaymentDayForMonthAsync(previousMonth.Year, previousMonth.Month);

        // IMPORTANT: Apply weekend handling to payment dates FIRST, then calculate period boundaries
        // E.g., if salary day 23 falls on Saturday (Aug 23 2025), it gets moved to Friday (Aug 22)
        // Period must end on Aug 21 (day before adjusted payment date)

        // Calculate actual payment dates with weekend handling
        DateTime currentPaymentDate = CreateSafeDate(year, month, currentMonthPaymentDay);
        currentPaymentDate = ApplyWeekendHandling(currentPaymentDate, weekendHandling);

        DateTime previousPaymentDate = CreateSafeDate(previousMonth.Year, previousMonth.Month, previousMonthPaymentDay);
        previousPaymentDate = ApplyWeekendHandling(previousPaymentDate, weekendHandling);

        // Period = from previous payment date to day before current payment date
        DateTime periodStart = previousPaymentDate;
        DateTime periodEnd = currentPaymentDate.AddDays(-1);

        return (periodStart, periodEnd);
    }

    /// <summary>
    /// Get payment day for a specific month, checking exceptions first
    /// Checks for specific year exception first, then permanent exception, then default
    /// </summary>
    public async Task<int> GetPaymentDayForMonthAsync(int year, int month)
    {
        // Load exceptions if not cached
        if (_cachedExceptions == null)
        {
            try
            {
                _cachedExceptions = await _globalDb.GetAllSalaryExceptionsAsync();
            }
            catch
            {
                _cachedExceptions = new List<SalaryException>();
            }
        }

        // Check if there's a specific year exception for this month/year
        var exception = _cachedExceptions.FirstOrDefault(e => e.Mese == month && e.Anno == year);
        if (exception != null)
        {
            return exception.GiornoAlternativo;
        }

        // No specific year exception, check for permanent exception (IsPermanent = true)
        var permanentException = _cachedExceptions.FirstOrDefault(e => e.Mese == month && e.IsPermanent);
        if (permanentException != null)
        {
            return permanentException.GiornoAlternativo;
        }

        // No exception, return default configured day
        return await GetConfiguredPaymentDayAsync();
    }

    /// <summary>
    /// Create a date, handling edge cases (e.g., day 31 in February)
    /// </summary>
    private DateTime CreateSafeDate(int year, int month, int day)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int actualDay = Math.Min(day, daysInMonth);
        return new DateTime(year, month, actualDay);
    }

    private async Task<int> GetConfiguredPaymentDayAsync()
    {
        // Return cached value if available
        if (_cachedSalaryDay.HasValue)
            return _cachedSalaryDay.Value;

        try
        {
            var setting = await _globalDb.GetSettingAsync("salary_payment_day");
            if (!string.IsNullOrEmpty(setting) && int.TryParse(setting, out int day))
            {
                _cachedSalaryDay = day;
                return day;
            }
        }
        catch
        {
            // If error reading settings, use default
        }

        _cachedSalaryDay = DEFAULT_SALARY_DAY;
        return DEFAULT_SALARY_DAY;
    }

    private async Task<string> GetConfiguredWeekendHandlingAsync()
    {
        // Return cached value if available
        if (_cachedWeekendHandling != null)
            return _cachedWeekendHandling;

        try
        {
            var setting = await _globalDb.GetSettingAsync("salary_weekend_handling");
            if (!string.IsNullOrEmpty(setting))
            {
                _cachedWeekendHandling = setting;
                return setting;
            }
        }
        catch
        {
            // If error reading settings, use default
        }

        _cachedWeekendHandling = "Anticipa a venerdì";
        return _cachedWeekendHandling;
    }

    private DateTime ApplyWeekendHandling(DateTime date, string handling)
    {
        if (handling == "Anticipa a venerdì")
        {
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-1);
            }
        }
        else if (handling == "Posticipa a lunedì")
        {
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
        }
        // "Ignora (paga nel weekend)" = no changes

        return date;
    }
}
