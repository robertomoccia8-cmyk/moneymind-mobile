using MoneyMindApp.Services.Security;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.License;

namespace MoneyMindApp;

public partial class App : Application
{
    private readonly IBiometricAuthService _biometricService;
    private readonly ILicenseService _licenseService;
    private readonly ILoggingService _loggingService;

    // Throttle license check: max 1 volta ogni 5 minuti
    private static DateTime _lastLicenseCheck = DateTime.MinValue;
    private const int LICENSE_CHECK_THROTTLE_MINUTES = 5;

    // Timer periodico per check licenza ogni 10 minuti (anche se app sempre aperta)
    private PeriodicTimer? _licenseCheckTimer;
    private CancellationTokenSource? _licenseCheckCts;
    private const int LICENSE_CHECK_INTERVAL_MINUTES = 10;

    public App(IBiometricAuthService biometricService, ILicenseService licenseService, ILoggingService loggingService)
    {
        InitializeComponent();

        _biometricService = biometricService;
        _licenseService = licenseService;
        _loggingService = loggingService;

        // Apply saved theme BEFORE creating Shell
        ApplySavedTheme();

        MainPage = new AppShell();

        // Avvia timer periodico per check licenza automatico
        StartPeriodicLicenseCheck();
    }

    private void ApplySavedTheme()
    {
        try
        {
            var savedTheme = Preferences.Get("app_theme", "Light"); // ✅ Default: Light instead of Auto

            UserAppTheme = savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                "Auto" => AppTheme.Unspecified,
                _ => AppTheme.Light // ✅ Fallback to Light
            };

            _loggingService.LogInfo($"Applied saved theme: {savedTheme}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error applying saved theme", ex);
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        _loggingService.LogInfo("App started");

        // Check if onboarding is completed
        var onboardingCompleted = Preferences.Get("onboarding_completed", false);

        if (!onboardingCompleted)
        {
            _loggingService.LogInfo("Onboarding not completed, navigating to welcome page");
            await Shell.Current.GoToAsync("onboarding/welcome");
            return;
        }

        // ========== LICENSE CHECK (come desktop) ==========
        _loggingService.LogInfo("Checking license status...");
        var (isValid, message, license) = await _licenseService.CheckLicenseStatusAsync();
        _lastLicenseCheck = DateTime.Now; // Update timestamp per throttling

        if (!isValid)
        {
            _loggingService.LogWarning($"License check failed: {message}");

            // 🔒 LICENZA OBBLIGATORIA - nessuna opzione di uscita
            await Shell.Current.DisplayAlert(
                "Licenza Richiesta",
                $"{message}\n\nMoneyMind richiede una licenza attiva per funzionare.\n\nVerrai portato alla pagina di attivazione.",
                "OK");

            // Vai a pagina attivazione (OBBLIGATORIO)
            await Shell.Current.GoToAsync("onboarding/license");
            return;
        }

        _loggingService.LogInfo($"License valid: {license?.Email}, Subscription: {license?.Subscription}");

        // ✅ CHECK IF ACCOUNTS EXIST - Skip account creation if they do
        try
        {
            var globalDbService = Handler.MauiContext!.Services.GetRequiredService<MoneyMindApp.Services.Database.GlobalDatabaseService>();
            await globalDbService.InitializeAsync();
            var existingAccounts = await globalDbService.GetAllAccountsAsync();

            if (existingAccounts.Count == 0)
            {
                _loggingService.LogInfo("No accounts found - redirecting to account creation");
                await Shell.Current.DisplayAlert(
                    "Crea il tuo primo conto",
                    "Benvenuto! Crea il tuo primo conto per iniziare.",
                    "OK");
                await Shell.Current.GoToAsync("onboarding/account");
                return;
            }

            _loggingService.LogInfo($"Found {existingAccounts.Count} existing accounts - skipping account creation");

            // Set current account if not set
            var currentAccountId = Preferences.Get("current_account_id", 0);
            if (currentAccountId == 0 && existingAccounts.Count > 0)
            {
                var firstAccount = existingAccounts.OrderByDescending(a => a.LastAccessedAt).FirstOrDefault();
                if (firstAccount != null)
                {
                    Preferences.Set("current_account_id", firstAccount.Id);
                    _loggingService.LogInfo($"Set current account to: {firstAccount.Nome} (ID: {firstAccount.Id})");
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error checking existing accounts", ex);
            // Continue with normal flow if check fails
        }

        // Check biometric auth if enabled
        var biometricEnabled = Preferences.Get("biometric_enabled", false);
        if (biometricEnabled)
        {
            // First check if biometric is available on this device
            var isAvailable = await _biometricService.IsAvailableAsync();

            if (!isAvailable)
            {
                _loggingService.LogWarning("Biometric enabled but not available on device - disabling");
                Preferences.Set("biometric_enabled", false);

                await Shell.Current.DisplayAlert(
                    "Biometric non disponibile",
                    "Sblocco biometrico disabilitato perché il dispositivo non supporta l'autenticazione biometrica.",
                    "OK");
            }
            else
            {
                _loggingService.LogInfo("Biometric authentication required");

                // Give user 3 attempts
                int maxAttempts = 3;
                bool authenticated = false;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    authenticated = await _biometricService.AuthenticateAsync("Accedi a MoneyMind");

                    if (authenticated)
                    {
                        _loggingService.LogInfo("Biometric authentication successful");
                        break;
                    }

                    _loggingService.LogWarning($"Biometric authentication failed - attempt {attempt}/{maxAttempts}");

                    // If not the last attempt, don't show anything - the plugin will show retry dialog
                    if (attempt == maxAttempts)
                    {
                        // All attempts failed - ask user what to do
                        var disableBiometric = await Shell.Current.DisplayAlert(
                            "Autenticazione fallita",
                            "Autenticazione biometrica fallita dopo 3 tentativi. Vuoi disabilitare lo sblocco biometrico?",
                            "Disabilita",
                            "Chiudi app");

                        if (disableBiometric)
                        {
                            Preferences.Set("biometric_enabled", false);
                            _loggingService.LogInfo("Biometric disabled by user after failed attempts");
                            authenticated = true; // Allow access since they disabled it
                        }
                        else
                        {
                            _loggingService.LogWarning("User chose to close app after failed biometric attempts");
                            Application.Current?.Quit();
                            return;
                        }
                    }
                }

                if (!authenticated)
                {
                    Application.Current?.Quit();
                    return;
                }
            }
        }

        // Navigate to main page
        // Navigation removed - let Shell handle default tab naturally
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _loggingService.LogInfo("App entering sleep mode");

        // Save last active time for auto-lock feature
        Preferences.Set("last_active_time", DateTime.Now.Ticks);
    }

    protected override async void OnResume()
    {
        base.OnResume();
        _loggingService.LogInfo("App resuming from sleep");

        // ========== LICENSE CHECK (throttled - max 1 ogni 5 minuti) ==========
        var minutesSinceLastCheck = (DateTime.Now - _lastLicenseCheck).TotalMinutes;

        if (minutesSinceLastCheck >= LICENSE_CHECK_THROTTLE_MINUTES)
        {
            _loggingService.LogInfo($"OnResume: Checking license (last check {minutesSinceLastCheck:F1} minutes ago)");

            var (isValid, message, license) = await _licenseService.CheckLicenseStatusAsync();
            _lastLicenseCheck = DateTime.Now; // Update last check time

            if (!isValid)
            {
                _loggingService.LogWarning($"OnResume: License invalid - {message}");

                // 🚫 BLOCCA IMMEDIATAMENTE anche se app era aperta
                await Shell.Current.DisplayAlert(
                    "Licenza non valida",
                    $"{message}\n\nL'app verrà chiusa.",
                    "OK");

                Application.Current?.Quit();
                return;
            }

            _loggingService.LogInfo($"OnResume: License valid - {license?.Email}, Subscription: {license?.Subscription}");
        }
        else
        {
            _loggingService.LogInfo($"OnResume: License check skipped (throttled - last check {minutesSinceLastCheck:F1} minutes ago)");
        }

        // Check if auto-lock is enabled (5 minutes inactivity)
        var biometricEnabled = Preferences.Get("biometric_enabled", false);
        var autoLockMinutes = Preferences.Get("auto_lock_minutes", 5);

        if (biometricEnabled && autoLockMinutes > 0)
        {
            var lastActiveTicks = Preferences.Get("last_active_time", DateTime.Now.Ticks);
            var lastActiveTime = new DateTime(lastActiveTicks);
            var inactiveMinutes = (DateTime.Now - lastActiveTime).TotalMinutes;

            if (inactiveMinutes >= autoLockMinutes)
            {
                // Check if biometric is still available
                var isAvailable = await _biometricService.IsAvailableAsync();

                if (!isAvailable)
                {
                    _loggingService.LogWarning("Auto-lock: biometric not available - disabling");
                    Preferences.Set("biometric_enabled", false);
                }
                else
                {
                    _loggingService.LogInfo($"Auto-lock triggered after {inactiveMinutes:F1} minutes");

                    // Give user 3 attempts
                    int maxAttempts = 3;
                    bool authenticated = false;

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        authenticated = await _biometricService.AuthenticateAsync("Sblocca MoneyMind");

                        if (authenticated)
                        {
                            _loggingService.LogInfo("Auto-lock authentication successful");
                            break;
                        }

                        _loggingService.LogWarning($"Auto-lock authentication failed - attempt {attempt}/{maxAttempts}");

                        if (attempt == maxAttempts)
                        {
                            // All attempts failed - ask user what to do
                            var disableBiometric = await Shell.Current.DisplayAlert(
                                "Autenticazione fallita",
                                "Autenticazione biometrica fallita dopo 3 tentativi. Vuoi disabilitare lo sblocco biometrico?",
                                "Disabilita",
                                "Chiudi app");

                            if (disableBiometric)
                            {
                                Preferences.Set("biometric_enabled", false);
                                _loggingService.LogInfo("Biometric disabled by user after failed auto-lock attempts");
                                authenticated = true; // Allow access since they disabled it
                            }
                            else
                            {
                                _loggingService.LogWarning("User chose to close app after failed auto-lock attempts");
                                Application.Current?.Quit();
                            }
                        }
                    }
                }
            }
        }
    }

    private void StartPeriodicLicenseCheck()
    {
        _licenseCheckCts = new CancellationTokenSource();
        _licenseCheckTimer = new PeriodicTimer(TimeSpan.FromMinutes(LICENSE_CHECK_INTERVAL_MINUTES));

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _licenseCheckTimer.WaitForNextTickAsync(_licenseCheckCts.Token))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _loggingService.LogInfo($"Periodic license check (every {LICENSE_CHECK_INTERVAL_MINUTES} minutes)");

                        var (isValid, message, license) = await _licenseService.CheckLicenseStatusAsync();
                        _lastLicenseCheck = DateTime.Now; // Update timestamp

                        if (!isValid)
                        {
                            _loggingService.LogWarning($"Periodic check: License invalid - {message}");

                            await Shell.Current.DisplayAlert(
                                "Licenza Non Valida",
                                $"{message}\n\nL'app verrà chiusa.",
                                "OK");

                            Application.Current?.Quit();
                        }
                        else
                        {
                            _loggingService.LogInfo($"Periodic check: License valid - {license?.Email}");
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                _loggingService.LogInfo("Periodic license check cancelled");
            }
        }, _licenseCheckCts.Token);
    }
}
