namespace MoneyMindApp.Services;

public interface ISalaryPeriodService
{
    Task<(DateTime Start, DateTime End)> GetCurrentPeriodAsync();
    Task<(DateTime Start, DateTime End)> GetPeriodForDateAsync(DateTime date);
    Task<(DateTime Start, DateTime End)> GetSalaryPeriodForMonthAsync(int year, int month);
    Task<(DateTime Start, DateTime End)> GetSalaryPeriodForPaymentMonthAsync(int year, int month);
    void InvalidateCache();
}
