using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

public partial class SalaryConfigViewModel : ObservableObject
{
    private readonly GlobalDatabaseService _globalDatabaseService;
    private readonly ILoggingService _loggingService;
    private readonly ISalaryPeriodService _salaryPeriodService;
    private readonly DatabaseService _databaseService;

    #region Base Configuration Properties

    [ObservableProperty]
    private int paymentDay = 27;

    [ObservableProperty]
    private ObservableCollection<string> weekendOptions = new()
    {
        "Ignora (paga nel weekend)",
        "Anticipa a venerdì",
        "Posticipa a lunedì"
    };

    [ObservableProperty]
    private string? selectedWeekendOption;

    [ObservableProperty]
    private string weekendExplanation = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PaymentPreview> previewPayments = new();

    [ObservableProperty]
    private bool isSaving;

    #endregion

    #region Exceptions Properties

    [ObservableProperty]
    private ObservableCollection<SalaryException> salaryExceptions = new();

    [ObservableProperty]
    private ObservableCollection<string> monthOptions = new()
    {
        "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno",
        "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre"
    };

    [ObservableProperty]
    private ObservableCollection<string> yearOptions = new();

    [ObservableProperty]
    private string? selectedExceptionMonth;

    [ObservableProperty]
    private string? selectedExceptionYear;

    [ObservableProperty]
    private int exceptionPaymentDay = 15;

    [ObservableProperty]
    private bool isAddingException;

    #endregion

    public SalaryConfigViewModel(
        GlobalDatabaseService globalDatabaseService,
        ILoggingService loggingService,
        ISalaryPeriodService salaryPeriodService,
        DatabaseService databaseService)
    {
        _globalDatabaseService = globalDatabaseService;
        _loggingService = loggingService;
        _salaryPeriodService = salaryPeriodService;
        _databaseService = databaseService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Initialize database for current account to load transactions
            var currentAccountId = Preferences.Get("current_account_id", 0);
            if (currentAccountId > 0)
            {
                await _databaseService.InitializeAsync(currentAccountId);
            }

            // Populate year options dynamically from transactions
            await LoadYearOptionsAsync();

            // Load saved configuration
            var paymentDaySetting = await _globalDatabaseService.GetSettingAsync("salary_payment_day");
            if (!string.IsNullOrEmpty(paymentDaySetting) && int.TryParse(paymentDaySetting, out int savedDay))
            {
                PaymentDay = savedDay;
            }

            var weekendSetting = await _globalDatabaseService.GetSettingAsync("salary_weekend_handling");
            if (!string.IsNullOrEmpty(weekendSetting))
            {
                SelectedWeekendOption = weekendSetting;
            }
            else
            {
                SelectedWeekendOption = "Anticipa a venerdì";
            }

            // Load exceptions
            await LoadExceptionsAsync();

            UpdateWeekendExplanation();
            UpdatePreview();

            _loggingService.LogInfo($"Salary configuration loaded: Day {PaymentDay}, Weekend: {SelectedWeekendOption}, Exceptions: {SalaryExceptions.Count}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading salary configuration", ex);
            SelectedWeekendOption = "Anticipa a venerdì";
            UpdateWeekendExplanation();
            UpdatePreview();
        }
    }

    private async Task LoadYearOptionsAsync()
    {
        try
        {
            var transactions = await _databaseService.GetAllTransactionsAsync();

            if (transactions.Any())
            {
                var minYear = transactions.Min(t => t.Data.Year);
                var maxYear = Math.Max(transactions.Max(t => t.Data.Year), DateTime.Now.Year);

                var years = new List<string>();

                // Add all years from min to max
                for (int year = minYear; year <= maxYear; year++)
                {
                    years.Add(year.ToString());
                }

                // Add "Permanente" option
                years.Add("Permanente");

                YearOptions = new ObservableCollection<string>(years);

                // Set default selection to current year
                SelectedExceptionYear = DateTime.Now.Year.ToString();
            }
            else
            {
                // No transactions, use default: current year, next 2 years, + Permanente
                var currentYear = DateTime.Now.Year;
                YearOptions = new ObservableCollection<string>
                {
                    currentYear.ToString(),
                    (currentYear + 1).ToString(),
                    (currentYear + 2).ToString(),
                    "Permanente"
                };
                SelectedExceptionYear = currentYear.ToString();
            }

            _loggingService.LogInfo($"Year options loaded: {string.Join(", ", YearOptions)}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading year options", ex);

            // Fallback to default
            var currentYear = DateTime.Now.Year;
            YearOptions = new ObservableCollection<string>
            {
                currentYear.ToString(),
                (currentYear + 1).ToString(),
                (currentYear + 2).ToString(),
                "Permanente"
            };
            SelectedExceptionYear = currentYear.ToString();
        }
    }

    private async Task LoadExceptionsAsync()
    {
        try
        {
            var exceptions = await _globalDatabaseService.GetAllSalaryExceptionsAsync();
            SalaryExceptions = new ObservableCollection<SalaryException>(exceptions);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error loading salary exceptions", ex);
            SalaryExceptions = new ObservableCollection<SalaryException>();
        }
    }

    partial void OnPaymentDayChanged(int value)
    {
        UpdatePreview();
    }

    partial void OnSelectedWeekendOptionChanged(string? value)
    {
        UpdateWeekendExplanation();
        UpdatePreview();
    }

    private void UpdateWeekendExplanation()
    {
        WeekendExplanation = SelectedWeekendOption switch
        {
            "Ignora (paga nel weekend)" => "Il pagamento rimane nel weekend. Il periodo continua normalmente.",
            "Anticipa a venerdì" => "Se il pagamento cade sabato o domenica, viene anticipato al venerdì precedente.",
            "Posticipa a lunedì" => "Se il pagamento cade sabato o domenica, viene posticipato al lunedì successivo.",
            _ => ""
        };
    }

    private void UpdatePreview()
    {
        var previews = new List<PaymentPreview>();
        var today = DateTime.Now;

        for (int i = 0; i < 3; i++)
        {
            var targetMonth = today.AddMonths(i);

            // Check if there's an exception for this month
            // First check for specific year exception, then check for permanent exception
            var exception = SalaryExceptions.FirstOrDefault(e =>
                e.Mese == targetMonth.Month && e.Anno == targetMonth.Year);

            if (exception == null)
            {
                // No specific year exception, check for permanent exception
                exception = SalaryExceptions.FirstOrDefault(e =>
                    e.Mese == targetMonth.Month && e.IsPermanent);
            }

            int dayToUse = exception?.GiornoAlternativo ?? PaymentDay;
            var paymentDate = CalculatePaymentDate(targetMonth.Year, targetMonth.Month, dayToUse);

            var preview = new PaymentPreview
            {
                Day = paymentDate,
                Note = GetPaymentNote(targetMonth.Year, targetMonth.Month, paymentDate, dayToUse, exception != null)
            };

            previews.Add(preview);
        }

        PreviewPayments = new ObservableCollection<PaymentPreview>(previews);
    }

    private DateTime CalculatePaymentDate(int year, int month, int day)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int actualDay = Math.Min(day, daysInMonth);

        var date = new DateTime(year, month, actualDay);

        // Handle weekend
        if (SelectedWeekendOption == "Anticipa a venerdì")
        {
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-1);
            }
        }
        else if (SelectedWeekendOption == "Posticipa a lunedì")
        {
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
        }

        return date;
    }

    private string GetPaymentNote(int year, int month, DateTime actualDate, int requestedDay, bool isException)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int actualRequestedDay = Math.Min(requestedDay, daysInMonth);
        var originalDate = new DateTime(year, month, actualRequestedDay);

        var notes = new List<string>();

        // Mark if it's an exception
        if (isException)
        {
            notes.Add("eccezione");
        }

        // Check if adjusted for day overflow
        if (actualRequestedDay != requestedDay)
        {
            notes.Add($"giorno {requestedDay} non disponibile");
        }

        // Check if adjusted for weekend
        if (actualDate != originalDate && !isException)
        {
            if (SelectedWeekendOption == "Anticipa a venerdì")
            {
                notes.Add("anticipato");
            }
            else if (SelectedWeekendOption == "Posticipa a lunedì")
            {
                notes.Add("posticipato");
            }
        }
        else if (actualDate != originalDate && isException)
        {
            if (SelectedWeekendOption == "Anticipa a venerdì")
            {
                notes.Add("anticipato");
            }
            else if (SelectedWeekendOption == "Posticipa a lunedì")
            {
                notes.Add("posticipato");
            }
        }

        return notes.Count > 0 ? $"({string.Join(", ", notes)})" : string.Empty;
    }

    #region Exception Commands

    [RelayCommand]
    private async Task AddExceptionAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedExceptionMonth))
            {
                await Shell.Current.DisplayAlert("Attenzione", "Seleziona un mese", "OK");
                return;
            }

            if (string.IsNullOrEmpty(SelectedExceptionYear))
            {
                await Shell.Current.DisplayAlert("Attenzione", "Seleziona un anno", "OK");
                return;
            }

            IsAddingException = true;

            int monthNumber = MonthOptions.IndexOf(SelectedExceptionMonth) + 1;
            bool isPermanent = SelectedExceptionYear == "Permanente";
            int yearNumber = isPermanent ? 0 : int.Parse(SelectedExceptionYear);

            // Check if exception already exists
            SalaryException? existing;
            if (isPermanent)
            {
                // For permanent exceptions, check only month with IsPermanent = true
                existing = SalaryExceptions.FirstOrDefault(e => e.Mese == monthNumber && e.IsPermanent);
            }
            else
            {
                // For specific year exceptions, check month + year
                existing = SalaryExceptions.FirstOrDefault(e => e.Mese == monthNumber && e.Anno == yearNumber);
            }

            if (existing != null)
            {
                bool update = await Shell.Current.DisplayAlert(
                    "Eccezione esistente",
                    $"Esiste già un'eccezione per {SelectedExceptionMonth} {SelectedExceptionYear}.\n\nVuoi aggiornarla?",
                    "Aggiorna", "Annulla");

                if (!update) return;
            }

            var exception = new SalaryException
            {
                Mese = monthNumber,
                Anno = yearNumber,
                IsPermanent = isPermanent,
                GiornoAlternativo = ExceptionPaymentDay
            };

            await _globalDatabaseService.InsertSalaryExceptionAsync(exception);

            // Reload exceptions
            await LoadExceptionsAsync();

            // Invalidate cache
            _salaryPeriodService.InvalidateCache();

            // Update preview
            UpdatePreview();

            _loggingService.LogInfo($"Salary exception added: {SelectedExceptionMonth} {SelectedExceptionYear} → Day {ExceptionPaymentDay} (Permanent: {isPermanent})");

            // Reset form
            SelectedExceptionMonth = null;
            ExceptionPaymentDay = 15;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error adding salary exception", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile aggiungere l'eccezione", "OK");
        }
        finally
        {
            IsAddingException = false;
        }
    }

    [RelayCommand]
    private async Task DeleteExceptionAsync(SalaryException exception)
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma eliminazione",
                $"Eliminare l'eccezione per {exception.MeseNome} {exception.Anno}?",
                "Elimina", "Annulla");

            if (!confirm) return;

            await _globalDatabaseService.DeleteSalaryExceptionAsync(exception.Id);

            // Reload exceptions
            await LoadExceptionsAsync();

            // Invalidate cache
            _salaryPeriodService.InvalidateCache();

            // Update preview
            UpdatePreview();

            _loggingService.LogInfo($"Salary exception deleted: {exception.MeseNome} {exception.Anno}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting salary exception", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile eliminare l'eccezione", "OK");
        }
    }

    #endregion

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            // Save to database
            await _globalDatabaseService.SaveSettingAsync("salary_payment_day", PaymentDay.ToString());
            await _globalDatabaseService.SaveSettingAsync("salary_weekend_handling", SelectedWeekendOption ?? "Anticipa a venerdì");

            // Invalidate cache so Dashboard uses new values immediately
            _salaryPeriodService.InvalidateCache();

            _loggingService.LogInfo($"Salary configuration saved: Day {PaymentDay}, Weekend: {SelectedWeekendOption}");

            await Shell.Current.DisplayAlert("Successo", "Configurazione salvata!\n\nIl periodo stipendiale verrà aggiornato nella Dashboard.", "OK");

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error saving salary configuration", ex);
            await Shell.Current.DisplayAlert("Errore", "Impossibile salvare la configurazione", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
