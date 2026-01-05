# Testing Strategy - MoneyMindApp

## 🎯 Testing Pyramid

```
           ╱╲
          ╱  ╲         E2E Tests (5%)
         ╱────╲        UI/Integration Tests (15%)
        ╱──────╲       Unit Tests (80%)
       ╱────────╲
```

---

## 🧪 1. Unit Tests (80% Coverage Target)

### Framework
- **xUnit** + **Moq** (mocking) + **FluentAssertions**

### Test Project Structure
```
MoneyMindApp.Tests/
├── Services/
│   ├── StatisticsServiceTests.cs
│   ├── SalaryPeriodServiceTests.cs
│   ├── DuplicateDetectionServiceTests.cs
│   ├── WiFiSyncServiceTests.cs
│   └── ImportExportServiceTests.cs
├── ViewModels/
│   ├── MainViewModelTests.cs
│   ├── TransactionsViewModelTests.cs
│   └── ImportViewModelTests.cs
├── Helpers/
│   ├── LevenshteinDistanceTests.cs
│   ├── CurrencyFormatterTests.cs
│   └── DateTimeHelperTests.cs
└── Models/
    └── TransactionTests.cs
```

### Priority Test Cases

**StatisticsService**:
```csharp
[Fact]
public async Task CalculateStats_WithMixedTransactions_ReturnsCorrectTotals()
{
    // Arrange
    var transactions = new List<Transaction>
    {
        new() { Importo = 1000m, Data = DateTime.Now },  // Entrata
        new() { Importo = -200m, Data = DateTime.Now },  // Uscita
        new() { Importo = -50m, Data = DateTime.Now }
    };

    // Act
    var stats = _service.CalculateStats(transactions, initialBalance: 500m);

    // Assert
    stats.Income.Should().Be(1000m);
    stats.Expenses.Should().Be(250m);
    stats.Savings.Should().Be(750m);
    stats.TotalBalance.Should().Be(1250m); // 500 + 750
}
```

**SalaryPeriodService**:
```csharp
[Theory]
[InlineData(2025, 1, 15, "2024-12-15", "2025-01-14")] // Gennaio
[InlineData(2025, 2, 15, "2025-01-15", "2025-02-14")] // Febbraio
public void GetPeriod_WithPaymentDay15_ReturnsCorrectRange(
    int year, int month, int day, string expectedStart, string expectedEnd)
{
    // Arrange
    var config = new SalaryConfiguration { PaymentDay = day };

    // Act
    var period = _service.GetPeriod(year, month, config);

    // Assert
    period.Start.Should().Be(DateTime.Parse(expectedStart));
    period.End.Should().Be(DateTime.Parse(expectedEnd));
}

[Fact]
public void GetPaymentDate_WithSaturdayAndAnticipa_ReturnsNearestFriday()
{
    // Sabato 15 Gennaio 2022 → Venerdì 14
    var result = _service.GetPaymentDate(2022, 1, 15, WeekendHandling.Anticipa);

    result.DayOfWeek.Should().Be(DayOfWeek.Friday);
    result.Day.Should().Be(14);
}
```

**DuplicateDetectionService**:
```csharp
[Fact]
public void DetectDuplicates_WithSimilarTransactions_ReturnsDuplicateGroups()
{
    var transactions = new List<Transaction>
    {
        new() { Data = new DateTime(2025,1,15), Importo = -45.20m, Descrizione = "Spesa Esselunga" },
        new() { Data = new DateTime(2025,1,15), Importo = -45.21m, Descrizione = "Spesa Esselunga Via Roma" },
        new() { Data = new DateTime(2025,1,16), Importo = -50.00m, Descrizione = "Benzina" }
    };

    var groups = _service.DetectDuplicates(transactions);

    groups.Should().HaveCount(1);
    groups[0].Transactions.Should().HaveCount(2);
}
```

**Run Tests**:
```bash
dotnet test --logger "console;verbosity=detailed"
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🖥️ 2. Integration Tests (15%)

### Database Tests

```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly DatabaseService _db;
    private readonly string _testDbPath;

    public DatabaseIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _db = new DatabaseService(_testDbPath);
        _db.InitializeAsync().Wait();
    }

    [Fact]
    public async Task SaveAndRetrieve_Transaction_Success()
    {
        // Arrange
        var transaction = new Transaction
        {
            Data = DateTime.Now,
            Importo = 100m,
            Descrizione = "Test"
        };

        // Act
        await _db.SaveAsync(transaction);
        var retrieved = await _db.GetByIdAsync(transaction.ID);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Importo.Should().Be(100m);
    }

    public void Dispose()
    {
        _db?.CloseAsync().Wait();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
```

### WiFi Sync Integration Test

```csharp
[Fact]
public async Task WiFiSync_BidirectionalSync_Success()
{
    // Arrange
    var serverService = new WiFiSyncService(_mockDb.Object, _mockLogger.Object);
    await serverService.StartServerAsync();

    var clientService = new WiFiSyncClient();
    var serverIP = serverService.LocalIPAddress;

    // Act
    var result = await clientService.SyncBidirectionalAsync(serverIP);

    // Assert
    result.Success.Should().BeTrue();
    await serverService.StopServerAsync();
}
```

---

## 📱 3. UI Tests (5% - Manual + Automated)

### Manual Test Matrix

| Scenario | Device | OS | Expected | Pass/Fail |
|----------|--------|----|-----------| ---------|
| Dashboard Load | Pixel 5 | Android 13 | Stats cards show correct values | ✅ |
| Add Transaction | iPhone 12 | iOS 16 | Transaction saved + appears in list | ⬜ |
| Import CSV | Galaxy S21 | Android 12 | 100 transactions imported | ⬜ |
| WiFi Sync | Hotspot | Android | Desktop ↔ Mobile sync | ⬜ |
| Biometric Auth | Face ID | iOS 15 | App unlocks with face | ⬜ |
| Dark Theme | All | All | UI readable, correct colors | ⬜ |
| Rotate Screen | All | All | Layout adapts, data persists | ⬜ |
| Offline Mode | Airplane | All | App usable, sync queued | ⬜ |

### Appium (Opzionale - Automated UI Tests)

```csharp
[Test]
public void AddTransaction_ValidData_AppearsInList()
{
    // Given I'm on the Dashboard
    var dashboardPage = new DashboardPage(driver);
    dashboardPage.Should().BeDisplayed();

    // When I tap FAB and add transaction
    dashboardPage.TapAddButton();
    var editPage = new TransactionEditPage(driver);
    editPage.EnterAmount("45.20");
    editPage.EnterDescription("Test Transaction");
    editPage.TapSave();

    // Then transaction appears in list
    var transactionsPage = new TransactionsPage(driver);
    transactionsPage.TransactionList.Should().Contain("Test Transaction");
}
```

---

## 🚀 4. Performance Tests

### Benchmark: Load 10k Transactions

```csharp
[Benchmark]
public async Task LoadTransactions_10k()
{
    var transactions = await _db.GetTransactionsAsync();
    // Target: < 500ms
}

[Benchmark]
public void CalculateStats_10k()
{
    var stats = _statsService.CalculateStats(_transactions);
    // Target: < 100ms
}
```

**Run**:
```bash
dotnet run -c Release --project MoneyMindApp.Benchmarks
```

**Target Metrics**:
- Cold start: < 2s
- Dashboard load: < 300ms
- Transaction list scroll: 60fps (16ms/frame)
- Import 1000 CSV rows: < 5s
- WiFi Sync 5000 transactions: < 10s

---

## 🔒 5. Security Tests

### Penetration Testing Checklist

- [ ] SQL Injection: Test input fields (Description, Causale)
- [ ] XSS: Test export HTML/PDF with malicious input
- [ ] Path Traversal: Test import file picker
- [ ] Biometric Bypass: Test force-quit + reopen
- [ ] Network Sniffing: Test WiFi Sync packets (Wireshark)
- [ ] APK Decompilation: Check for hardcoded secrets
- [ ] Root Detection: Test on rooted device
- [ ] Certificate Pinning: Test MITM attack on API calls

### Tools
- **OWASP ZAP**: API security testing
- **Frida**: Runtime instrumentation (Android)
- **MobSF**: Static analysis APK
- **Wireshark**: Network traffic analysis

---

## 🧪 6. Compatibility Testing

### Device Matrix (Minimum)

**Android**:
- Pixel 5 (Android 13)
- Galaxy S21 (Android 12)
- Xiaomi Mi 11 (MIUI 13)
- OnePlus 9 (OxygenOS)

**iOS** (se disponibile Mac):
- iPhone 12 (iOS 16)
- iPhone 13 Pro (iOS 17)
- iPad Air (iPadOS 16)

**Screen Sizes**:
- Small (< 5.5"): Compact UI
- Medium (5.5-6.5"): Standard
- Large (> 6.5"): Utilize space
- Tablet (> 7"): Master-detail

---

## 📊 7. Accessibility Tests

### WCAG 2.1 AA Compliance

- [ ] **Color Contrast**: 4.5:1 for text, 3:1 for UI elements
- [ ] **Touch Targets**: Min 48x48dp
- [ ] **Screen Reader**: TalkBack (Android) / VoiceOver (iOS) navigation
- [ ] **Focus Indicators**: Visible borders on focused elements
- [ ] **Text Scaling**: Readable at 200% system font size

**Tools**:
- Android Accessibility Scanner
- iOS Accessibility Inspector
- Contrast Checker: https://webaim.org/resources/contrastchecker/

---

## 🐛 8. Bug Triage & Severity

### Priority Matrix

| Severity | Description | Examples | SLA |
|----------|-------------|----------|-----|
| **P0 - Critical** | App crash, data loss | SQLite corruption, sync data loss | Fix in 24h |
| **P1 - High** | Feature broken | Import fails, stats wrong | Fix in 3 days |
| **P2 - Medium** | Degraded UX | Slow load, layout glitch | Fix in 1 week |
| **P3 - Low** | Minor annoyance | Typo, icon misaligned | Fix in 2 weeks |
| **P4 - Trivial** | Cosmetic | Color slightly off | Backlog |

---

## 🔄 9. Regression Testing

### Pre-Release Checklist

**Core Flows** (test ogni release):
- [ ] First launch onboarding
- [ ] Create account + first transaction
- [ ] Import CSV (100 rows)
- [ ] Export Excel
- [ ] WiFi Sync Desktop ↔ Mobile
- [ ] Biometric auth lock/unlock
- [ ] Dark theme toggle
- [ ] Permission requests (Storage, Camera)
- [ ] Beta license validation
- [ ] Update check

**Automation**: CI/CD pipeline runs unit tests su ogni commit.

---

## 🚢 10. Beta Testing Program

### Phases

**Alpha** (Internal, 2-3 testers):
- Developer + close friends
- Test core features
- Collect crash logs
- Duration: 2 weeks

**Closed Beta** (10-20 testers):
- Invite-only via TestFlight/Google Play Internal Testing
- Test full feature set
- Feedback surveys
- Duration: 4 weeks

**Open Beta** (100+ testers):
- Public opt-in
- Final stress test
- Polish UI/UX based on feedback
- Duration: 2 weeks

**Production** (Release):
- Gradual rollout: 10% → 50% → 100%
- Monitor crash rate (target: < 0.5%)

### Feedback Collection

**In-App**:
- Settings → "Send Feedback" (email intent)
- Shake to report bug (screenshot + logs)

**External**:
- Google Forms survey
- Discord/Telegram beta community
- GitHub Issues

---

## 📋 Testing Checklist (Pre-Release)

- [ ] All unit tests pass (> 80% coverage)
- [ ] Integration tests pass
- [ ] Manual test matrix completed
- [ ] Performance benchmarks met
- [ ] Security audit passed
- [ ] Accessibility audit passed
- [ ] 3 devices tested (Android + iOS)
- [ ] Dark theme tested
- [ ] Offline mode tested
- [ ] Beta testers feedback addressed
- [ ] Crash-free rate > 99.5%

---

## 🛠️ CI/CD Pipeline

```yaml
# .github/workflows/test.yml
name: Test & Build
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Run Tests
        run: dotnet test --logger trx --collect:"XPlat Code Coverage"
      - name: Upload Coverage
        uses: codecov/codecov-action@v3

  build-android:
    needs: test
    runs-on: windows-latest
    steps:
      - name: Build APK
        run: dotnet publish -f net8.0-android -c Release
      - name: Upload APK Artifact
        uses: actions/upload-artifact@v3
        with:
          name: MoneyMindApp-Android
          path: bin/Release/net8.0-android/publish/*.apk
```

---

**Ultima Review**: 2025-01-XX
