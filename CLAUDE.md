# CLAUDE.md - MoneyMindApp (Mobile)

> **⚠️ IMPORTANTE**: Leggi `STATO_ARTE.md` per lo stato corrente del progetto!
>
> Questo file contiene solo il **mapping Desktop→Mobile** e le **linee guida architetturali**.
>
> **Al termine di ogni sessione**: Aggiorna `STATO_ARTE.md` con i progressi.

---

## 🔴 REGOLE OBBLIGATORIE PER OGNI SESSIONE

### Prima di iniziare qualsiasi implementazione:

1. **LEGGI `STATO_ARTE.md`** - Stato corrente del progetto
2. **LEGGI `ROADMAP.md`** - Piano fasi da seguire IN ORDINE
3. **LEGGI `FILES_TO_CREATE.md`** - Checklist files da creare per fase
4. **VERIFICA la fase corrente** - Non saltare fasi!

### Durante l'implementazione:

5. **SEGUI `ROADMAP.md` ALLA LETTERA** - Implementa le fasi nell'ordine indicato
6. **USA `FILES_TO_CREATE.md`** come checklist - Spunta ogni file completato
7. **RISPETTA `UI_UX_GUIDELINES.md`** - Design system e colori
8. **APPLICA `SECURITY.md`** - Biometrico, encryption, permissions
9. **TESTA su emulatore** dopo ogni feature completata

### Al termine della sessione:

10. **AGGIORNA `STATO_ARTE.md`** con:
    - Files creati/modificati
    - Funzionalità implementate
    - TODO rimanenti
    - Errori riscontrati
11. **VERIFICA allineamento** tra STATO_ARTE e ROADMAP

### Ordine Fasi ROADMAP (da seguire RIGOROSAMENTE):

| Fase | Descrizione | Files | Status |
|------|-------------|-------|--------|
| 0 | Security & Critical Setup | 21 | ✅ |
| 1 | Core Setup (Dashboard) | 9 | ✅ |
| 2 | Transazioni CRUD | 9 | ✅ |
| 3 | Multi-Conto | 6 | ✅ |
| 4 | Stipendi | 6 | ✅ |
| 5 | Duplicati | 5 | ✅ |
| 6 | Import | 9 | ✅ |
| 7 | Export | 6 | ✅ |
| 8 | Analytics | 4 | ✅ |
| 9 | Settings | 11 | ✅ |
| 10 | UI/UX Polish | 6 | ✅ |
| 11 | Testing | 5 | ✅ |
| 12 | Deployment | - | ⏳ PROSSIMA |

### Guide di Riferimento:

| File | Contenuto | Quando Consultare |
|------|-----------|-------------------|
| `ROADMAP.md` | Piano fasi completo | SEMPRE prima di implementare |
| `FILES_TO_CREATE.md` | Checklist 86 files | Per sapere cosa creare |
| `UI_UX_GUIDELINES.md` | Design system | Per colori, font, layout |
| `SECURITY.md` | Sicurezza | Per biometric, crypto |
| `SYNC_STRATEGY.md` | WiFi Sync | Per sync desktop |
| `PERMISSIONS.md` | Android/iOS permissions | Per runtime permissions |
| `ONBOARDING.md` | UX primo avvio | Per flow onboarding |
| `TESTING_STRATEGY.md` | Unit/Integration tests | Per testing |
| `DEPLOYMENT.md` | Google Play/App Store | Per release |

---

## 📋 Quick Reference

- **Stato Progetto**: `STATO_ARTE.md` ← **LEGGI SEMPRE PER PRIMO!**
- **Roadmap Completa**: `ROADMAP.md` ← **SEGUI L'ORDINE FASI!**
- **Files da Creare**: `FILES_TO_CREATE.md` ← **CHECKLIST COMPLETA**
- **Quick Start**: `QUICK_START.md`
- **Security**: `SECURITY.md`
- **WiFi Sync**: `SYNC_STRATEGY.md`

---

## Architettura

**Stack**: .NET MAUI 8.0 + SQLite + MVVM + CommunityToolkit.Mvvm
**Linguaggio**: C# (portato da VB.NET desktop)
**Target**: Android 7.0+ (API 24), iOS 11+, Windows 10+

### Database - Compatibilità Desktop

**CRITICO**: Stessi database desktop per sincronizzazione seamless!

```csharp
// Path Cross-Platform
string appDataPath = DeviceInfo.Platform == DevicePlatform.WinUI
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyMind")
    : FileSystem.AppDataDirectory;
```

**Files Database**:
- `MoneyMind_Global.db` - Conti, Settings
- `MoneyMind_Conto_XXX.db` - Transazioni per conto

**NO CLASSIFICAZIONI**: Pattern/MacroCategoria/Categoria NON usati in mobile. Solo lettura transazioni grezze.

---

## Mapping Desktop → Mobile

### Path Sorgenti Desktop

**Desktop VB.NET Source**: `C:\Users\rober\Documents\MoneyMind\`

Usa questi file come riferimento quando implementi le features mobile:

---

### 📊 1. Dashboard → MainPage.xaml

**Desktop Source**: `Views/MainWindow.xaml.vb` (righe 1-500)

**Funzioni Chiave da Portare**:
```vb
' MainWindow.xaml.vb:UpdateAllStats()
Private Sub UpdateAllStats()
    Dim periodo = GestorePeriodi.GetPeriodoCorrente()
    Dim transazioni = DatabaseService.GetTransazioni(periodo.Inizio, periodo.Fine)

    Dim entrate = transazioni.Where(Function(t) t.Importo > 0).Sum(Function(t) t.Importo)
    Dim uscite = Math.Abs(transazioni.Where(Function(t) t.Importo < 0).Sum(Function(t) t.Importo))
    Dim risparmio = entrate - uscite

    ' Saldo Totale = Saldo Inizio + SUM(Importi)
    Dim saldoTotale = SaldoIniziale + transazioni.Sum(Function(t) t.Importo)
End Sub
```

**Mobile Conversion** (C#):
```csharp
// MainViewModel.cs
public async Task LoadStatisticsAsync()
{
    var period = _salaryPeriodService.GetCurrentPeriod();
    var transactions = await _transactionRepository.GetTransactionsAsync(period.Start, period.End);

    Statistics = new AccountStatistics
    {
        Income = transactions.Where(t => t.Importo > 0).Sum(t => t.Importo),
        Expenses = Math.Abs(transactions.Where(t => t.Importo < 0).Sum(t => t.Importo)),
        Savings = transactions.Sum(t => t.Importo),
        TotalBalance = InitialBalance + transactions.Sum(t => t.Importo),
        TransactionCount = transactions.Count
    };
}
```

---

### 📝 2. Elenco Transazioni → TransactionsPage.xaml

**Desktop Source**: `Views/MainWindow.xaml.vb` (righe 500-1200)

**Funzioni Chiave**:
- `CaricaTransazioni()` - Load lista
- `FiltroTransazioni()` - Filtri data/importo
- `CercaTransazione()` - Ricerca testuale
- `EliminaTransazione()` - Delete con conferma

**Mobile Pattern**:
```csharp
// TransactionsViewModel.cs
[ObservableProperty]
private ObservableCollection<Transaction> transactions;

[ObservableProperty]
private string searchText;

[RelayCommand]
private async Task LoadTransactionsAsync()
{
    var query = _transactionRepository.GetQuery();

    if (StartDate.HasValue)
        query = query.Where(t => t.Data >= StartDate.Value);

    if (!string.IsNullOrEmpty(SearchText))
        query = query.Where(t => t.Descrizione.Contains(SearchText) ||
                                  t.Causale.Contains(SearchText));

    var items = await query.OrderByDescending(t => t.Data).ToListAsync();
    Transactions = new ObservableCollection<Transaction>(items);
}
```

---

### 🏦 3. Cambia Conto → AccountSelectionPage.xaml

**Desktop Source**: `Views/ContoSelectionWindow.xaml.vb`

**Funzioni Chiave**:
- `CaricaConti()` - Load lista conti
- `CalcolaSaldoConto()` - Calcolo saldo = SaldoIniziale + SUM(Transazioni)
- `SalvaContoSelezionato()` - Persistenza

**Logica VB.NET**:
```vb
' Calcola saldo corrente
Dim transazioni = DatabaseService.GetTransazioniConto(conto.Id)
conto.SaldoCorrente = conto.SaldoIniziale + transazioni.Sum(Function(t) t.Importo)
```

---

### 💰 4. Configura Stipendi → SalaryConfigPage.xaml

**Desktop Source**: `Views/SalaryConfigWindow.xaml.vb` (1218 righe!)

**Servizi VB.NET da Portare**:
- `Services/GestoreStipendi.vb` → `SalaryPeriodService.cs`
  - `GetGiornoPagamento(mese, anno)` - Calcola giorno effettivo
  - `GestisciWeekend(data)` - Anticipa/Posticipa
  - `GetPeriodoStipendiale(data)` - Range periodo

**Logica Gestione Weekend**:
```vb
' Anticipa/Posticipa weekend
If dataStipendio.DayOfWeek = DayOfWeek.Saturday OrElse dataStipendio.DayOfWeek = DayOfWeek.Sunday Then
    Select Case gestioneWeekend
        Case "ANTICIPA"
            While dataStipendio.DayOfWeek = DayOfWeek.Saturday OrElse dataStipendio.DayOfWeek = DayOfWeek.Sunday
                dataStipendio = dataStipendio.AddDays(-1)
            End While
        Case "POSTICIPA"
            While dataStipendio.DayOfWeek = DayOfWeek.Saturday OrElse dataStipendio.DayOfWeek = DayOfWeek.Sunday
                dataStipendio = dataStipendio.AddDays(1)
            End While
    End Select
End If
```

---

### 🔍 5. Rileva Duplicati → DuplicatesPage.xaml

**Desktop Source**: `Views/DuplicateManagerWindow.xaml.vb`

**Algoritmo Detection**:
```vb
' Stessa data + Importo ± 0.01€ + Levenshtein > 80%
If t1.Data <> t2.Data Then Continue For
If Math.Abs(t1.Importo - t2.Importo) > 0.01 Then Continue For

Dim distance = CalcolaLevenshtein(t1.Descrizione, t2.Descrizione)
Dim similarity = 1 - (distance / Math.Max(t1.Descrizione.Length, t2.Descrizione.Length))

If similarity > 0.8 Then
    gruppi.Add(New DuplicateGroup With {.Transactions = {t1, t2}})
End If
```

---

### 📥 6. Importa → ImportPage.xaml

**Desktop Source**: `Services/ExportImportService.vb` (righe 1-800)

**Pattern Import CSV**:
```vb
Public Function ImportaCsv(filePath As String, mapping As ColumnMapping) As Integer
    Using reader As New StreamReader(filePath, Encoding.UTF8)
        Dim headers = reader.ReadLine().Split(";"c)

        While Not reader.EndOfStream
            Dim values = reader.ReadLine().Split(";"c)

            ' Mapping colonne
            Dim data = ParseData(values(mapping.ColonnaData), "dd/MM/yyyy")
            Dim importo = ParseImporto(values(mapping.ColonnaImporto))
            Dim descrizione = values(mapping.ColonnaDescrizione)

            ' Insert DB
            DatabaseService.InserisciTransazione(New Transazione With {
                .Data = data,
                .Importo = importo,
                .Descrizione = descrizione
            })
        End While
    End Using
End Function
```

---

### 📤 7. Esporta → ExportPage.xaml

**Desktop Source**: `Services/ExportImportService.vb` (righe 800-1500)

**Pattern Export Excel** (EPPlus):
```vb
Using package As New ExcelPackage()
    Dim worksheet = package.Workbook.Worksheets.Add("Transazioni")

    ' Header con formattazione
    worksheet.Cells("A1:C1").Style.Font.Bold = True

    ' Data rows con formato valuta
    worksheet.Cells($"C{row}").Style.Numberformat.Format = "€#,##0.00"

    ' Auto-fit columns
    worksheet.Cells.AutoFitColumns()

    package.SaveAs(New FileInfo(outputPath))
End Using
```

---

### 📊 8. Analisi Anno → AnalyticsPage.xaml

**Desktop Source**: `Services/AnalyticsService.vb`

**Pattern Statistiche Mensili**:
```vb
Public Function GetStatisticheMensili(anno As Integer) As List(Of MonthStats)
    For mese = 1 To 12
        Dim dataInizio = New DateTime(anno, mese, 1)
        Dim dataFine = dataInizio.AddMonths(1).AddDays(-1)

        Dim transazioni = DatabaseService.GetTransazioni(dataInizio, dataFine)

        stats.Add(New MonthStats With {
            .Mese = mese,
            .Entrate = transazioni.Where(Function(t) t.Importo > 0).Sum(Function(t) t.Importo),
            .Uscite = Math.Abs(transazioni.Where(Function(t) t.Importo < 0).Sum(Function(t) t.Importo)),
            .Risparmio = transazioni.Sum(Function(t) t.Importo)
        })
    Next
End Function
```

**Mobile Chart Setup** (LiveChartsCore):
```csharp
// AnalyticsViewModel.cs
public ISeries[] Series { get; set; }

Series = new ISeries[]
{
    new ColumnSeries<decimal>
    {
        Name = "Entrate",
        Values = monthlyStats.Select(m => m.Income).ToArray(),
        Fill = new SolidColorPaint(SKColors.Green)
    },
    new ColumnSeries<decimal>
    {
        Name = "Uscite",
        Values = monthlyStats.Select(m => m.Expenses).ToArray(),
        Fill = new SolidColorPaint(SKColors.Red)
    }
};
```

---

### ⚙️ 9. Impostazioni → SettingsPage.xaml

**Desktop Source**: `Services/BetaLicenseManager.vb`

**License Check Pattern**:
```vb
' SettingsWindow.xaml.vb
Dim license = BetaLicenseManager.GetLicenseData()
TxtEmail.Text = license.Email
TxtSubscription.Text = license.Subscription
TxtExpires.Text = license.ExpiresAt.ToString("dd/MM/yyyy")
```

**Mobile**: Usa `HttpClient` per chiamare Google Sheets API (stesso backend desktop)

---

### 🔄 10. Aggiornamenti → UpdatesPage.xaml

**Desktop Source**: `Services/UpdateService.vb`

**GitHub API Check**:
```vb
Public Async Function CheckForUpdates() As Task(Of UpdateInfo)
    Dim client As New HttpClient()
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MoneyMind")

    Dim response = Await client.GetStringAsync("https://api.github.com/repos/username/moneymind/releases/latest")
    Dim json = JsonConvert.DeserializeObject(Of GitHubRelease)(response)

    Dim latestVersion = Version.Parse(json.tag_name.TrimStart("v"c))
    Dim currentVersion = Version.Parse(VersionManager.CURRENT_VERSION)

    Return New UpdateInfo With {
        .IsUpdateAvailable = latestVersion > currentVersion,
        .LatestVersion = latestVersion.ToString(),
        .ReleaseNotes = json.body,
        .DownloadUrl = json.assets(0).browser_download_url
    }
End Function
```

---

## Pattern MVVM Standard

```csharp
// ViewModel pattern con CommunityToolkit.Mvvm
[ObservableProperty]
private decimal totalBalance;

[ObservableProperty]
private ObservableCollection<Transaction> transactions;

[RelayCommand]
private async Task LoadDataAsync()
{
    // Async data loading
}

[RelayCommand]
private async Task SaveTransactionAsync()
{
    // Save with validation
}
```

---

## Calcolo Statistiche (CRITICO)

**NO CLASSIFICAZIONI in mobile!**

```csharp
// Calcolo saldo: SaldoIniziale + SUM(Importi)
public async Task<decimal> GetTotalBalanceAsync(decimal saldoIniziale)
{
    var transactions = await GetAllTransactionsAsync();
    return saldoIniziale + transactions.Sum(t => t.Importo);
}

// Statistiche periodo
public async Task<AccountStatistics> CalculateStatsAsync(DateTime start, DateTime end)
{
    var transactions = await GetTransactionsAsync(start, end);

    return new AccountStatistics
    {
        TotalBalance = InitialBalance + transactions.Sum(t => t.Importo),
        Income = transactions.Where(t => t.Importo > 0).Sum(t => t.Importo),
        Expenses = Math.Abs(transactions.Where(t => t.Importo < 0).Sum(t => t.Importo)),
        Savings = transactions.Sum(t => t.Importo),
        TransactionCount = transactions.Count
    };
}
```

---

## Beta License Mobile

**STESSO backend Google Sheets** (Desktop + Mobile):

- **Fingerprint mobile**: `DeviceInfo.Model + DeviceInfo.Manufacturer + DeviceInfo.Name`
- **Cache locale**: `Preferences.Set("license", json)`
- **Grace period**: 7 giorni offline
- **API**: `?action=checkStatus` ogni avvio

---

## Conversione VB.NET → C#

**Pattern comuni**:
- `Dim x As Integer` → `int x`
- `Function(...) As String` → `string MethodName(...)`
- `If...Then...End If` → `if (...) { }`
- `For Each x In list` → `foreach (var x in list)`
- `AddHandler btn.Click, AddressOf Handler` → `btn.Clicked += Handler`

**Tool online**: Telerik Code Converter (https://converter.telerik.com/)

---

## Gotchas Mobile

1. **SQLite Path**: Diverso per piattaforma → `FileSystem.AppDataDirectory`
2. **Async Everywhere**: UI freeze se usi sync methods
3. **ObservableCollection**: Per binding ListView (auto-update UI)
4. **Shell Lifecycle**: `OnAppearing/OnDisappearing` per refresh data
5. **File Picker**: Permissions Android (`READ_EXTERNAL_STORAGE`)
6. **Charts Performance**: Throttle update con `CancellationToken`
7. **Saldo Punto 0**: Sempre da `SaldoIniziale + SUM`, ignora `MacroCategoria`

---

## 🔄 Workflow Sessione Claude

### 1. Inizio Sessione
```
"Leggi STATO_ARTE.md per vedere stato attuale.
Voglio continuare implementando [FEATURE X] dalla FASE [N].
Usa CLAUDE.md come riferimento per mapping Desktop→Mobile."
```

### 2. Fine Sessione
```
"Aggiorna STATO_ARTE.md con tutto ciò che hai fatto in questa sessione.
Includi:
- Files creati/modificati
- Funzionalità implementate
- TODO rimanenti
- Eventuali errori riscontrati
"
```

---

## 📚 Documentazione Completa

**Per approfondimenti specifici**:

1. **STATO_ARTE.md** (5 min) - ⚠️ **LEGGI SEMPRE PER PRIMO!**
2. **ROADMAP.md** (20 min) - Piano 11 settimane completo
3. **QUICK_START.md** (30 min) - MVP in 9 step
4. **SECURITY.md** (20 min) - Biometric, permissions, encryption
5. **SYNC_STRATEGY.md** (20 min) - WiFi sync + hotspot
6. **PERMISSIONS.md** (15 min) - Runtime permissions
7. **ONBOARDING.md** (15 min) - UX primo avvio
8. **UI_UX_GUIDELINES.md** (20 min) - Design system
9. **TESTING_STRATEGY.md** (15 min) - Unit/Integration/UI tests
10. **DEPLOYMENT.md** (20 min) - Google Play/App Store

---

**Fine CLAUDE.md - Usa come reference rapido per mapping Desktop→Mobile**

**Per lo stato attuale del progetto**: Leggi `STATO_ARTE.md` ← **SEMPRE!**
