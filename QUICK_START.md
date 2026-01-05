# Quick Start - Primi Passi

## 🎯 Obiettivo
Creare app mobile **MoneyMindApp** (Android/iOS/Windows) usando .NET MAUI, portando le funzionalità essenziali dell'app desktop MoneyMind.

## 📋 Checklist Pre-Requisiti

- [x] Visual Studio 2022 installato
- [x] Android Studio installato (SDK configurato)
- [ ] .NET 8.0 SDK installato
- [ ] Workload MAUI installato in VS2022
- [ ] Android Emulator configurato (Pixel 5 API 30+)

## 🚀 Step 1: Setup Ambiente

### A. Verifica .NET SDK
```bash
dotnet --version
# Output atteso: 8.0.x o superiore
```

Se non presente: https://dotnet.microsoft.com/download/dotnet/8.0

### B. Installa Workload MAUI
```bash
dotnet workload install maui
```

### C. Verifica Android SDK
```bash
echo %ANDROID_HOME%
# Output: C:\Users\rober\AppData\Local\Android\Sdk (o simile)

# Se vuoto, configura:
set ANDROID_HOME=C:\Users\rober\AppData\Local\Android\Sdk
set PATH=%PATH%;%ANDROID_HOME%\emulator;%ANDROID_HOME%\platform-tools
```

### D. Avvia Emulator
```bash
# Lista emulatori disponibili
emulator -list-avds

# Avvia Pixel 5 API 30 (o quello disponibile)
emulator -avd Pixel_5_API_30
```

---

## 🏗️ Step 2: Crea Progetto Base

### A. Genera Progetto MAUI
```bash
cd "C:\Users\rober\Documents\MoneyMindApp"
dotnet new maui -n MoneyMindApp -f net8.0
```

**Output atteso**:
```
MoneyMindApp/
├── MauiProgram.cs
├── App.xaml(.cs)
├── AppShell.xaml(.cs)
├── MainPage.xaml(.cs)
├── Platforms/
└── MoneyMindApp.csproj
```

### B. Installa Pacchetti NuGet
```bash
cd MoneyMindApp

# SQLite
dotnet add package sqlite-net-pcl --version 1.9.172
dotnet add package SQLitePCLRaw.bundle_green --version 2.1.8

# MVVM Toolkit
dotnet add package CommunityToolkit.Mvvm --version 8.4.0
dotnet add package CommunityToolkit.Maui --version 9.0.0

# Charts
dotnet add package LiveChartsCore.SkiaSharpView.Maui --version 2.0.0-rc3.3

# Excel Export
dotnet add package ClosedXML --version 0.104.1
```

### C. Test Build Iniziale
```bash
# Build
dotnet build -f net8.0-android

# Run su emulator
dotnet run -f net8.0-android
```

**Atteso**: App "Hello World" si apre su emulator Android.

---

## 📝 Step 3: Crea Struttura Cartelle

```bash
# Dalla root MoneyMindApp/
mkdir Models ViewModels Views Services DataAccess Converters Behaviors Helpers Extensions

# Services sub-folders
cd Services
mkdir Database Repositories Business Platform Sync
cd ..

# Resources sub-folders (se non esistono già)
cd Resources
mkdir Images Fonts Styles
cd ..
```

---

## 🗄️ Step 4: Converti Primo Model (Transaction)

### A. Copia da Desktop
Apri `C:\Users\rober\Documents\MoneyMind\Models\Transazione.vb`

### B. Converti VB → C#
Usa tool online: https://www.codeconvert.ai/vb-to-csharp-converter

**Input VB.NET**:
```vb
Public Class Transazione
    Public Property ID As Integer
    Public Property Data As Date
    Public Property Importo As Decimal
    Public Property Descrizione As String
    Public Property Causale As String
    Public Property MacroCategoria As String
    Public Property Categoria As String
    Public Property DataInserimento As DateTime
    Public Property DataModifica As DateTime
End Class
```

**Output C#**:
```csharp
public class Transaction
{
    public int ID { get; set; }
    public DateTime Data { get; set; }
    public decimal Importo { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public string Causale { get; set; } = string.Empty;

    // NO MacroCategoria/Categoria per mobile (classificazione skippata)

    public DateTime DataInserimento { get; set; } = DateTime.Now;
    public DateTime DataModifica { get; set; } = DateTime.Now;
}
```

### C. Salva File
Crea `Models/Transaction.cs` con il codice convertito.

---

## 💾 Step 5: Crea DatabaseService Base

### A. Crea File
`Services/Database/DatabaseService.cs`

```csharp
using SQLite;

namespace MoneyMindApp.Services.Database;

public class DatabaseService
{
    private SQLiteAsyncConnection? _connection;
    private readonly string _dbPath;

    public DatabaseService()
    {
        // Path cross-platform
        _dbPath = DeviceInfo.Platform == DevicePlatform.WinUI
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyMind", "MoneyMind_Conto_001.db")
            : Path.Combine(FileSystem.AppDataDirectory, "MoneyMind_Conto_001.db");
    }

    public async Task InitializeAsync()
    {
        if (_connection != null)
            return;

        // Crea directory se non esiste
        var directory = Path.GetDirectoryName(_dbPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        _connection = new SQLiteAsyncConnection(_dbPath);

        // Crea tabelle
        await _connection.CreateTableAsync<Transaction>();
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await InitializeAsync();
        return await _connection!.Table<Transaction>().ToListAsync();
    }

    public async Task<int> SaveTransactionAsync(Transaction transaction)
    {
        await InitializeAsync();

        if (transaction.ID != 0)
            return await _connection!.UpdateAsync(transaction);
        else
            return await _connection!.InsertAsync(transaction);
    }

    public async Task<int> DeleteTransactionAsync(Transaction transaction)
    {
        await InitializeAsync();
        return await _connection!.DeleteAsync(transaction);
    }
}
```

### B. Registra in DI Container
Modifica `MauiProgram.cs`:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Registra services
        builder.Services.AddSingleton<DatabaseService>();

        return builder.Build();
    }
}
```

---

## 🎨 Step 6: Crea MainViewModel (Dashboard)

### A. Crea Model per Stats
`Models/AccountStatistics.cs`

```csharp
namespace MoneyMindApp.Models;

public class AccountStatistics
{
    public decimal TotalBalance { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Savings { get; set; }
    public int TransactionCount { get; set; }
}
```

### B. Crea ViewModel
`ViewModels/MainViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;

namespace MoneyMindApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty]
    private AccountStatistics statistics = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool areValuesVisible = true;

    public MainViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        try
        {
            IsLoading = true;

            // Carica transazioni mese corrente (semplificato)
            var transactions = await _databaseService.GetTransactionsAsync();
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthTransactions = transactions.Where(t => t.Data >= startOfMonth).ToList();

            // Calcola stats
            Statistics = new AccountStatistics
            {
                Income = monthTransactions.Where(t => t.Importo > 0).Sum(t => t.Importo),
                Expenses = Math.Abs(monthTransactions.Where(t => t.Importo < 0).Sum(t => t.Importo)),
                Savings = monthTransactions.Sum(t => t.Importo),
                TotalBalance = 0 + monthTransactions.Sum(t => t.Importo), // TODO: SaldoIniziale
                TransactionCount = monthTransactions.Count
            };
        }
        catch (Exception ex)
        {
            // TODO: Logging
            await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleValuesVisibility()
    {
        AreValuesVisible = !AreValuesVisible;
    }
}
```

### C. Registra ViewModel
`MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<MainViewModel>();
builder.Services.AddSingleton<MainPage>();
```

---

## 🖼️ Step 7: Crea MainPage UI (Dashboard Base)

Sostituisci `MainPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:MoneyMindApp.ViewModels"
             x:Class="MoneyMindApp.MainPage"
             x:DataType="vm:MainViewModel"
             Title="Dashboard">

    <Grid RowDefinitions="Auto,*" Padding="16">

        <!-- Header -->
        <HorizontalStackLayout Grid.Row="0" Spacing="8" Margin="0,0,0,16">
            <Label Text="💰 MoneyMind" FontSize="24" FontAttributes="Bold" VerticalOptions="Center" />
            <Button Text="👁️" Command="{Binding ToggleValuesVisibilityCommand}"
                    WidthRequest="40" HeightRequest="40" />
        </HorizontalStackLayout>

        <!-- Stats Grid -->
        <Grid Grid.Row="1" RowDefinitions="Auto,Auto" ColumnDefinitions="*,*"
              RowSpacing="16" ColumnSpacing="16">

            <!-- Saldo Totale -->
            <Frame Grid.Row="0" Grid.Column="0" BorderColor="LightGray" HasShadow="True"
                   Padding="16" CornerRadius="12">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Saldo Totale" FontSize="14" TextColor="Gray" />
                    <Label Text="{Binding Statistics.TotalBalance, StringFormat='€{0:N2}'}"
                           FontSize="20" FontAttributes="Bold"
                           IsVisible="{Binding AreValuesVisible}" />
                    <Label Text="****" FontSize="20" FontAttributes="Bold"
                           IsVisible="{Binding AreValuesVisible, Converter={StaticResource InverseBoolConverter}}" />
                </VerticalStackLayout>
            </Frame>

            <!-- Entrate -->
            <Frame Grid.Row="0" Grid.Column="1" BorderColor="LightGray" HasShadow="True"
                   Padding="16" CornerRadius="12" BackgroundColor="#E8F5E9">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Entrate" FontSize="14" TextColor="Gray" />
                    <Label Text="{Binding Statistics.Income, StringFormat='€{0:N2}'}"
                           FontSize="20" FontAttributes="Bold" TextColor="Green"
                           IsVisible="{Binding AreValuesVisible}" />
                </VerticalStackLayout>
            </Frame>

            <!-- Uscite -->
            <Frame Grid.Row="1" Grid.Column="0" BorderColor="LightGray" HasShadow="True"
                   Padding="16" CornerRadius="12" BackgroundColor="#FFEBEE">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Uscite" FontSize="14" TextColor="Gray" />
                    <Label Text="{Binding Statistics.Expenses, StringFormat='€{0:N2}'}"
                           FontSize="20" FontAttributes="Bold" TextColor="Red"
                           IsVisible="{Binding AreValuesVisible}" />
                </VerticalStackLayout>
            </Frame>

            <!-- Risparmio -->
            <Frame Grid.Row="1" Grid.Column="1" BorderColor="LightGray" HasShadow="True"
                   Padding="16" CornerRadius="12">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Risparmio" FontSize="14" TextColor="Gray" />
                    <Label Text="{Binding Statistics.Savings, StringFormat='€{0:N2}'}"
                           FontSize="20" FontAttributes="Bold"
                           IsVisible="{Binding AreValuesVisible}" />
                </VerticalStackLayout>
            </Frame>

        </Grid>

        <!-- Loading Indicator -->
        <ActivityIndicator Grid.Row="1" IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}"
                           HorizontalOptions="Center" VerticalOptions="Center" />

    </Grid>

</ContentPage>
```

Code-behind `MainPage.xaml.cs`:

```csharp
using MoneyMindApp.ViewModels;

namespace MoneyMindApp;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadStatisticsCommand.ExecuteAsync(null);
    }
}
```

---

## ▶️ Step 8: Test Prima Esecuzione

### A. Build & Run
```bash
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

### B. Verifica
- [ ] App si apre su emulator
- [ ] Dashboard mostra 4 card (Saldo, Entrate, Uscite, Risparmio)
- [ ] Valori mostrano €0.00 (normale, DB vuoto)
- [ ] Tap occhio 👁️ nasconde/mostra valori

### C. Debug DB Path
Aggiungi log temporaneo in `DatabaseService.InitializeAsync()`:

```csharp
System.Diagnostics.Debug.WriteLine($"DB Path: {_dbPath}");
```

Verifica output in Visual Studio Output window.

---

## 📊 Step 9: Test con Dati Desktop (Opzionale)

### A. Copia Database Desktop
```bash
# Trova DB desktop
dir "C:\Users\rober\AppData\Roaming\MoneyMind\MoneyMind_Conto_*.db"

# Copia su emulator Android
adb push "C:\Users\rober\AppData\Roaming\MoneyMind\MoneyMind_Conto_001.db" /data/data/com.moneymind.app/files/
```

### B. Riavvia App
Ora dashboard dovrebbe mostrare stats reali dal DB desktop!

---

## 🎉 Risultato Finale Step 1-9

✅ **App funzionante** con:
- Dashboard con 4 stats cards
- Calcolo stats da DB SQLite
- Toggle visibilità valori
- Database compatibile desktop

---

## 📚 Prossimi Step (Fase 2)

Segui **ROADMAP.md** per implementare:
1. TransactionsPage (lista transazioni)
2. ContoSelectionPage (switch conti)
3. Import/Export
4. Charts Analytics
5. Impostazioni

---

## 🆘 Troubleshooting

### Errore: "Android SDK not found"
```bash
# Configura ambiente
set ANDROID_HOME=C:\Users\rober\AppData\Local\Android\Sdk
set PATH=%PATH%;%ANDROID_HOME%\emulator;%ANDROID_HOME%\platform-tools
```

### Errore: "sqlite3.dll not found" (Windows)
```bash
dotnet add package SQLitePCLRaw.bundle_green
```

### Emulator non si avvia
```bash
# Ricrea emulator
avdmanager create avd -n Pixel5 -k "system-images;android-30;google_apis;x86_64" -d pixel_5
```

### App crasha all'avvio
- Verifica `MauiProgram.cs` ha tutti i services registrati
- Check Output window per stack trace
- Aggiungi try-catch in `MainViewModel.LoadStatisticsAsync()`

---

## 📞 Comandi Utili

```bash
# Build
dotnet build -f net8.0-android

# Clean
dotnet clean

# Run
dotnet run -f net8.0-android

# List devices
adb devices

# View logs
adb logcat | findstr MoneyMind

# Uninstall app
adb uninstall com.moneymind.app

# Pull DB from device
adb pull /data/data/com.moneymind.app/files/MoneyMind_Conto_001.db C:\Temp\
```

---

**Next**: Dopo aver completato Step 1-9, apri `ROADMAP.md` Fase 2 per continuare sviluppo.
