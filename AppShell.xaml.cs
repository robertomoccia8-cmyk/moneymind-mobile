using MoneyMindApp.Views;

namespace MoneyMindApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register onboarding routes programmatically
        Routing.RegisterRoute("onboarding/welcome", typeof(WelcomePage));
        Routing.RegisterRoute("onboarding/license", typeof(LicenseActivationPage));
        Routing.RegisterRoute("onboarding/account", typeof(CreateAccountPage));
        Routing.RegisterRoute("onboarding/biometric", typeof(BiometricSetupPage));
        Routing.RegisterRoute("onboarding/tour", typeof(QuickTourPage));

        // Register transaction management routes
        Routing.RegisterRoute("addtransaction", typeof(AddTransactionPage));
        Routing.RegisterRoute("edittransaction", typeof(EditTransactionPage));

        // Register account management routes
        Routing.RegisterRoute("addaccount", typeof(AddAccountPage));
        Routing.RegisterRoute("editaccount", typeof(EditAccountPage));

        // Register salary configuration routes
        Routing.RegisterRoute("salaryconfig", typeof(SalaryConfigPage));

        // Register admin panel route
        Routing.RegisterRoute("admin", typeof(AdminPage));

        // Register import/export routes (FASE 6-7)
        Routing.RegisterRoute("import", typeof(ImportPage));
        Routing.RegisterRoute("importConfigSelection", typeof(ImportConfigSelectionPage));
        Routing.RegisterRoute("importHeaderSelection", typeof(ImportHeaderSelectionPage));
        Routing.RegisterRoute("importColumnMapping", typeof(ImportPage));
        Routing.RegisterRoute("importValidation", typeof(ImportValidationPage));
        Routing.RegisterRoute("export", typeof(ExportPage));

        // Register duplicates route (FASE 5)
        Routing.RegisterRoute("duplicates", typeof(DuplicatesPage));

        // Register WiFi Sync route
        Routing.RegisterRoute("wifisync", typeof(WiFiSyncPage));
    }
}
