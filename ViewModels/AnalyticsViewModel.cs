using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Logging;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILoggingService _loggingService;
    private readonly ISalaryPeriodService _salaryPeriodService;

    [ObservableProperty]
    private int selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> availableYears = new();

    [ObservableProperty]
    private List<MonthlyStats> monthlyStats = new();

    [ObservableProperty]
    private ISeries[] incomeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] expenseSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] savingsSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] yAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? highestIncomeMonth;

    [ObservableProperty]
    private string? highestExpenseMonth;

    [ObservableProperty]
    private decimal totalYearIncome;

    [ObservableProperty]
    private decimal totalYearExpenses;

    [ObservableProperty]
    private decimal totalYearSavings;

    [ObservableProperty]
    private decimal averageMonthlySavings;

    [ObservableProperty]
    private ISeries[] savingsCumulativeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] savingsBarSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private string? bestSavingsMonth;

    [ObservableProperty]
    private string? worstSavingsMonth;

    [ObservableProperty]
    private int positiveMonthsCount;

    [ObservableProperty]
    private int negativeMonthsCount;

    public AnalyticsViewModel(
        IAnalyticsService analyticsService,
        ILoggingService loggingService,
        ISalaryPeriodService salaryPeriodService)
    {
        _analyticsService = analyticsService;
        _loggingService = loggingService;
        _salaryPeriodService = salaryPeriodService;

        InitializeAxes();
    }

    public async Task InitializeAsync()
    {
        // Initialize available years based on current salary period
        await InitializeAvailableYearsAsync();

        await LoadDataAsync();
    }

    partial void OnSelectedYearChanged(int value)
    {
        MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            _loggingService.LogInfo($"Loading analytics for year {SelectedYear}");

            // Get monthly stats
            MonthlyStats = await _analyticsService.GetMonthlyStatsAsync(SelectedYear);

            // Calculate totals
            TotalYearIncome = MonthlyStats.Sum(s => s.Income);
            TotalYearExpenses = MonthlyStats.Sum(s => s.Expenses);
            TotalYearSavings = MonthlyStats.Sum(s => s.Savings);

            // Calculate savings stats
            var monthsWithData = MonthlyStats.Where(s => s.TransactionCount > 0).ToList();
            AverageMonthlySavings = monthsWithData.Count > 0
                ? monthsWithData.Average(s => s.Savings)
                : 0;

            PositiveMonthsCount = MonthlyStats.Count(s => s.Savings > 0);
            NegativeMonthsCount = MonthlyStats.Count(s => s.Savings < 0);

            // Best/Worst savings months
            var bestMonth = MonthlyStats.Where(s => s.TransactionCount > 0).OrderByDescending(s => s.Savings).FirstOrDefault();
            var worstMonth = MonthlyStats.Where(s => s.TransactionCount > 0).OrderBy(s => s.Savings).FirstOrDefault();

            BestSavingsMonth = bestMonth != null
                ? $"{bestMonth.MonthName}: {bestMonth.FormattedSavings}"
                : "N/A";
            WorstSavingsMonth = worstMonth != null
                ? $"{worstMonth.MonthName}: {worstMonth.FormattedSavings}"
                : "N/A";

            // Get highest months
            var highestIncome = await _analyticsService.GetHighestIncomeMonthAsync(SelectedYear);
            var highestExpense = await _analyticsService.GetHighestExpenseMonthAsync(SelectedYear);

            HighestIncomeMonth = highestIncome != null
                ? $"{highestIncome.MonthName}: {highestIncome.FormattedIncome}"
                : "N/A";

            HighestExpenseMonth = highestExpense != null
                ? $"{highestExpense.MonthName}: {highestExpense.FormattedExpenses}"
                : "N/A";

            // Update axes (for theme changes)
            InitializeAxes();

            // Update charts
            UpdateCharts();

            _loggingService.LogInfo($"Analytics loaded: {MonthlyStats.Sum(s => s.TransactionCount)} transactions");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading analytics", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCharts()
    {
        // Income/Expense bar chart
        IncomeSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Name = "Entrate",
                Values = MonthlyStats.Select(s => s.Income).ToArray(),
                Fill = new SolidColorPaint(SKColors.Green),
                Stroke = null,
                DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
            }
        };

        ExpenseSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Name = "Uscite",
                Values = MonthlyStats.Select(s => s.Expenses).ToArray(),
                Fill = new SolidColorPaint(SKColors.Red),
                Stroke = null,
                DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
            }
        };

        // Savings line chart (trend)
        SavingsSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Name = "Risparmio",
                Values = MonthlyStats.Select(s => s.Savings).ToArray(),
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(SKColors.Blue),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 },
                GeometrySize = 10,
                LineSmoothness = 0.5
            }
        };

        // Savings bar chart with positive/negative colors
        var savingsValues = MonthlyStats.Select(s => s.Savings).ToArray();
        var barColors = savingsValues.Select(v => v >= 0
            ? new SolidColorPaint(new SKColor(0, 200, 83)) // Green for positive
            : new SolidColorPaint(new SKColor(255, 23, 68)) // Red for negative
        ).ToArray();

        SavingsBarSeries = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Name = "Risparmio Mensile",
                Values = savingsValues,
                Fill = new SolidColorPaint(SKColors.Blue),
                Stroke = null,
                DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
            }
        };

        // Cumulative savings chart
        var cumulativeSavings = new decimal[12];
        decimal runningTotal = 0;
        for (int i = 0; i < MonthlyStats.Count && i < 12; i++)
        {
            runningTotal += MonthlyStats[i].Savings;
            cumulativeSavings[i] = runningTotal;
        }

        SavingsCumulativeSeries = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Name = "Risparmio Cumulativo",
                Values = cumulativeSavings,
                Fill = new SolidColorPaint(new SKColor(33, 150, 243, 50)), // Light blue fill
                Stroke = new SolidColorPaint(new SKColor(33, 150, 243)) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(new SKColor(33, 150, 243)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                GeometrySize = 8,
                LineSmoothness = 0.3
            }
        };
    }

    private void InitializeAxes()
    {
        // Determine label color based on current theme
        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
        var labelColor = isDarkMode ? SKColors.LightGray : SKColors.DarkGray;
        var separatorColor = isDarkMode ? new SKColor(60, 60, 60) : SKColors.LightGray;

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" },
                LabelsRotation = 0,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(labelColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = 1 }
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => $"€{value:N0}",
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(labelColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = 1 }
            }
        };
    }

    /// <summary>
    /// Initialize available years based on current salary period
    /// Includes all years from (currentEndYear - 2) to currentEndYear
    /// This ensures that if salary period spans two years (e.g., Dec 2025 - Jan 2026),
    /// both years are included in the picker
    /// </summary>
    private async Task InitializeAvailableYearsAsync()
    {
        try
        {
            // Get current salary period to determine the end year
            var (startDate, endDate) = await _salaryPeriodService.GetCurrentPeriodAsync();

            // Use the end date's year as the reference (could be next year if period spans years)
            var currentEndYear = endDate.Year;

            // Add current end year + 2 previous years
            AvailableYears.Clear();
            for (int i = 0; i < 3; i++)
            {
                AvailableYears.Add(currentEndYear - i);
            }

            // Set selected year to current end year if not already set
            if (!AvailableYears.Contains(SelectedYear))
            {
                SelectedYear = currentEndYear;
            }

            _loggingService.LogInfo($"Available years initialized: {string.Join(", ", AvailableYears)} (period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy})");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing available years", ex);

            // Fallback to simple calculation
            var currentYear = DateTime.Now.Year;
            AvailableYears.Clear();
            for (int i = 0; i < 3; i++)
            {
                AvailableYears.Add(currentYear - i);
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Clear cache and reload
        if (_analyticsService is AnalyticsService analyticsService)
        {
            analyticsService.ClearCache();
        }
        await LoadDataAsync();
    }
}
