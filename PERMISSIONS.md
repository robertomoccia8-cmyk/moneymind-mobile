# Runtime Permissions - Android & iOS

## 🔐 Overview

Android 6.0+ (API 23) e iOS richiedono **runtime permissions** per accedere a risorse sensibili.

**IMPORTANTE**: Permissions devono essere richieste al momento dell'uso (just-in-time), NON all'avvio app.

---

## 📋 Permissions Necessarie

### Android

**AndroidManifest.xml** (`Platforms/Android/AndroidManifest.xml`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application android:allowBackup="true"
                 android:icon="@mipmap/appicon"
                 android:label="MoneyMind"
                 android:roundIcon="@mipmap/appicon_round"
                 android:supportsRtl="true"
                 android:usesCleartextTraffic="false">
    </application>

    <!-- ===== PERMISSIONS ===== -->

    <!-- Internet (per sync WiFi, beta license check, updates) -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />

    <!-- Storage (per import/export file) - Android 12- -->
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"
                     android:maxSdkVersion="32" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"
                     android:maxSdkVersion="32" />

    <!-- Storage (per import/export file) - Android 13+ -->
    <uses-permission android:name="android.permission.READ_MEDIA_IMAGES"
                     android:minSdkVersion="33" />

    <!-- Biometric (Face/Fingerprint) -->
    <uses-permission android:name="android.permission.USE_BIOMETRIC" />
    <uses-permission android:name="android.permission.USE_FINGERPRINT" />

    <!-- Camera (opzionale - solo se scan ricevute) -->
    <!-- <uses-permission android:name="android.permission.CAMERA" /> -->

    <!-- Vibration (per haptic feedback) -->
    <uses-permission android:name="android.permission.VIBRATE" />
</manifest>
```

### iOS

**Info.plist** (`Platforms/iOS/Info.plist`):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- App Info -->
    <key>CFBundleName</key>
    <string>MoneyMind</string>

    <!-- Permissions Descriptions (OBBLIGATORIE!) -->

    <!-- Face ID / Touch ID -->
    <key>NSFaceIDUsageDescription</key>
    <string>MoneyMind usa Face ID per proteggere i tuoi dati finanziari.</string>

    <!-- Photo Library (per import/export file via Photos) -->
    <key>NSPhotoLibraryUsageDescription</key>
    <string>MoneyMind ha bisogno di accedere alle foto per importare file di transazioni.</string>
    <key>NSPhotoLibraryAddUsageDescription</key>
    <string>MoneyMind salverà i file esportati nella tua libreria foto.</string>

    <!-- Camera (opzionale - solo se scan ricevute) -->
    <!--
    <key>NSCameraUsageDescription</key>
    <string>MoneyMind usa la fotocamera per scansionare ricevute.</string>
    -->

    <!-- Local Network (per WiFi Sync) -->
    <key>NSLocalNetworkUsageDescription</key>
    <string>MoneyMind usa la rete locale per sincronizzare con il computer.</string>

    <!-- Bonjour Services (per mDNS discovery - opzionale) -->
    <key>NSBonjourServices</key>
    <array>
        <string>_moneymind._tcp</string>
    </array>

    <!-- Privacy - No iCloud Sync for Financial Data -->
    <key>NSUbiquitousContainers</key>
    <dict/>
</dict>
</plist>
```

---

## 🛠️ PermissionService Implementation

### Interface

**File**: `Services/Platform/IPermissionService.cs`

```csharp
namespace MoneyMindApp.Services.Platform;

public interface IPermissionService
{
    Task<PermissionStatus> CheckStatusAsync<T>() where T : Permissions.BasePermission, new();
    Task<PermissionStatus> RequestAsync<T>() where T : Permissions.BasePermission, new();
    Task<bool> CheckAndRequestAsync<T>() where T : Permissions.BasePermission, new();
}

public enum PermissionStatus
{
    Granted,
    Denied,
    Restricted,
    Unknown
}
```

### Implementation

**File**: `Services/Platform/PermissionService.cs`

```csharp
using Microsoft.Maui.ApplicationModel;

namespace MoneyMindApp.Services.Platform;

public class PermissionService : IPermissionService
{
    public async Task<PermissionStatus> CheckStatusAsync<T>() where T : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<T>();
        return MapStatus(status);
    }

    public async Task<PermissionStatus> RequestAsync<T>() where T : Permissions.BasePermission, new()
    {
        var status = await Permissions.RequestAsync<T>();
        return MapStatus(status);
    }

    public async Task<bool> CheckAndRequestAsync<T>() where T : Permissions.BasePermission, new()
    {
        var status = await CheckStatusAsync<T>();

        if (status == PermissionStatus.Granted)
            return true;

        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // iOS non permette re-request se già denied, user deve aprire Settings
            await Shell.Current.DisplayAlert(
                "Permesso Richiesto",
                "Devi abilitare questo permesso nelle Impostazioni dell'app.",
                "Apri Impostazioni",
                "Annulla");

            AppInfo.ShowSettingsUI();
            return false;
        }

        // Request permission
        status = await RequestAsync<T>();
        return status == PermissionStatus.Granted;
    }

    private PermissionStatus MapStatus(Microsoft.Maui.ApplicationModel.PermissionStatus status)
    {
        return status switch
        {
            Microsoft.Maui.ApplicationModel.PermissionStatus.Granted => PermissionStatus.Granted,
            Microsoft.Maui.ApplicationModel.PermissionStatus.Denied => PermissionStatus.Denied,
            Microsoft.Maui.ApplicationModel.PermissionStatus.Restricted => PermissionStatus.Restricted,
            _ => PermissionStatus.Unknown
        };
    }
}
```

---

## 📱 Usage Patterns

### 1. Storage Permission (Import CSV)

**ImportViewModel.cs**:

```csharp
[RelayCommand]
private async Task SelectFileAsync()
{
    try
    {
        // Check/Request permission
        var hasPermission = await _permissionService.CheckAndRequestAsync<Permissions.StorageRead>();

        if (!hasPermission)
        {
            await Shell.Current.DisplayAlert(
                "Permesso Negato",
                "MoneyMind ha bisogno di accedere ai file per importare transazioni.",
                "OK");
            return;
        }

        // Procedi con file picker
        var result = await FilePicker.PickAsync(new PickOptions
        {
            FileTypes = FilePickerFileType.Plain
        });

        if (result != null)
        {
            await ImportFileAsync(result);
        }
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}
```

### 2. Storage Permission (Export Excel)

**ExportViewModel.cs**:

```csharp
[RelayCommand]
private async Task ExportExcelAsync()
{
    try
    {
        // Android 13+ non richiede permission per file condivisi (Share API)
        // Android 12- richiede WRITE_EXTERNAL_STORAGE
        if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major < 13)
        {
            var hasPermission = await _permissionService.CheckAndRequestAsync<Permissions.StorageWrite>();

            if (!hasPermission)
            {
                await Shell.Current.DisplayAlert(
                    "Permesso Negato",
                    "MoneyMind ha bisogno di salvare file sul dispositivo.",
                    "OK");
                return;
            }
        }

        // Genera Excel
        var filePath = await _exportService.ExportToExcelAsync(_transactions);

        // Share file (no permission required)
        await Share.RequestAsync(new ShareFileRequest
        {
            Title = "Esporta Transazioni",
            File = new ShareFile(filePath)
        });
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}
```

### 3. Camera Permission (Scan Ricevute - Opzionale)

**ScanReceiptPage.xaml.cs**:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    var hasPermission = await _permissionService.CheckAndRequestAsync<Permissions.Camera>();

    if (!hasPermission)
    {
        await Shell.Current.DisplayAlert(
            "Fotocamera Non Disponibile",
            "MoneyMind ha bisogno della fotocamera per scansionare ricevute.",
            "OK");

        await Shell.Current.GoToAsync("..");
        return;
    }

    // Avvia camera preview
    StartCameraPreview();
}
```

### 4. Biometric Permission

**BiometricAuthService.cs** (già gestito internamente da MAUI):

```csharp
public async Task<bool> AuthenticateAsync(string reason)
{
    try
    {
        // MAUI gestisce automaticamente permission per biometric
        var result = await DeviceSecurityMonitor.AuthenticateAsync(
            new AuthenticationRequest
            {
                Title = "Autenticazione Richiesta",
                Reason = reason
            });

        return result.Succeeded;
    }
    catch (FeatureNotEnabledException)
    {
        // Device non ha lock screen o biometric
        await Shell.Current.DisplayAlert(
            "Autenticazione Non Disponibile",
            "Configura un PIN, pattern o biometric nelle impostazioni del dispositivo.",
            "OK");
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Biometric auth error: {ex.Message}");
        return false;
    }
}
```

---

## 🚨 Permission Denial Handling

### Graceful Degradation

```csharp
public async Task<bool> TryImportFileAsync()
{
    var hasPermission = await _permissionService.CheckAndRequestAsync<Permissions.StorageRead>();

    if (!hasPermission)
    {
        // Fallback: usa Share API (no permission required)
        var result = await Shell.Current.DisplayAlert(
            "Permesso Storage Negato",
            "Vuoi importare file tramite condivisione?",
            "Sì",
            "No");

        if (result)
        {
            await UseShareSheetImportAsync();
        }

        return false;
    }

    // Standard file picker
    return await UseFilePickerImportAsync();
}

private async Task UseShareSheetImportAsync()
{
    await Shell.Current.DisplayAlert(
        "Condividi File",
        "Apri il file manager, seleziona il file CSV e scegli 'Condividi con MoneyMind'.",
        "OK");

    // App deve gestire intent/share target (vedi DEPLOYMENT.md)
}
```

---

## 🎯 Best Practices

### 1. Just-In-Time Requests

**❌ BAD** (richiesta all'avvio):
```csharp
// App.xaml.cs
protected override async void OnStart()
{
    await Permissions.RequestAsync<Permissions.StorageRead>();
    await Permissions.RequestAsync<Permissions.Camera>();
    // User confused: "Perché serve fotocamera?"
}
```

**✅ GOOD** (richiesta quando serve):
```csharp
// ImportPage.OnAppearing()
protected override async void OnAppearing()
{
    // Solo quando user clicca "Importa"
}
```

### 2. Explain Before Asking

**❌ BAD**:
```csharp
await Permissions.RequestAsync<Permissions.Camera>();
// Dialog system generico appare senza contesto
```

**✅ GOOD**:
```csharp
// Mostra rationale prima
var shouldRequest = await Shell.Current.DisplayAlert(
    "Fotocamera Richiesta",
    "MoneyMind usa la fotocamera per scansionare ricevute e estrarre automaticamente importi e merchant.",
    "OK",
    "Annulla");

if (shouldRequest)
{
    await Permissions.RequestAsync<Permissions.Camera>();
}
```

### 3. Handle "Don't Ask Again" (Android)

```csharp
public async Task<bool> CheckAndRequestWithRationaleAsync<T>() where T : Permissions.BasePermission, new()
{
    var status = await Permissions.CheckStatusAsync<T>();

    if (status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
        return true;

    // Android: se user ha cliccato "Don't ask again"
    if (ShouldShowRationale<T>())
    {
        var result = await Shell.Current.DisplayAlert(
            "Permesso Richiesto",
            "Questo permesso è necessario per la funzionalità. Vuoi aprire le impostazioni?",
            "Impostazioni",
            "Annulla");

        if (result)
        {
            AppInfo.ShowSettingsUI();
        }

        return false;
    }

    // Prima richiesta: mostra dialog sistema
    status = await Permissions.RequestAsync<T>();
    return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
}

private bool ShouldShowRationale<T>() where T : Permissions.BasePermission, new()
{
#if ANDROID
    // Platform-specific check
    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
    var permission = GetAndroidPermissionString<T>();
    return AndroidX.Core.App.ActivityCompat.ShouldShowRequestPermissionRationale(activity, permission);
#else
    return false;
#endif
}
```

---

## 📊 Permission Check Matrix

| Feature | Permission | Android | iOS | Fallback |
|---------|------------|---------|-----|----------|
| Import CSV | StorageRead | ✅ API 23+ | ✅ | Share Sheet |
| Export Excel | StorageWrite | ✅ API 23-32 | ✅ | Share API (no perm) |
| Scan Ricevute | Camera | ✅ | ✅ | Manual entry |
| Biometric Lock | USE_BIOMETRIC | ✅ | ✅ | PIN fallback |
| WiFi Sync | INTERNET | ✅ Auto | ✅ Auto | File sync |
| Beta License | INTERNET | ✅ Auto | ✅ Auto | Offline grace 7d |

**Auto** = Permission granted automatically (non-dangerous)

---

## 🧪 Testing Permissions

### Simulare Denial

**Android**:
```bash
# Revoca permission
adb shell pm revoke com.moneymind.app android.permission.READ_EXTERNAL_STORAGE

# Grant permission
adb shell pm grant com.moneymind.app android.permission.READ_EXTERNAL_STORAGE
```

**iOS Simulator**:
Settings → MoneyMind → Permissions → Toggle OFF

### Test Cases

1. **First Request**: User grants → Funzionalità attiva
2. **First Request**: User denies → Mostra fallback
3. **Second Request** (Android): User "Don't ask again" → Redirect settings
4. **Second Request** (iOS): Permission denied → Redirect settings
5. **Revoke During Use**: Permission revocata mentre app in foreground → Graceful error

---

## 🔐 Privacy Impact

### Permissions e Privacy Policy

Ogni permission **DEVE** essere dichiarata in Privacy Policy con:
- Perché serve
- Quando viene richiesta
- Cosa succede se negata
- Come revocarla

**Esempio Privacy Policy**:

> **Accesso Storage (Android)**
> MoneyMind richiede accesso ai file per importare estratti conto CSV dalla tua banca. Il permesso viene richiesto solo quando selezioni "Importa". Se neghi il permesso, puoi usare la funzione "Condividi" come alternativa. Nessun file viene caricato online.

---

## 📋 Implementation Checklist

### Android
- [ ] `AndroidManifest.xml` con tutte le permission
- [ ] Runtime check per API 23+
- [ ] Rationale dialog per "Don't ask again"
- [ ] Fallback per ogni permission negata
- [ ] Test su Android 6-14

### iOS
- [ ] `Info.plist` con `UsageDescription` per ogni permission
- [ ] Redirect a Settings se denied
- [ ] Test su iOS 11-17

### Cross-Platform
- [ ] `PermissionService.cs` implementato
- [ ] `CheckAndRequestAsync` usato ovunque
- [ ] Just-in-time requests
- [ ] Graceful degradation
- [ ] Privacy Policy aggiornata

---

## 🚀 Next Steps

1. Implementa `PermissionService.cs`
2. Aggiungi check in ogni feature che richiede permission
3. Testa denial scenarios
4. Aggiorna Privacy Policy
5. Submit a Google Play/App Store review

---

**Ultima Review**: 2025-01-XX
