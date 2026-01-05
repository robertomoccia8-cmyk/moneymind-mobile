using MoneyMindApp.Models;

namespace MoneyMindApp.Services;

public interface IAnalyticsService
{
    /// <summary>
    /// Get monthly statistics for a specific year
    /// </summary>
    Task<List<MonthlyStats>> GetMonthlyStatsAsync(int year);

    /// <summary>
    /// Get average daily spending for a period
    /// </summary>
    Task<decimal> GetAverageDailySpendingAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get highest expense month in a year
    /// </summary>
    Task<MonthlyStats?> GetHighestExpenseMonthAsync(int year);

    /// <summary>
    /// Get highest income month in a year
    /// </summary>
    Task<MonthlyStats?> GetHighestIncomeMonthAsync(int year);
}
