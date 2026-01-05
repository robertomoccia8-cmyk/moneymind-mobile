# Security Implementation - MoneyMindApp

## 🔐 Overview Sicurezza

App gestisce **dati finanziari sensibili** → Sicurezza è PRIORITÀ MASSIMA.

---

## 🎯 Security Requirements

### 1. **Biometric Authentication** ⚠️ OBBLIGATORIO

**Obiettivo**: Bloccare accesso app con Face ID/Touch ID/PIN.

#### Implementazione

**File**: `Services/Security/BiometricAuthService.cs`

```csharp
using Microsoft.Maui.Authentication;

namespace MoneyMindApp.Services.Security;

public interface IBiometricAuthService
{
    Task<bool> IsAvailableAsync();
    Task<bool> AuthenticateAsync(string reason);
    Task<BiometricType> GetBiometricTypeAsync();
}

public enum BiometricType
{
    None,
    Fingerprint,
    Face,
    Iris
}

public class BiometricAuthService : IBiometricAuthService
{
    public async Task<bool> IsAvailableAsync()
    {
        return await DeviceSecurityMonitor.IsAvailableAsync();
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            var result = await DeviceSecurityMonitor.AuthenticateAsync(
                new AuthenticationRequest
                {
                    Title = "Autenticazione Richiesta",
                    Reason = reason,
                    AllowAlternateAuthentication = true, // Permetti PIN se biometric fail
                    ConfirmationRequired = false
                });

            return result.Succeeded;
        }
        catch (Exception ex)
        {
            // Log error
            return false;
        }
    }

    public async Task<BiometricType> GetBiometricTypeAsync()
    {
        if (!await IsAvailableAsync())
            return BiometricType.None;

        // Platform-specific detection
#if ANDROID
        return BiometricType.Fingerprint; // Android usa fingerprint/face mixed
#elif IOS
        return BiometricType.Face; // iPhone X+ usa Face ID
#else
        return BiometricType.None;
#endif
    }
}
```

#### UI Flow

**App.xaml.cs** (On Resume):

```csharp
protected override async void OnResume()
{
    base.OnResume();

    // Verifica setting utente
    var biometricEnabled = Preferences.Get("biometric_enabled", true);

    if (biometricEnabled)
    {
        var authService = ServiceHelper.GetService<IBiometricAuthService>();
        var authenticated = await authService.AuthenticateAsync("Sblocca MoneyMind");

        if (!authenticated)
        {
            // Blocca app con overlay
            MainPage = new BiometricLockPage();
        }
    }
}
```

**BiometricLockPage.xaml**:
- Icona lucchetto
- Label "Sblocca con Face ID / Touch ID"
- Button "Riprova"
- Button "Esci" (chiude app)

#### Settings Toggle

**SettingsPage.xaml**:
```xml
<Switch IsToggled="{Binding BiometricEnabled}"
        OnToggled="OnBiometricToggled" />
<Label Text="Richiedi Face ID all'apertura" />
```

**Vincolo**: Se user disabilita biometric → chiedi conferma con password/PIN.

---

### 2. **Database Encryption** (Opzionale ma Consigliato)

**Obiettivo**: Criptare DB SQLite con chiave derivata da device.

#### Opzione A: SQLCipher (Raccomandato)

**NuGet**: `SQLitePCLRaw.bundle_sqlcipher`

```csharp
public async Task InitializeAsync()
{
    var dbPath = GetDatabasePath();
    var key = await GetEncryptionKeyAsync(); // Da Keychain/KeyStore

    var options = new SQLiteConnectionString(
        dbPath,
        SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex,
        storeDateTimeAsTicks: true,
        key: key // Encryption key
    );

    _connection = new SQLiteAsyncConnection(options);
}

private async Task<string> GetEncryptionKeyAsync()
{
    // Usa SecureStorage per salvare/recuperare chiave
    var key = await SecureStorage.GetAsync("db_encryption_key");

    if (string.IsNullOrEmpty(key))
    {
        // Genera nuova chiave casuale
        key = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await SecureStorage.SetAsync("db_encryption_key", key);
    }

    return key;
}
```

**Pro**: Encryption at-rest completa
**Contro**: Performance -10%, compatibilità desktop (serve stessa chiave)

#### Opzione B: Column-Level Encryption (Semplice)

Cripta solo colonne sensibili (Descrizione, Causale):

```csharp
public class Transaction
{
    public int ID { get; set; }
    public DateTime Data { get; set; }
    public decimal Importo { get; set; }

    // Stored encrypted in DB
    private string _descrizioneEncrypted;

    [Ignore]
    public string Descrizione
    {
        get => DecryptString(_descrizioneEncrypted);
        set => _descrizioneEncrypted = EncryptString(value);
    }
}

private string EncryptString(string plainText)
{
    var key = GetEncryptionKey();
    using var aes = Aes.Create();
    aes.Key = key;
    aes.GenerateIV();

    var encryptor = aes.CreateEncryptor();
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

    // IV + Cipher
    return Convert.ToBase64String(aes.IV.Concat(cipherBytes).ToArray());
}
```

**Pro**: Semplice, compatibile desktop
**Contro**: Solo colonne specifiche

---

### 3. **Secure Storage (API Keys, Tokens)**

**Obiettivo**: Salvare API keys/license token in modo sicuro.

#### Android: KeyStore
#### iOS: Keychain

**.NET MAUI fornisce `SecureStorage`** (wrapper cross-platform):

```csharp
// Salva API Key
await SecureStorage.SetAsync("openai_api_key", apiKey);

// Recupera
var apiKey = await SecureStorage.GetAsync("openai_api_key");

// Elimina
SecureStorage.Remove("openai_api_key");
```

**IMPORTANTE**:
- Android: Richiede device lock screen (PIN/Pattern) attivo
- iOS: Sincronizza con iCloud Keychain (se abilitato)

#### Migrazione da Desktop

Desktop usa file cache Base64 nascosto. Mobile deve migrare:

```csharp
public async Task MigrateLicenseFromDesktop()
{
    // Desktop path
    var desktopLicensePath = @"C:\ProgramData\MoneyMind\.beta_license";

    if (File.Exists(desktopLicensePath))
    {
        var base64 = File.ReadAllText(desktopLicensePath);
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        // Salva in SecureStorage mobile
        await SecureStorage.SetAsync("license_token", json);
    }
}
```

---

### 4. **Network Security**

#### HTTPS Only

**Android (AndroidManifest.xml)**:
```xml
<application android:usesCleartextTraffic="false">
```

**iOS (Info.plist)**:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <false/>
</dict>
```

#### Certificate Pinning (Opzionale)

Per API critiche (Google Sheets beta license):

```csharp
public class PinnedHttpClientHandler : HttpClientHandler
{
    private const string ExpectedPublicKey = "sha256/AAAAAAAAAA...";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Verifica certificato server
        var certificate = request.RequestUri.Host == "sheets.googleapis.com"
            ? GetServerCertificate(response)
            : null;

        if (certificate != null && !VerifyPin(certificate))
        {
            throw new SecurityException("Certificate pinning failed!");
        }

        return response;
    }
}
```

#### API Key Obfuscation

**NON hardcodare chiavi in codice**:

```csharp
// ❌ BAD
private const string API_KEY = "sk-proj-ABC123...";

// ✅ GOOD
private async Task<string> GetApiKeyAsync()
{
    return await SecureStorage.GetAsync("openai_api_key");
}
```

---

### 5. **Input Validation & Sanitization**

#### SQL Injection Prevention

**.NET MAUI SQLite usa parametrized queries** (safe by default):

```csharp
// ✅ SAFE (parametrized)
await connection.QueryAsync<Transaction>(
    "SELECT * FROM Transazioni WHERE Descrizione = ?",
    searchText);

// ❌ UNSAFE (string interpolation)
await connection.QueryAsync<Transaction>(
    $"SELECT * FROM Transazioni WHERE Descrizione = '{searchText}'");
```

#### XSS Prevention (Export HTML/PDF)

```csharp
private string SanitizeHtml(string input)
{
    return System.Net.WebUtility.HtmlEncode(input);
}

// Uso in export PDF
html += $"<td>{SanitizeHtml(transaction.Descrizione)}</td>";
```

---

### 6. **Session Management & Auto-Lock**

#### Inactivity Timeout

**App.xaml.cs**:

```csharp
private DateTime _lastActivityTime = DateTime.Now;
private const int AUTO_LOCK_MINUTES = 5;

public void RegisterActivity()
{
    _lastActivityTime = DateTime.Now;
}

protected override async void OnResume()
{
    base.OnResume();

    var inactiveMinutes = (DateTime.Now - _lastActivityTime).TotalMinutes;

    if (inactiveMinutes > AUTO_LOCK_MINUTES)
    {
        // Lock app
        await RequestBiometricAuthAsync();
    }
}
```

#### Activity Tracking

Ogni Page OnAppearing():

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    (Application.Current as App)?.RegisterActivity();
}
```

---

### 7. **Logging & Audit Trail** (Privacy-Aware)

**NEVER log dati sensibili**:

```csharp
// ❌ BAD
Logger.Info($"Transaction created: {transaction.Descrizione}, {transaction.Importo}");

// ✅ GOOD
Logger.Info($"Transaction created: ID={transaction.ID}");
```

#### Audit Critical Actions

**LoggingService.cs**:

```csharp
public enum AuditAction
{
    Login,
    Logout,
    TransactionCreated,
    TransactionDeleted,
    ExportData,
    ImportData,
    SettingsChanged,
    DatabaseCleared
}

public void LogAudit(AuditAction action, string details = null)
{
    var entry = new AuditLogEntry
    {
        Timestamp = DateTime.UtcNow,
        Action = action.ToString(),
        Details = details, // NO dati sensibili!
        DeviceId = GetDeviceFingerprint()
    };

    // Salva in DB o file log
    SaveAuditLog(entry);
}
```

---

### 8. **Data Deletion & Privacy**

#### GDPR Right to Erasure

**SettingsPage** → "Elimina Tutti i Dati":

```csharp
[RelayCommand]
private async Task DeleteAllDataAsync()
{
    var confirmed = await Shell.Current.DisplayAlert(
        "Conferma Eliminazione",
        "Tutti i tuoi dati saranno eliminati PERMANENTEMENTE. Questa azione NON è reversibile.",
        "Elimina Tutto",
        "Annulla");

    if (!confirmed)
        return;

    try
    {
        // 1. Elimina database
        await _databaseService.DeleteDatabaseAsync();

        // 2. Elimina SecureStorage
        SecureStorage.RemoveAll();

        // 3. Elimina Preferences
        Preferences.Clear();

        // 4. Elimina file logs
        await _loggingService.ClearAllLogsAsync();

        // 5. Logout + ritorna a onboarding
        await Shell.Current.GoToAsync("//onboarding");
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}
```

---

### 9. **Screenshot Prevention** (Optional)

Previeni screenshot su schermate sensibili (Dashboard):

**Android (MainActivity.cs)**:
```csharp
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Blocca screenshot
    Window?.SetFlags(
        WindowManagerFlags.Secure,
        WindowManagerFlags.Secure);
}
```

**iOS (AppDelegate.cs)**:
```csharp
// iOS non permette blocco screenshot, ma possiamo nascondere contenuto in App Switcher
```

**Settings Toggle**: "Blocca Screenshot" (opzionale, default OFF per usabilità).

---

### 10. **Backup Encryption**

Export backup criptato:

```csharp
public async Task ExportEncryptedBackupAsync(string password)
{
    // 1. Serializza DB a JSON
    var transactions = await _databaseService.GetAllTransactionsAsync();
    var json = JsonSerializer.Serialize(transactions);

    // 2. Cripta con password utente
    var encrypted = EncryptWithPassword(json, password);

    // 3. Salva file .mmbackup
    var backupPath = Path.Combine(FileSystem.CacheDirectory, "backup_encrypted.mmbackup");
    await File.WriteAllBytesAsync(backupPath, encrypted);

    // 4. Share file
    await Share.RequestAsync(new ShareFileRequest
    {
        Title = "Backup MoneyMind Criptato",
        File = new ShareFile(backupPath)
    });
}

private byte[] EncryptWithPassword(string plainText, string password)
{
    using var aes = Aes.Create();
    var salt = RandomNumberGenerator.GetBytes(16);
    var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);

    aes.Key = key.GetBytes(32);
    aes.GenerateIV();

    var encryptor = aes.CreateEncryptor();
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

    // Return: Salt (16) + IV (16) + Cipher
    return salt.Concat(aes.IV).Concat(cipherBytes).ToArray();
}
```

---

## 📋 Security Checklist

### Development
- [ ] SecureStorage per API keys/tokens
- [ ] Biometric auth implementato
- [ ] Database encryption (SQLCipher o column-level)
- [ ] Parametrized queries (SQL injection safe)
- [ ] HTTPS only (no cleartext)
- [ ] Input validation su tutti i form
- [ ] Logging NO dati sensibili
- [ ] Auto-lock dopo 5 min inattività

### Testing
- [ ] Test auth flow (Face ID/Touch ID/PIN)
- [ ] Test session timeout
- [ ] Test encrypted backup/restore
- [ ] Test GDPR delete all data
- [ ] Penetration testing (OWASP Mobile Top 10)

### Deployment
- [ ] Obfuscation codice (ProGuard Android, .NET Obfuscator)
- [ ] Certificate pinning per API critiche
- [ ] Privacy Policy completa
- [ ] Security disclosure policy
- [ ] Bug bounty program (optional)

---

## 🛡️ OWASP Mobile Top 10 (2024)

### Compliance MoneyMindApp

| Risk | Descrizione | Mitigazione |
|------|-------------|-------------|
| M1: Improper Credential Usage | API keys hardcoded | ✅ SecureStorage |
| M2: Inadequate Supply Chain Security | Dipendenze vulnerabili | ✅ NuGet audit |
| M3: Insecure Authentication | Auth debole | ✅ Biometric + Device PIN |
| M4: Insufficient Input Validation | SQL injection, XSS | ✅ Parametrized queries |
| M5: Insecure Communication | HTTP cleartext | ✅ HTTPS only |
| M6: Inadequate Privacy Controls | Log dati sensibili | ✅ Privacy-aware logging |
| M7: Insufficient Binary Protection | Reverse engineering | ⚠️ Obfuscation raccomandato |
| M8: Security Misconfiguration | Debug mode in prod | ✅ Release builds |
| M9: Insecure Data Storage | DB non criptato | ✅ SQLCipher |
| M10: Insufficient Cryptography | Weak encryption | ✅ AES-256 + SHA-256 |

---

## 🔐 Encryption Standards

- **Symmetric**: AES-256-GCM
- **Hashing**: SHA-256 (PBKDF2 per password, 10k iterations)
- **Key Derivation**: Rfc2898DeriveBytes (PBKDF2)
- **Random**: `RandomNumberGenerator` (.NET Cryptography)

---

## 📱 Platform-Specific Security

### Android
- **KeyStore**: Chiavi hardware-backed (TEE/Secure Enclave)
- **SafetyNet**: Verifica device integrity (root detection)
- **Permissions**: Runtime model (6.0+)

### iOS
- **Keychain**: Sincronizza con iCloud (opzionale)
- **Secure Enclave**: Chiavi biometric (iPhone 5s+)
- **Jailbreak Detection**: Opzionale (UIKit checks)

---

## 🚨 Incident Response Plan

In caso di breach:

1. **Detect**: Monitoring logs anomali
2. **Contain**: Revoca API keys compromesse
3. **Eradicate**: Fix vulnerabilità
4. **Recover**: Deploy patch urgente
5. **Post-Incident**: Notifica utenti (GDPR 72h)

---

## 📞 Security Contacts

- **Report Vulnerability**: security@moneymind.app (da creare)
- **Responsible Disclosure**: 90 giorni per fix prima di public disclosure
- **Bug Bounty**: HackerOne/BugCrowd (futuro)

---

**Ultima Review**: 2025-01-XX (Aggiorna ogni major release)
