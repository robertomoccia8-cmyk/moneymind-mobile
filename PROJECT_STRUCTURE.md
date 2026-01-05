# Struttura Progetto - MoneyMindApp

## Directory Tree Completa
```
MoneyMindApp/
│
├── 📁 Models/                          # Domain Entities
│   ├── Transaction.cs                  # Entity transazione
│   ├── BankAccount.cs                  # Entity conto corrente
│   ├── AccountStatistics.cs            # Stats aggregate
│   ├── SalaryConfiguration.cs          # Config stipendi
│   ├── SalaryException.cs              # Eccezioni mesi
│   ├── ImportConfiguration.cs          # Config import salvate
│   ├── LicenseData.cs                  # Beta license cache
│   └── ChartDataPoint.cs               # Dati per grafici
│
├── 📁 ViewModels/                      # MVVM Logic
│   ├── BaseViewModel.cs                # Base class (INotifyPropertyChanged)
│   ├── MainViewModel.cs                # Dashboard stats
│   ├── TransactionsViewModel.cs        # Lista + filtri
│   ├── TransactionEditViewModel.cs     # Add/Edit form
│   ├── AccountSelectionViewModel.cs    # Switch conti
│   ├── SalaryConfigViewModel.cs        # Config stipendi
│   ├── ImportViewModel.cs              # Import wizard
│   ├── ExportViewModel.cs              # Export + share
│   ├── DuplicatesViewModel.cs          # Duplicate detection
│   ├── AnalyticsViewModel.cs           # Charts data
│   ├── SettingsViewModel.cs            # Settings + license
│   ├── AdminViewModel.cs               # Admin panel
│   └── UpdatesViewModel.cs             # Updates checker
│
├── 📁 Views/                           # XAML UI
│   ├── MainPage.xaml(.cs)              # Dashboard
│   ├── TransactionsPage.xaml           # Lista transazioni
│   ├── TransactionEditPage.xaml        # Form modale
│   ├── AccountSelectionPage.xaml       # Grid conti
│   ├── SalaryConfigPage.xaml           # Tab config stipendi
│   ├── ImportPage.xaml                 # Import wizard
│   ├── ExportPage.xaml                 # Export options
│   ├── DuplicatesPage.xaml             # Duplicate manager
│   ├── AnalyticsPage.xaml              # Charts dashboard
│   ├── SettingsPage.xaml               # Settings form
│   ├── AdminPage.xaml                  # Admin tools
│   └── UpdatesPage.xaml                # Updates list
│
├── 📁 Services/                        # Business Logic
│   │
│   ├── 📁 Database/
│   │   ├── DatabaseService.cs          # Personal DB manager (SQLite)
│   │   ├── GlobalDatabaseService.cs    # Global DB manager
│   │   ├── DatabaseInitializer.cs      # Schema creation/migration
│   │   └── DatabasePathProvider.cs     # Cross-platform path logic
│   │
│   ├── 📁 Repositories/                # Data Access Layer
│   │   ├── ITransactionRepository.cs
│   │   ├── TransactionRepository.cs
│   │   ├── IAccountRepository.cs
│   │   └── AccountRepository.cs
│   │
│   ├── 📁 Business/
│   │   ├── AccountService.cs           # Gestione conti
│   │   ├── StatisticsService.cs        # Calcolo stats (no classification)
│   │   ├── SalaryPeriodService.cs      # Periodi stipendiali
│   │   ├── DuplicateDetectionService.cs # Algoritmo duplicati
│   │   ├── ImportExportService.cs      # CSV/Excel I/O
│   │   └── BackupService.cs            # Backup/restore
│   │
│   ├── 📁 Platform/
│   │   ├── IFilePickerService.cs       # Interface picker
│   │   ├── IShareService.cs            # Interface share
│   │   ├── IPermissionService.cs       # Runtime permissions
│   │   └── Implementations per platform (Platforms/)
│   │
│   ├── 📁 Sync/
│   │   ├── CloudSyncService.cs         # (Futuro) Google Drive sync
│   │   └── ConflictResolver.cs
│   │
│   ├── LicenseService.cs               # Beta license verification
│   ├── UpdateService.cs                # GitHub releases checker
│   ├── CacheService.cs                 # In-memory cache
│   └── LoggingService.cs               # File logging
│
├── 📁 DataAccess/                      # Repository Pattern Base
│   ├── IRepository.cs                  # Generic repo interface
│   └── BaseRepository.cs               # Base CRUD operations
│
├── 📁 Converters/                      # XAML Value Converters
│   ├── BoolToVisibilityConverter.cs
│   ├── AmountToColorConverter.cs       # Verde/Rosso per +/-
│   ├── DateToStringConverter.cs
│   ├── AmountToStringConverter.cs      # Formatting €
│   └── InverseBoolConverter.cs
│
├── 📁 Behaviors/                       # XAML Behaviors
│   ├── NumericValidationBehavior.cs    # Solo numeri Entry
│   ├── EventToCommandBehavior.cs
│   └── EmailValidationBehavior.cs
│
├── 📁 Resources/                       # Assets
│   │
│   ├── 📁 Images/
│   │   ├── appicon.svg                 # App icon (512x512)
│   │   ├── splash.svg                  # Splash screen
│   │   ├── logo.png
│   │   └── Icons/                      # UI icons (Material Design)
│   │       ├── home.svg
│   │       ├── list.svg
│   │       ├── settings.svg
│   │       ├── import.svg
│   │       ├── export.svg
│   │       └── ...
│   │
│   ├── 📁 Fonts/
│   │   ├── Inter-Regular.ttf           # Modern font
│   │   ├── Inter-Bold.ttf
│   │   └── MaterialIcons-Regular.ttf   # Icons font
│   │
│   ├── 📁 Styles/
│   │   ├── Colors.xaml                 # Palette colori
│   │   └── Styles.xaml                 # Global styles
│   │
│   └── 📁 Raw/
│       └── database_schema.sql         # Schema reference
│
├── 📁 Platforms/                       # Platform-Specific Code
│   │
│   ├── 📁 Android/
│   │   ├── MainActivity.cs
│   │   ├── MainApplication.cs
│   │   ├── AndroidManifest.xml         # Permissions
│   │   ├── Resources/
│   │   │   ├── values/
│   │   │   │   └── colors.xml
│   │   │   └── drawable/
│   │   └── Services/                   # Android implementations
│   │       ├── FilePickerService.cs
│   │       ├── ShareService.cs
│   │       └── PermissionService.cs
│   │
│   ├── 📁 iOS/
│   │   ├── AppDelegate.cs
│   │   ├── Info.plist
│   │   ├── Entitlements.plist
│   │   └── Services/                   # iOS implementations
│   │       ├── FilePickerService.cs
│   │       ├── ShareService.cs
│   │       └── PermissionService.cs
│   │
│   └── 📁 Windows/
│       ├── App.xaml(.cs)
│       ├── Package.appxmanifest
│       └── Services/                   # Windows implementations
│           └── ...
│
├── 📁 Helpers/                         # Utility Classes
│   ├── Constants.cs                    # App constants
│   ├── DateTimeHelper.cs               # Date utils
│   ├── CurrencyFormatter.cs            # Formatting €
│   ├── LevenshteinDistance.cs          # String similarity
│   ├── ValidationHelper.cs             # Input validation
│   └── DeviceFingerprintHelper.cs      # Device ID generation
│
├── 📁 Extensions/                      # Extension Methods
│   ├── StringExtensions.cs
│   ├── DateTimeExtensions.cs
│   └── CollectionExtensions.cs
│
├── 📁 Exceptions/                      # Custom Exceptions
│   ├── DatabaseException.cs
│   ├── LicenseException.cs
│   └── ImportException.cs
│
├── AppShell.xaml(.cs)                  # Shell Navigation
├── App.xaml(.cs)                       # App lifecycle
├── MauiProgram.cs                      # DI Container + Config
├── MoneyMindApp.csproj                 # Project file
│
├── CLAUDE.md                           # Istruzioni Claude
├── ROADMAP.md                          # Plan implementazione
├── PROJECT_STRUCTURE.md                # Questo file
├── README.md                           # Setup instructions
└── .gitignore

```

## Architettura Layers

```
┌─────────────────────────────────────┐
│          Views (XAML)               │  ← UI Layer
├─────────────────────────────────────┤
│         ViewModels (MVVM)           │  ← Presentation Logic
├─────────────────────────────────────┤
│      Services (Business Logic)      │  ← Business Layer
├─────────────────────────────────────┤
│    Repositories (Data Access)       │  ← Data Layer
├─────────────────────────────────────┤
│      Models (Domain Entities)       │  ← Domain Layer
└─────────────────────────────────────┘
```

## Dependency Injection Flow

```csharp
MauiProgram.cs
│
├── Services
│   ├── Singleton: DatabaseService
│   ├── Singleton: GlobalDatabaseService
│   ├── Singleton: AccountService
│   ├── Singleton: LicenseService
│   ├── Singleton: CacheService
│   ├── Transient: StatisticsService
│   └── Transient: ImportExportService
│
├── Repositories
│   ├── Scoped: TransactionRepository
│   └── Scoped: AccountRepository
│
├── ViewModels
│   ├── Transient: MainViewModel
│   ├── Transient: TransactionsViewModel
│   └── ...
│
└── Views
    ├── Transient: MainPage
    ├── Transient: TransactionsPage
    └── ...
```

## Navigation Flow

```
AppShell (Root)
│
├── FlyoutItem: Dashboard
│   └── MainPage
│
├── FlyoutItem: Transazioni
│   ├── TransactionsPage
│   └── [Modal] TransactionEditPage
│
├── FlyoutItem: Conti
│   └── AccountSelectionPage
│
├── FlyoutItem: Strumenti
│   ├── ImportPage
│   ├── ExportPage
│   ├── DuplicatesPage
│   └── SalaryConfigPage
│
├── FlyoutItem: Analisi
│   └── AnalyticsPage
│
├── FlyoutItem: Impostazioni
│   ├── SettingsPage
│   ├── UpdatesPage
│   └── [Admin Only] AdminPage
│
└── [OnAppStart] → LicenseCheck
    ├── [Invalid] → BetaActivationPage (WIP)
    └── [Valid] → AccountSelection → MainPage
```

## Data Flow Example (Load Statistics)

```
User tap "Dashboard"
    ↓
MainPage.OnAppearing()
    ↓
MainViewModel.LoadStatisticsCommand.Execute()
    ↓
StatisticsService.CalculateStatsAsync()
    ↓
TransactionRepository.GetTransactionsAsync(start, end)
    ↓
DatabaseService._connection.Table<Transaction>().Where(...)
    ↓
SQLite Query → Personal DB (MoneyMind_Conto_XXX.db)
    ↓
Return List<Transaction>
    ↓
Calculate: TotalBalance, Income, Expenses, Savings
    ↓
MainViewModel.Statistics = stats (INotifyPropertyChanged)
    ↓
UI auto-update via data binding
```

## Security & Best Practices

### 1. **Secure Storage**
```csharp
// License cache (encrypted)
await SecureStorage.SetAsync("license_token", encryptedJson);

// API keys (encrypted)
await SecureStorage.SetAsync("openai_api_key", key);
```

### 2. **Database Encryption** (Optional - SQLCipher)
```csharp
var options = new SQLiteConnectionString(dbPath,
    storeDateTimeAsTicks: true,
    key: deviceKey); // Encryption key
```

### 3. **API Calls (HttpClient)**
```csharp
public class LicenseService
{
    private readonly HttpClient _httpClient;

    public LicenseService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("LicenseApi");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<LicenseData> VerifyAsync(string betaKey)
    {
        var response = await _httpClient.GetAsync($"...?action=verify&key={betaKey}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LicenseData>();
    }
}
```

### 4. **Input Validation**
```csharp
// ViewModel validation
public string Importo
{
    get => _importo;
    set
    {
        if (!decimal.TryParse(value, out var amount))
            throw new ValidationException("Importo non valido");

        if (amount == 0)
            throw new ValidationException("Importo deve essere diverso da zero");

        SetProperty(ref _importo, value);
    }
}
```

### 5. **Error Handling**
```csharp
[RelayCommand]
private async Task LoadTransactionsAsync()
{
    try
    {
        IsBusy = true;
        Transactions = await _transactionRepository.GetAllAsync();
    }
    catch (DatabaseException ex)
    {
        await Shell.Current.DisplayAlert("Errore Database", ex.Message, "OK");
        _loggingService.LogError(ex);
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", "Si è verificato un errore imprevisto", "OK");
        _loggingService.LogError(ex);
    }
    finally
    {
        IsBusy = false;
    }
}
```

## Performance Optimizations

### 1. **ListView Virtualization**
```xml
<CollectionView ItemsSource="{Binding Transactions}"
                RemainingItemsThreshold="10"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <!-- Lightweight item template -->
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

### 2. **Async Loading with Pagination**
```csharp
private const int PageSize = 50;
private int _currentPage = 0;

[RelayCommand]
private async Task LoadMoreAsync()
{
    var newItems = await _repository.GetPagedAsync(_currentPage * PageSize, PageSize);
    foreach (var item in newItems)
        Transactions.Add(item);

    _currentPage++;
}
```

### 3. **Caching**
```csharp
public class CacheService
{
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        if (!_cache.TryGetValue(key, out T value))
        {
            value = await factory();
            _cache.Set(key, value, expiration);
        }
        return value;
    }
}
```

### 4. **Database Indexing**
```csharp
// DatabaseInitializer.cs
await connection.ExecuteAsync(@"
    CREATE INDEX IF NOT EXISTS idx_transazioni_data
    ON Transazioni(Data DESC);

    CREATE INDEX IF NOT EXISTS idx_transazioni_importo
    ON Transazioni(Importo);
");
```

## Modern UI Design Guidelines

### Color Palette (Material Design 3)
```xml
<!-- Light Theme -->
<Color x:Key="Primary">#6750A4</Color>          <!-- Purple -->
<Color x:Key="Secondary">#625B71</Color>
<Color x:Key="Tertiary">#7D5260</Color>
<Color x:Key="Surface">#FFFBFE</Color>
<Color x:Key="Background">#FFFBFE</Color>
<Color x:Key="Error">#BA1A1A</Color>
<Color x:Key="Success">#2E7D32</Color>          <!-- Green -->

<!-- Dark Theme -->
<Color x:Key="PrimaryDark">#D0BCFF</Color>
<Color x:Key="SurfaceDark">#1C1B1F</Color>
<Color x:Key="BackgroundDark">#1C1B1F</Color>
```

### Typography (Inter Font)
```xml
<Style x:Key="HeadlineLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterBold" />
    <Setter Property="FontSize" Value="32" />
    <Setter Property="LineHeight" Value="40" />
</Style>

<Style x:Key="BodyMedium" TargetType="Label">
    <Setter Property="FontFamily" Value="InterRegular" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineHeight" Value="20" />
</Style>
```

### Animations
```xml
<!-- Fade in animation -->
<VisualStateGroup x:Name="CommonStates">
    <VisualState x:Name="Normal" />
    <VisualState x:Name="Selected">
        <VisualState.Setters>
            <Setter Property="BackgroundColor" Value="{StaticResource Primary}" />
            <Setter Property="Opacity" Value="0.8" />
        </VisualState.Setters>
    </VisualState>
</VisualStateGroup>
```

## Testing Strategy

### Unit Tests (xUnit)
```
MoneyMindApp.Tests/
├── Services/
│   ├── StatisticsServiceTests.cs
│   ├── SalaryPeriodServiceTests.cs
│   └── DuplicateDetectionServiceTests.cs
├── ViewModels/
│   ├── MainViewModelTests.cs
│   └── TransactionsViewModelTests.cs
└── Helpers/
    └── LevenshteinDistanceTests.cs
```

### UI Tests (Appium - Opzionale)
```
MoneyMindApp.UITests/
├── DashboardTests.cs
├── TransactionsTests.cs
└── ImportTests.cs
```

## Build & CI/CD

### GitHub Actions Workflow
```yaml
# .github/workflows/build.yml
name: Build & Test
on: [push, pull_request]
jobs:
  build-android:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Build APK
        run: dotnet publish -f net8.0-android -c Release
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: MoneyMindApp-Android
          path: bin/Release/net8.0-android/publish/*.apk
```

## Deployment Checklist

### Android (Google Play)
- [ ] Firma APK/AAB con keystore
- [ ] Incrementa versionCode in AndroidManifest
- [ ] Screenshot 5" + 7" + 10" (min 2 per size)
- [ ] Privacy Policy URL
- [ ] Feature graphic 1024x500
- [ ] Upload su Internal Testing → Alpha → Beta → Production

### iOS (App Store - Se disponibile Mac)
- [ ] Provisioning Profile + Certificate
- [ ] Incrementa CFBundleVersion in Info.plist
- [ ] Screenshot iPhone + iPad (varie dimensioni)
- [ ] App Store Connect: Descrizione, Keywords, Screenshot
- [ ] Submit for Review

### Windows (Microsoft Store - Opzionale)
- [ ] MSIX package signing
- [ ] Incrementa version in Package.appxmanifest
- [ ] Partner Center submission
