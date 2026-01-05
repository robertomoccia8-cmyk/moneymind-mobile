using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;

namespace MoneyMindApp.Services;

/// <summary>
/// Service for calculating analytics and aggregated statistics
/// Uses salary periods (mesi stipendiali) instead of calendar months
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private readonly ISalaryPeriodService _salaryPeriodService;

    // Cache for monthly stats to avoid repeated DB queries
    private Dictionary<int, List<MonthlyStats>> _monthlyStatsCache = new();

    public AnalyticsService(
        DatabaseService databaseService,
        ILoggingService loggingService,
        ISalaryPeriodService salaryPeriodService)
    {
        _databaseService = databaseService;
        _loggingService = loggingService;
        _salaryPeriodService = salaryPeriodService;
    }

    /// <summary>
    /// Get monthly statistics for a specific year (12 salary periods)
    /// Uses salary periods based on when salary is PAID in that year (not when period starts)
    /// E.g., "January 2026" shows the period ending with January salary payment (Dec 15 2025 - Jan 22 2026)
    /// </summary>
    public async Task<List<MonthlyStats>> GetMonthlyStatsAsync(int year)
    {
        // Check cache first
        if (_monthlyStatsCache.ContainsKey(year))
        {
            _loggingService.LogDebug($"Returning cached monthly stats for {year}");
            return _monthlyStatsCache[year];
        }

        try
        {
            var stats = new List<MonthlyStats>();

            for (int month = 1; month <= 12; month++)
            {
                // Calculate the salary period that ENDS in this month (i.e., salary payment month)
                // "January 2026" = period ending with January 2026 salary = previous month to current month
                var (startDate, endDate) = await _salaryPeriodService.GetSalaryPeriodForPaymentMonthAsync(year, month);

                // Get transactions for this salary period
                var (income, expenses, savings, count) = await _databaseService.GetStatisticsAsync(startDate, endDate);

                stats.Add(new MonthlyStats
                {
                    Year = year,
                    Month = month,
                    Income = income,
                    Expenses = expenses,
                    TransactionCount = count,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            // Cache the results
            _monthlyStatsCache[year] = stats;

            _loggingService.LogInfo($"Calculated monthly stats for {year} (salary payment periods): {stats.Sum(s => s.TransactionCount)} transactions");
            return stats;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error calculating monthly stats for {year}", ex);
            return new List<MonthlyStats>();
        }
    }

    /// <summary>
    /// Get average daily spending for a period
    /// </summary>
    public async Task<decimal> GetAverageDailySpendingAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var (income, expenses, savings, count) = await _databaseService.GetStatisticsAsync(startDate, endDate);
            var days = (endDate - startDate).Days + 1;

            if (days <= 0)
                return 0;

            return expenses / days;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error calculating average daily spending", ex);
            return 0;
        }
    }

    /// <summary>
    /// Get highest expense month in a year
    /// </summary>
    public async Task<MonthlyStats?> GetHighestExpenseMonthAsync(int year)
    {
        var stats = await GetMonthlyStatsAsync(year);
        return stats.OrderByDescending(s => s.Expenses).FirstOrDefault();
    }

    /// <summary>
    /// Get highest income month in a year
    /// </summary>
    public async Task<MonthlyStats?> GetHighestIncomeMonthAsync(int year)
    {
        var stats = await GetMonthlyStatsAsync(year);
        return stats.OrderByDescending(s => s.Income).FirstOrDefault();
    }

    /// <summary>
    /// Clear cache (call after adding/editing/deleting transactions)
    /// </summary>
    public void ClearCache()
    {
        _monthlyStatsCache.Clear();
        _loggingService.LogDebug("Analytics cache cleared");
    }
}
