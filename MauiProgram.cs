using Microsoft.Extensions.Logging;
using MoneyMindApp.Services;
using MoneyMindApp.Services.Security;
using MoneyMindApp.Services.Platform;
using IApkInstallerService = MoneyMindApp.Services.Platform.IApkInstallerService;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Sync;
using MoneyMindApp.Services.Logging;
using MoneyMindApp.Services.License;
using MoneyMindApp.Services.Updates;
using MoneyMindApp.Services.ImportExport;
using MoneyMindApp.Services.Business;
using MoneyMindApp.Services.Backup;
using MoneyMindApp.ViewModels;
using MoneyMindApp.Views;
using CommunityToolkit.Maui;
using Serilog;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MoneyMindApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Initialize Serilog
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "logs", "moneymind.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Add Serilog
        builder.Logging.AddSerilog(dispose: true);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // FASE 0: Security & Critical Services
        RegisterPhase0Services(builder.Services);

        // Platform-specific services
        RegisterPlatformServices(builder.Services);

        // FASE 1: Main ViewModels & Pages
        RegisterPhase1Services(builder.Services);

        return builder.Build();
    }

    private static void RegisterPhase0Services(IServiceCollection services)
    {
        // Security Services
        services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
        services.AddSingleton<IPermissionService, PermissionService>();

        // Database Services
        services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<GlobalDatabaseService>();

        // Logging & Crash Reporting
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ICrashReportingService, CrashReportingService>();

        // WiFi Sync Service
        services.AddSingleton<IWiFiSyncService, WiFiSyncService>();

        // Backup Service
        services.AddSingleton<IBackupService, BackupService>();

        // Salary Period Service
        services.AddSingleton<ISalaryPeriodService, SalaryPeriodService>();

        // Analytics Service
        services.AddSingleton<IAnalyticsService, AnalyticsService>();

        // License & Updates Services (FASE 6)
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // Import/Export Service (FASE 6-7)
        services.AddSingleton<IImportExportService, ImportExportService>();
        services.AddSingleton<IConfigurazioneImportazioneService, ConfigurazioneImportazioneService>();
        services.AddSingleton<IImportValidationService, ImportValidationService>();

        // Duplicate Detection Service (FASE 5)
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();

        // Onboarding ViewModels
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<LicenseActivationViewModel>();
        services.AddTransient<CreateAccountViewModel>();
        services.AddTransient<BiometricSetupViewModel>();
        services.AddTransient<QuickTourViewModel>();

        // Onboarding Pages
        services.AddTransient<WelcomePage>();
        services.AddTransient<LicenseActivationPage>();
        services.AddTransient<CreateAccountPage>();
        services.AddTransient<BiometricSetupPage>();
        services.AddTransient<QuickTourPage>();
    }

    private static void RegisterPhase1Services(IServiceCollection services)
    {
        // FASE 1: Core ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<AccountSelectionViewModel>();

        // FASE 1: Core Pages
        services.AddTransient<MainPage>();
        services.AddTransient<TransactionsPage>();
        services.AddTransient<AccountSelectionPage>();

        // FASE 2: Transaction Management
        services.AddTransient<AddTransactionViewModel>();
        services.AddTransient<AddTransactionPage>();
        services.AddTransient<EditTransactionViewModel>();
        services.AddTransient<EditTransactionPage>();

        // FASE 3: Account Management
        services.AddTransient<AddAccountViewModel>();
        services.AddTransient<AddAccountPage>();
        services.AddTransient<EditAccountViewModel>();
        services.AddTransient<EditAccountPage>();

        // FASE 4: Salary Configuration
        services.AddTransient<SalaryConfigViewModel>();
        services.AddTransient<SalaryConfigPage>();

        // FASE 5: Analytics & Charts
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<AnalyticsPage>();

        // FASE 6: Settings & System
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<AdminPage>();

        // FASE 6-7: Import/Export
        services.AddTransient<ImportViewModel>();
        services.AddTransient<ImportPage>();
        services.AddTransient<ImportConfigSelectionViewModel>();
        services.AddTransient<ImportConfigSelectionPage>();
        services.AddTransient<ImportHeaderSelectionViewModel>();
        services.AddTransient<ImportHeaderSelectionPage>();
        services.AddTransient<ImportValidationViewModel>();
        services.AddTransient<ImportValidationPage>();
        services.AddTransient<ExportViewModel>();
        services.AddTransient<ExportPage>();

        // FASE 5: Duplicati
        services.AddTransient<DuplicatesViewModel>();
        services.AddTransient<DuplicatesPage>();

        // WiFi Sync
        services.AddTransient<WiFiSyncViewModel>();
        services.AddTransient<WiFiSyncPage>();
    }

    private static void RegisterPlatformServices(IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<IApkInstallerService, Platforms.Android.Services.ApkInstallerService>();
#else
        services.AddSingleton<IApkInstallerService, ApkInstallerServiceStub>();
#endif
    }
}
