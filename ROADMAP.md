# ROADMAP - MoneyMindApp Mobile

## ⚠️ FASE 0: Security & Critical Setup (Settimana 0 - 3-4 giorni)

**CRITICO**: Implementare PRIMA di iniziare Fase 1!

### Step 0.1: Security Services
- [ ] `Services/Security/BiometricAuthService.cs` - Face/Touch ID
- [ ] `Services/Security/EncryptionService.cs` - AES encryption helper
- [ ] `Services/Platform/IPermissionService.cs` - Interface permissions
- [ ] `Platforms/Android/Services/PermissionService.cs` - Android impl
- [ ] `Platforms/iOS/Services/PermissionService.cs` - iOS impl

### Step 0.2: Database Migration
- [ ] `Services/Database/DatabaseMigration.cs` - Schema versioning
- [ ] Version check logic + auto-migrate

### Step 0.3: Onboarding Flow
- [ ] `Views/Onboarding/WelcomePage.xaml` - Welcome screen
- [ ] `Views/Onboarding/LicenseActivationPage.xaml` - Beta key activation
- [ ] `Views/Onboarding/CreateAccountPage.xaml` - First account setup
- [ ] `Views/Onboarding/BiometricSetupPage.xaml` - Enable Face ID
- [ ] `Views/Onboarding/QuickTourPage.xaml` - Optional carousel tour

### Step 0.4: WiFi Sync Foundation
- [ ] `Services/Sync/WiFiSyncService.cs` - HTTP server mobile
- [ ] Desktop: `WiFiSyncClient.vb` - HTTP client desktop
- [ ] Test hotspot mobile → computer connection

### Step 0.5: Crash Reporting & Logging
- [ ] `Services/Telemetry/CrashReportingService.cs` - App Center/Firebase setup
- [ ] `Services/LoggingService.cs` - File logging

### Step 0.6: Documentation Review
- [ ] Leggi SECURITY.md (sicurezza implementazione)
- [ ] Leggi SYNC_STRATEGY.md (WiFi sync + file export)
- [ ] Leggi PERMISSIONS.md (runtime permissions)
- [ ] Leggi ONBOARDING.md (UX primo avvio)
- [ ] Leggi PRIVACY_POLICY.md (da personalizzare e pubblicare)

**Tempo Stimato**: 3-4 giorni
**Blockers**: Nessuno - inizia subito!

---

## FASE 1: Setup & Core (Settimana 1-2)

### Step 1.1: Crea Progetto MAUI
```bash
cd "C:\Users\rober\Documents\MoneyMindApp"
dotnet new maui -n MoneyMindApp -f net8.0
```

### Step 1.2: Aggiungi NuGet Packages
```bash
dotnet add package sqlite-net-pcl
dotnet add package SQLitePCLRaw.bundle_green
dotnet add package CommunityToolkit.Mvvm
dotnet add package CommunityToolkit.Maui
```

### Step 1.3: Converti Models (VB → C#)
- [ ] `Models/Transaction.cs` (da Transazione.vb)
- [ ] `Models/BankAccount.cs` (da ContoCorrente.vb)
- [ ] `Models/AccountStatistics.cs` (da ContoStatistica.vb)
- [ ] `Models/SalaryConfiguration.cs`
- [ ] `Models/SalaryException.cs`

### Step 1.4: DatabaseService
- [ ] `Services/Database/DatabaseService.cs` - Personal DB manager
- [ ] `Services/Database/GlobalDatabaseService.cs` - Global DB manager
- [ ] `Services/Database/DatabaseInitializer.cs` - Schema creation
- [ ] Cross-platform path logic (Android/iOS/Windows)

### Step 1.5: DI Container Setup
- [ ] `MauiProgram.cs` - Registra services + ViewModels
- [ ] Test connection SQLite su Android Emulator

---

## FASE 2: Dashboard (Settimana 3)

### Step 2.1: MainPage UI
- [ ] `Views/MainPage.xaml` - Layout dashboard
- [ ] Header: Logo + ContoCorrente dropdown
- [ ] Stats cards: Saldo/Entrate/Uscite/Risparmio/NrTransazioni
- [ ] Icona occhio per toggle visibilità valori
- [ ] Bottom nav: Dashboard/Transazioni/Importa/Impostazioni

### Step 2.2: MainViewModel
- [ ] `ViewModels/MainViewModel.cs`
- [ ] `[ObservableProperty] AccountStatistics stats`
- [ ] `[RelayCommand] LoadStatistics()` - Calcolo senza classificazioni
- [ ] `[RelayCommand] ToggleValuesVisibility()`
- [ ] `[RelayCommand] RefreshData()`

### Step 2.3: AccountService
- [ ] `Services/AccountService.cs`
- [ ] `GetCurrentAccountAsync()` - Da Preferences
- [ ] `SwitchAccountAsync(int accountId)`
- [ ] `GetAccountsAsync()` - Lista conti

### Step 2.4: Statistiche Logic
- [ ] Calcolo Saldo Totale: `InitialBalance + SUM(Importi)`
- [ ] Entrate: `SUM(Importo WHERE > 0)`
- [ ] Uscite: `ABS(SUM(Importo WHERE < 0))`
- [ ] Risparmio: `Entrate - Uscite`
- [ ] Periodo mese stipendiale (da SalaryPeriodService)

---

## FASE 3: Transazioni (Settimana 4)

### Step 3.1: TransactionsPage UI
- [ ] `Views/TransactionsPage.xaml`
- [ ] CollectionView virtualized
- [ ] Item template: Data | Descrizione | Importo (+ colore)
- [ ] SwipeView: Elimina (left), Modifica (right)
- [ ] SearchBar + filtri

### Step 3.2: TransactionsViewModel
- [ ] `ViewModels/TransactionsViewModel.cs`
- [ ] `ObservableCollection<Transaction> Transactions`
- [ ] `LoadTransactionsAsync(DateTime start, DateTime end)`
- [ ] `SearchCommand(string query)`
- [ ] `DeleteTransactionCommand(int id)`

### Step 3.3: Add/Edit Transaction
- [ ] `Views/TransactionEditPage.xaml` - Form modale
- [ ] DatePicker, Entry (Importo, Descrizione)
- [ ] Save/Cancel buttons
- [ ] Validazione input

---

## FASE 4: Conti & Stipendi (Settimana 5)

### Step 4.1: ContoSelectionPage
- [ ] `Views/ContoSelectionPage.xaml`
- [ ] Grid cards conti (icona + nome + saldo)
- [ ] Tap event → Switch conto
- [ ] Button "Aggiungi Conto"

### Step 4.2: SalaryConfigPage
- [ ] `Views/SalaryConfigPage.xaml`
- [ ] Slider giorno pagamento (1-31)
- [ ] Picker gestione weekend
- [ ] TabView: Base + Eccezioni
- [ ] ListView eccezioni mesi (add/remove)
- [ ] Anteprima calendario

### Step 4.3: SalaryPeriodService
- [ ] `Services/SalaryPeriodService.cs` (port da GestoreStipendi.vb)
- [ ] `GetCurrentPeriod()` → (start, end)
- [ ] `GetPaymentDate(int month, int year)` + weekend logic
- [ ] `GetExceptions()` da DB

---

## FASE 5: Import/Export (Settimana 6)

### Step 5.1: ImportPage
- [ ] `Views/ImportPage.xaml`
- [ ] File picker (CSV/Excel)
- [ ] Mapping colonne: ComboBox (Data, Importo, Descrizione)
- [ ] Preview DataGrid (prime 5 righe)
- [ ] Button "Importa" → insert DB
- [ ] Salva configurazione import (per riutilizzo)

### Step 5.2: ExportPage
- [ ] `Views/ExportPage.xaml`
- [ ] RadioButton formati: CSV, Excel, PDF
- [ ] DatePicker range (inizio, fine)
- [ ] Picker conto (o "Tutti")
- [ ] Button "Esporta" → genera file + Share API

### Step 5.3: ImportExportService
- [ ] `Services/ImportExportService.cs`
- [ ] `ImportCsvAsync(string path, ColumnMapping mapping)`
- [ ] `ExportExcelAsync(List<Transaction> data, string outputPath)`
- [ ] `ExportCsvAsync(...)`
- [ ] ClosedXML per Excel (platform-specific)

---

## FASE 6: Duplicati & Analisi (Settimana 7)

### Step 6.1: DuplicatesPage
- [ ] `Views/DuplicatesPage.xaml`
- [ ] Button "Rileva Duplicati"
- [ ] ListView gruppi duplicati (checkbox seleziona)
- [ ] Action: Mantieni primo, elimina altri
- [ ] Algoritmo: Data + Importo ± 0.01 + Levenshtein(Descrizione) > 0.8

### Step 6.2: AnalyticsPage
- [ ] `Views/AnalyticsPage.xaml`
- [ ] LiveCharts: CartesianChart
- [ ] Grafico barre: Entrate/Uscite per mese
- [ ] Grafico linea: Risparmio trend
- [ ] Slider confronto 2 anni
- [ ] Tab: Mese corrente, Anno, Confronto

### Step 6.3: AnalyticsService
- [ ] `Services/AnalyticsService.cs`
- [ ] `GetMonthlyStatsAsync(int year)` → 12 punti dati
- [ ] `GetYearComparisonAsync(int year1, int year2)`
- [ ] Cache risultati (CacheService)

---

## FASE 7: Settings & Admin (Settimana 8)

### Step 7.1: SettingsPage
- [ ] `Views/SettingsPage.xaml`
- [ ] Beta License status (email, subscription, expires)
- [ ] Picker tema: Light/Dark/System
- [ ] Entry simbolo valuta
- [ ] Switch backup automatico cloud (futuro)
- [ ] Button "Logout" (revoke license cache)

### Step 7.2: AdminPage
- [ ] `Views/AdminPage.xaml` (visibile solo se IsAdmin)
- [ ] ListView log eventi (ultimi 100)
- [ ] Cards: Nr transazioni, Nr conti, DB size
- [ ] Button "Force Sync"
- [ ] Button "Export Logs"

### Step 7.3: LicenseService
- [ ] `Services/LicenseService.cs` (port da BetaLicenseManager.vb)
- [ ] `VerifyLicenseAsync()` → API Google Sheets
- [ ] `CheckStatusAsync()` → revoke check
- [ ] `CacheLicense(LicenseData data)` → Preferences
- [ ] Grace period 7 giorni
- [ ] Fingerprint: DeviceInfo.Model + Manufacturer + Name

### Step 7.4: UpdatesPage
- [ ] `Views/UpdatesPage.xaml`
- [ ] Check GitHub releases (UpdateService)
- [ ] ListView changelog
- [ ] Button "Scarica Aggiornamento"
- [ ] Platform-specific install (Android: APK intent, iOS: App Store link)

---

## FASE 8: Polish & Deploy (Settimana 9-10)

### Step 8.1: Icons & Splash
- [ ] `Resources/Images/appicon.svg` - Icon 512x512
- [ ] `Resources/Images/splash.svg` - Splash screen
- [ ] `Resources/Styles/Colors.xaml` - Light/Dark theme
- [ ] `Resources/Fonts/*.ttf` - Custom fonts (se necessari)

### Step 8.2: Permissions Android
- [ ] `Platforms/Android/AndroidManifest.xml`
- [ ] `READ_EXTERNAL_STORAGE` (import file)
- [ ] `WRITE_EXTERNAL_STORAGE` (export file)
- [ ] `INTERNET` (API calls)
- [ ] Runtime permissions request (Android 6+)

### Step 8.3: Testing
- [ ] Android Emulator (Pixel 5, API 30)
- [ ] Device reale Android (test performance)
- [ ] iOS Simulator (se hai Mac)
- [ ] Windows desktop (F5 debug)
- [ ] Test scenari: 1000+ transazioni, switch conti, offline mode

### Step 8.4: Build Release
```bash
# Android APK
dotnet publish -f net8.0-android -c Release

# Android AAB (Google Play)
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab

# Windows MSIX
dotnet publish -f net8.0-windows10.0.19041.0 -c Release -p:GenerateAppxPackageOnBuild=true
```

### Step 8.5: Deploy Beta
- [ ] Upload APK/AAB su Google Play Internal Testing
- [ ] Genera link beta testers
- [ ] Aggiungi beta testers su Google Sheets (stesso sistema desktop)
- [ ] Monitor crash reports (Firebase/App Center)

---

## FASE 9: Cloud Sync (Opzionale - Futuro)

### Step 9.1: Sync Service
- [ ] `Services/Sync/CloudSyncService.cs`
- [ ] Upload DB delta su cloud (Google Drive API)
- [ ] Download modifiche remote
- [ ] Conflict resolution (last-write-wins)
- [ ] Background sync (WorkManager Android, BackgroundTasks iOS)

### Step 9.2: Sync UI
- [ ] Indicator sync status (header icon)
- [ ] Manual sync button
- [ ] Conflict resolution dialog

---

## Checklist Conversione Desktop → Mobile

### Da Copiare & Convertire
- [x] Database schema (stesso identico)
- [ ] Models: 5 classes VB → C#
- [ ] Services: 8 services VB → C# (skip classification logic)
- [ ] ViewModels: Creare nuovi (desktop usa code-behind)
- [ ] Views: Rewrite XAML (WPF → MAUI syntax)

### Da Skippare (Non Mobile)
- ❌ ClassificatoreTransazioni.vb - NO pattern matching
- ❌ GptClassificatoreTransazioni.vb - NO AI classification
- ❌ PatternService.vb - NO pattern management
- ❌ PatternManagerWindow - NO pattern editor
- ❌ SmartClassificationCenter - NO bulk classification
- ❌ Colonne: MacroCategoria, Categoria, Necessita, Frequenza, Stagionalita

### Da Semplificare
- ⚠️ BackupService: Solo export/import, no transazioni complesse
- ⚠️ ExportImportService: Solo CSV/Excel base
- ⚠️ UpdateService: Simplified per mobile store guidelines

---

## Comandi Utili

### Run Android Emulator
```bash
# Lista emulatori
emulator -list-avds

# Avvia emulator
emulator -avd Pixel_5_API_30

# Debug su emulator
dotnet build -f net8.0-android && dotnet run -f net8.0-android
```

### Run Windows
```bash
dotnet run -f net8.0-windows10.0.19041.0
```

### Converti VB → C# (Online Tool)
https://www.codeconvert.ai/vb-to-csharp-converter

### SQLite Viewer (Debug DB)
https://sqlitebrowser.org/
- Apri: `adb pull /data/data/com.moneymind.app/files/MoneyMind_Global.db`
