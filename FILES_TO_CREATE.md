# Files da Creare - Checklist Completa

## ⚠️ Fase 0 - Security & Critical Setup (PRIMA DI TUTTO!)

### Security Services (5 files)
- [ ] `Services/Security/BiometricAuthService.cs` - Face ID / Touch ID
- [ ] `Services/Security/EncryptionService.cs` - AES-256 encryption
- [ ] `Services/Platform/IPermissionService.cs` - Permission interface
- [ ] `Platforms/Android/Services/PermissionService.cs` - Android impl
- [ ] `Platforms/iOS/Services/PermissionService.cs` - iOS impl

### Onboarding (5 pages)
- [ ] `Views/Onboarding/WelcomePage.xaml(.cs)` + ViewModel
- [ ] `Views/Onboarding/LicenseActivationPage.xaml(.cs)` + ViewModel
- [ ] `Views/Onboarding/CreateAccountPage.xaml(.cs)` + ViewModel
- [ ] `Views/Onboarding/BiometricSetupPage.xaml(.cs)` + ViewModel
- [ ] `Views/Onboarding/QuickTourPage.xaml(.cs)` + ViewModel

### WiFi Sync (2 files)
- [ ] `Services/Sync/WiFiSyncService.cs` - HTTP server mobile
- [ ] Desktop: `Services/WiFiSyncClient.vb` - HTTP client (in MoneyMind desktop)

### Database Migration (1 file)
- [ ] `Services/Database/DatabaseMigration.cs` - Schema versioning

### Logging (2 files)
- [ ] `Services/Telemetry/CrashReportingService.cs` - App Center/Firebase
- [ ] `Services/LoggingService.cs` - File logging

### Models (1 file)
- [ ] `Models/LicenseData.cs` - Beta license model

**TOTALE FASE 0**: 21 files
**TEMPO**: 3-4 giorni
**BLOCKERS**: Nessuno - priorità MASSIMA!

---

## ✅ Fase 1 - Core Setup (Step 1-9 QUICK_START.md)

### Models (3 files)
- [ ] `Models/Transaction.cs` - Entity transazione (converti da Transazione.vb)
- [ ] `Models/BankAccount.cs` - Entity conto (converti da ContoCorrente.vb)
- [ ] `Models/AccountStatistics.cs` - Stats dashboard

### Services (2 files)
- [ ] `Services/Database/DatabaseService.cs` - Personal DB manager (SQLite)
- [ ] `Services/Database/DatabasePathProvider.cs` - Cross-platform path logic

### ViewModels (1 file)
- [ ] `ViewModels/MainViewModel.cs` - Dashboard logic

### Views (1 file)
- [ ] `MainPage.xaml(.cs)` - Dashboard UI (sostituisci default)

### Converters (1 file)
- [ ] `Converters/InverseBoolConverter.cs` - Per toggle visibilità

### Configuration
- [ ] `MauiProgram.cs` - Registra services (modifica esistente)

**TOTALE**: 9 files (7 nuovi + 2 modifiche)

---

## 🚀 Fase 2 - Transazioni (Settimana 1)

### Models (1 file)
- [ ] `Models/TransactionFilter.cs` - Filtri ricerca

### ViewModels (2 files)
- [ ] `ViewModels/TransactionsViewModel.cs` - Lista + filtri
- [ ] `ViewModels/TransactionEditViewModel.cs` - Form add/edit

### Views (2 files)
- [ ] `Views/TransactionsPage.xaml(.cs)` - Lista transazioni
- [ ] `Views/TransactionEditPage.xaml(.cs)` - Modale edit

### DataAccess (2 files)
- [ ] `DataAccess/IRepository.cs` - Generic interface
- [ ] `DataAccess/TransactionRepository.cs` - Transaction CRUD

### Converters (2 files)
- [ ] `Converters/AmountToColorConverter.cs` - Verde/Rosso per +/-
- [ ] `Converters/DateToStringConverter.cs` - Format data italiano

**TOTALE**: 9 files

---

## 🏦 Fase 3 - Multi-Conto (Settimana 2)

### Models (1 file)
- [ ] `Models/BankAccount.cs` - (già creato Fase 1)

### Services (2 files)
- [ ] `Services/Business/AccountService.cs` - Gestione conti
- [ ] `Services/Database/GlobalDatabaseService.cs` - Global DB manager

### ViewModels (1 file)
- [ ] `ViewModels/AccountSelectionViewModel.cs` - Switch conti

### Views (1 file)
- [ ] `Views/AccountSelectionPage.xaml(.cs)` - Grid conti

### Helpers (1 file)
- [ ] `Helpers/PreferencesHelper.cs` - Salva conto selezionato

**TOTALE**: 6 files

---

## 💰 Fase 4 - Stipendi (Settimana 3)

### Models (2 files)
- [ ] `Models/SalaryConfiguration.cs` - Config stipendi
- [ ] `Models/SalaryException.cs` - Eccezioni mesi

### Services (1 file)
- [ ] `Services/Business/SalaryPeriodService.cs` - Calcolo periodi (port da GestoreStipendi.vb)

### ViewModels (1 file)
- [ ] `ViewModels/SalaryConfigViewModel.cs` - Config UI

### Views (1 file)
- [ ] `Views/SalaryConfigPage.xaml(.cs)` - Tab config + eccezioni

### Helpers (1 file)
- [ ] `Helpers/DateTimeHelper.cs` - Weekend logic

**TOTALE**: 6 files

---

## 🔍 Fase 5 - Duplicati (Settimana 4)

### Models (1 file)
- [ ] `Models/DuplicateGroup.cs` - Gruppo duplicati

### Services (1 file)
- [ ] `Services/Business/DuplicateDetectionService.cs` - Algoritmo detection

### ViewModels (1 file)
- [ ] `ViewModels/DuplicatesViewModel.cs` - UI duplicati

### Views (1 file)
- [ ] `Views/DuplicatesPage.xaml(.cs)` - Lista duplicati

### Helpers (1 file)
- [ ] `Helpers/LevenshteinDistance.cs` - String similarity

**TOTALE**: 5 files

---

## 📥 Fase 6 - Import (Settimana 5)

### Models (2 files)
- [ ] `Models/ImportConfiguration.cs` - Config salvate
- [ ] `Models/ColumnMapping.cs` - Mapping colonne

### Services (2 files)
- [ ] `Services/Business/ImportExportService.cs` - CSV/Excel I/O
- [ ] `Services/Business/ConfigurationService.cs` - Save/load configs

### ViewModels (1 file)
- [ ] `ViewModels/ImportViewModel.cs` - Wizard import

### Views (1 file)
- [ ] `Views/ImportPage.xaml(.cs)` - UI wizard

### Platform Services (3 files)
- [ ] `Platforms/Android/Services/FilePickerService.cs`
- [ ] `Platforms/iOS/Services/FilePickerService.cs`
- [ ] `Services/Platform/IFilePickerService.cs` - Interface

**TOTALE**: 9 files

---

## 📤 Fase 7 - Export (Settimana 6)

### ViewModels (1 file)
- [ ] `ViewModels/ExportViewModel.cs` - Export options

### Views (1 file)
- [ ] `Views/ExportPage.xaml(.cs)` - UI export

### Platform Services (3 files)
- [ ] `Platforms/Android/Services/ShareService.cs`
- [ ] `Platforms/iOS/Services/ShareService.cs`
- [ ] `Services/Platform/IShareService.cs` - Interface

### Helpers (1 file)
- [ ] `Helpers/CurrencyFormatter.cs` - Format € per export

**TOTALE**: 6 files (riusa ImportExportService da Fase 6)

---

## 📊 Fase 8 - Analytics (Settimana 7)

### Models (1 file)
- [ ] `Models/ChartDataPoint.cs` - Dati charts

### Services (1 file)
- [ ] `Services/Business/AnalyticsService.cs` - Stats mensili/annuali

### ViewModels (1 file)
- [ ] `ViewModels/AnalyticsViewModel.cs` - Charts logic

### Views (1 file)
- [ ] `Views/AnalyticsPage.xaml(.cs)` - LiveCharts UI

**TOTALE**: 4 files

---

## ⚙️ Fase 9 - Settings (Settimana 8)

### Models (1 file)
- [ ] `Models/LicenseData.cs` - License cache model

### Services (3 files)
- [ ] `Services/LicenseService.cs` - Beta license verification (port da BetaLicenseManager.vb)
- [ ] `Services/UpdateService.cs` - GitHub releases check
- [ ] `Services/LoggingService.cs` - File logging

### ViewModels (3 files)
- [ ] `ViewModels/SettingsViewModel.cs` - Settings UI
- [ ] `ViewModels/AdminViewModel.cs` - Admin panel
- [ ] `ViewModels/UpdatesViewModel.cs` - Updates list

### Views (3 files)
- [ ] `Views/SettingsPage.xaml(.cs)` - Settings form
- [ ] `Views/AdminPage.xaml(.cs)` - Admin tools
- [ ] `Views/UpdatesPage.xaml(.cs)` - Updates UI

### Helpers (1 file)
- [ ] `Helpers/DeviceFingerprintHelper.cs` - Device ID (port da DeviceFingerprint.vb)

**TOTALE**: 11 files

---

## 🎨 Fase 10 - UI/UX Polish (Settimana 9)

### Resources (8 files)
- [ ] `Resources/Styles/Colors.xaml` - Palette Material 3
- [ ] `Resources/Styles/Styles.xaml` - Global styles
- [ ] `Resources/Images/appicon.svg` - App icon
- [ ] `Resources/Images/splash.svg` - Splash screen
- [ ] `Resources/Fonts/Inter-Regular.ttf`
- [ ] `Resources/Fonts/Inter-Bold.ttf`
- [ ] `Resources/Fonts/MaterialIcons-Regular.ttf`
- [ ] `Resources/Raw/empty_transactions.svg` - Empty state illustration

### Behaviors (2 files)
- [ ] `Behaviors/NumericValidationBehavior.cs` - Solo numeri Entry
- [ ] `Behaviors/EmailValidationBehavior.cs` - Email validation

### Converters (2 files)
- [ ] `Converters/BoolToVisibilityConverter.cs`
- [ ] `Converters/AmountToStringConverter.cs` - € formatting

### Shell Navigation (1 file)
- [ ] `AppShell.xaml(.cs)` - Rewrite con menu flyout

**TOTALE**: 15 files

---

## 🧪 Fase 11 - Testing (Settimana 10)

### Unit Tests (6 files)
- [ ] `Tests/Services/StatisticsServiceTests.cs`
- [ ] `Tests/Services/SalaryPeriodServiceTests.cs`
- [ ] `Tests/Services/DuplicateDetectionServiceTests.cs`
- [ ] `Tests/ViewModels/MainViewModelTests.cs`
- [ ] `Tests/ViewModels/TransactionsViewModelTests.cs`
- [ ] `Tests/Helpers/LevenshteinDistanceTests.cs`

**TOTALE**: 6 files (nuovo progetto `MoneyMindApp.Tests`)

---

## 📦 Riepilogo Totale Files

| Fase | Descrizione | Files | Settimane |
|------|-------------|-------|-----------|
| 1 | Core Setup | 9 | 1 |
| 2 | Transazioni | 9 | 1 |
| 3 | Multi-Conto | 6 | 1 |
| 4 | Stipendi | 6 | 1 |
| 5 | Duplicati | 5 | 1 |
| 6 | Import | 9 | 1 |
| 7 | Export | 6 | 1 |
| 8 | Analytics | 4 | 1 |
| 9 | Settings | 11 | 1 |
| 10 | UI/UX | 15 | 1 |
| 11 | Testing | 6 | 1 |
| **TOTALE** | | **86 files** | **11 settimane** |

---

## 🗂️ Files Desktop da NON Portare

### ❌ Classificazione (Skip Completo)
- `Services/ClassificatoreTransazioni.vb`
- `Services/GptClassificatoreTransazioni.vb`
- `Services/PatternService.vb`
- `Services/PatternManagerService.vb`
- `Views/PatternManagerWindow.xaml.vb`
- `Views/SmartClassificationCenter.xaml.vb`
- `Views/ClassificazioneDialog.xaml.vb`

### ❌ Backup Transazionale (Troppo Complesso Mobile)
- `Services/BackupService.vb` (transaction-based)
- `Services/AutoBackupService.vb` (scheduling desktop)
- `Services/BackupTransactionManager.vb`
- `Services/BackupLogger.vb`
- `Views/BackupManagerWindow.xaml.vb`

### ❌ Budget/Obiettivi (Fase Futura)
- `Models/BudgetItem.vb`
- `Models/ObiettivoRisparmio.vb`
- `Views/BudgetWindow.xaml.vb`

### ❌ Dashboard Grafici Desktop (Diverso da Mobile)
- `Views/DashboardWindow.xaml.vb` (troppo complesso, riscrivere)
- `Views/LiveChartsWindow.xaml.vb`

---

## 📝 Template Files Utili

### Base ViewModel Template
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MoneyMindApp.ViewModels;

public partial class [NAME]ViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    public [NAME]ViewModel()
    {
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            // TODO: Load data
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Base ContentPage Template
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:MoneyMindApp.ViewModels"
             x:Class="MoneyMindApp.Views.[NAME]Page"
             x:DataType="vm:[NAME]ViewModel"
             Title="[TITLE]">

    <Grid Padding="16">
        <!-- Content here -->
    </Grid>

</ContentPage>
```

---

## 🎯 Priorità Implementazione

### Must-Have (MVP)
1. ✅ Dashboard Stats (Fase 1)
2. ✅ Lista Transazioni (Fase 2)
3. ✅ Multi-Conto (Fase 3)
4. ✅ Import CSV (Fase 6)

### Should-Have
5. ✅ Configura Stipendi (Fase 4)
6. ✅ Export Excel (Fase 7)
7. ✅ Analytics Base (Fase 8)

### Nice-to-Have
8. ⚠️ Rileva Duplicati (Fase 5)
9. ⚠️ Admin Panel (Fase 9)
10. ⚠️ Dark Theme (Fase 10)

---

## 📊 Progress Tracking

Crea file `PROGRESS.md` per tracciare:

```markdown
# Progress Log

## Week 1 (YYYY-MM-DD)
- [x] Setup progetto MAUI
- [x] DatabaseService base
- [x] MainViewModel + MainPage
- [ ] TransactionsViewModel
- [ ] ...

## Week 2 (YYYY-MM-DD)
- [ ] ...
```

---

**Tip**: Usa questo file come checklist durante implementazione. Spunta `[x]` ogni file completato!
