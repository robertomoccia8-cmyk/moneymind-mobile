# Testing MoneyMindApp su Android Studio Emulator

## 📋 Prerequisiti Verificati

✅ .NET SDK 9.0 installato
✅ Android workload installato
✅ Android Studio installato

---

## 🔧 Step 1: Configurare Android SDK Path

### Opzione A: Tramite Variabile Ambiente (Raccomandato)

1. **Trova il tuo Android SDK Path**:
   - Apri Android Studio
   - File → Settings (o Ctrl+Alt+S)
   - Appearance & Behavior → System Settings → Android SDK
   - Copia il path (es. `C:\Users\rober\AppData\Local\Android\Sdk`)

2. **Imposta variabile ambiente**:
   ```powershell
   # Apri PowerShell come Amministratore
   [Environment]::SetEnvironmentVariable("ANDROID_HOME", "C:\Users\rober\AppData\Local\Android\Sdk", "User")
   [Environment]::SetEnvironmentVariable("ANDROID_SDK_ROOT", "C:\Users\rober\AppData\Local\Android\Sdk", "User")

   # Aggiungi al PATH
   $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
   $newPath = "$currentPath;C:\Users\rober\AppData\Local\Android\Sdk\platform-tools;C:\Users\rober\AppData\Local\Android\Sdk\emulator"
   [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
   ```

3. **Riavvia terminale** per applicare le modifiche

### Opzione B: Usa parametro build

```bash
dotnet build -f net8.0-android -p:AndroidSdkDirectory="C:\Users\rober\AppData\Local\Android\Sdk"
```

---

## 📱 Step 2: Creare/Avviare Emulatore Android

### Metodo 1: Tramite Android Studio (Più Facile)

1. **Apri Android Studio**

2. **Avvia Device Manager**:
   - Click su icona 📱 "Device Manager" nella toolbar (o Tools → Device Manager)

3. **Crea nuovo dispositivo virtuale** (se non ne hai):
   - Click "Create Device"
   - Seleziona "Pixel 7" o simile (raccomandato)
   - Next → Seleziona System Image:
     - **API Level 33 (Android 13 - Tiramisu)** ← Raccomandato
     - O **API Level 34 (Android 14)**
   - Download system image se richiesto
   - Next → Finish

4. **Avvia emulatore**:
   - Click sul ▶️ (Play) accanto al dispositivo
   - Aspetta che l'emulatore si avvii completamente (30-60 secondi)

### Metodo 2: Tramite Terminale

```bash
# Lista emulatori disponibili
emulator -list-avds

# Avvia un emulatore specifico
emulator -avd Pixel_7_API_33
```

---

## 🏗️ Step 3: Build & Deploy MoneyMindApp

### 1. Restore NuGet Packages

```bash
cd C:\Users\rober\Documents\MoneyMindApp
dotnet restore
```

**Output atteso**: `Restore completed in X seconds`

### 2. Build per Android

```bash
# Build in modalità Debug
dotnet build -f net8.0-android -c Debug

# Se serve specificare SDK path:
dotnet build -f net8.0-android -c Debug -p:AndroidSdkDirectory="C:\Users\rober\AppData\Local\Android\Sdk"
```

**Possibili Errori**:

#### ❌ Error: "Java SDK not found"
**Fix**: Installa JDK 11 o 17
```bash
# Scarica da: https://adoptium.net/
# Oppure usa quello di Android Studio:
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
```

#### ❌ Error: "Android SDK not found"
**Fix**: Specifica path SDK (vedi Step 1)

#### ❌ Error: "Plugin.Fingerprint" compilation error
**Fix Temporaneo**: Commenta nelle pagine che lo usano
```csharp
// BiometricSetupViewModel.cs - commenta temporaneamente
// var available = await _biometricService.IsAvailableAsync();
var available = false; // TODO: fix Plugin.Fingerprint
```

### 3. Deploy su Emulatore

**Metodo A: Deploy automatico**
```bash
# Deploy e run (richiede emulatore già avviato)
dotnet build -t:Run -f net8.0-android
```

**Metodo B: Deploy manuale**
```bash
# 1. Build APK
dotnet publish -f net8.0-android -c Debug

# 2. Trova APK generato
# Percorso: bin\Debug\net8.0-android\com.moneymind.app-Signed.apk

# 3. Verifica dispositivi connessi
adb devices

# 4. Install APK
adb install -r bin\Debug\net8.0-android\com.moneymind.app-Signed.apk

# 5. Launch app
adb shell am start -n com.moneymind.app/crc64XXX.MainActivity
```

---

## 🐛 Step 4: Debugging

### Visualizzare Logs Real-time

```bash
# Filtra logs per MoneyMind
adb logcat | findstr "MoneyMind"

# Oppure tutti i logs .NET
adb logcat | findstr "mono"
```

### Hot Reload (se supportato)

1. Modifica codice C#
2. Salva file
3. App si ricarica automaticamente (su .NET 8+)

### Debugging con Visual Studio

1. Apri `MoneyMindApp.sln` in Visual Studio 2022
2. Seleziona target: Android Emulator
3. F5 per debug
4. Breakpoints funzionanti!

---

## ✅ Step 5: Test Funzionalità

### Checklist Test Onboarding

- [ ] 1. **WelcomePage** appare al primo avvio
  - Verifica: 4 bullet points features
  - Button "Inizia" → LicenseActivationPage
  - Button "Salta" → MainPage

- [ ] 2. **LicenseActivationPage**
  - Input License Key (testa skip per ora)
  - Button "Salta" → CreateAccountPage

- [ ] 3. **CreateAccountPage**
  - Input Nome Conto: "Conto Test"
  - Input Saldo Iniziale: 1000
  - Button "Crea Conto" → BiometricSetupPage

- [ ] 4. **BiometricSetupPage**
  - Button "Abilita" (potrebbe fallire su emulatore)
  - Button "Salta" → QuickTourPage

- [ ] 5. **QuickTourPage**
  - Button "Vai alla Dashboard" → MainPage

### Checklist Test Dashboard (MainPage)

- [ ] **Statistiche Visibili**
  - Card "Saldo Totale" con €1000.00
  - Card "Entrate" con €0.00
  - Card "Uscite" con €0.00
  - Card "Risparmio" con €0.00
  - Card "Movimenti" con 0

- [ ] **Toggle Visibilità**
  - Tap su 👁 → valori diventano ****
  - Tap di nuovo → valori riappaiono

- [ ] **Pull-to-Refresh**
  - Swipe down → spinner → dati ricaricati

- [ ] **Navigation**
  - Button "Vedi Tutte" → TransactionsPage
  - Tab "Transazioni" (bottom) → TransactionsPage
  - Tab "Conti" (bottom) → AccountSelectionPage

### Checklist Test Transactions (TransactionsPage)

- [ ] **Empty State**
  - Messaggio "Nessuna transazione trovata"

- [ ] **Search Bar**
  - Input testo → (nessun risultato per ora)

- [ ] **Filtri**
  - Tap 🎚 → panel filtri appare
  - DatePicker Inizio/Fine selezionabili
  - Button "Cancella Filtri" → reset

- [ ] **FAB Button**
  - Tap "+" → (TODO: navigate to add page)

### Checklist Test Accounts (AccountSelectionPage)

- [ ] **Account Default**
  - Card "Conto Principale" visibile
  - Icona 💳
  - Saldo €0.00
  - Checkmark ✓ visibile (conto selezionato)

- [ ] **Tap to Select**
  - Tap su card → naviga a MainPage
  - Tab "Dashboard" → MainPage con "Conto Principale"

- [ ] **Action Buttons**
  - Tap ✏️ → (TODO: navigate to edit page)
  - Tap 🗑️ → Dialog "Non puoi eliminare l'ultimo conto"

- [ ] **FAB Button**
  - Tap "+" → (TODO: navigate to add page)

---

## 🚨 Troubleshooting Comuni

### App crashA all'avvio

**Causa**: Plugin.Fingerprint non disponibile su emulatore

**Fix**:
```csharp
// App.xaml.cs - OnStart()
// Commenta temporaneamente biometric check
// if (biometricEnabled) { ... }
```

### UI non risponde

**Causa**: Deadlock async/await

**Check**:
- Tutti i metodi database usano `await`?
- Mai usare `.Result` o `.Wait()`?

### Database non si crea

**Causa**: Path non accessibile

**Check**:
```csharp
// Debug log path
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MoneyMind_Global.db");
Debug.WriteLine($"DB Path: {dbPath}");
```

**Path Android**: `/data/user/0/com.moneymind.app/files/`

### Navigation non funziona

**Causa**: Routes non registrati

**Check**: `AppShell.xaml` contiene tutti i `<ShellContent>` con `Route="..."` corretto

---

## 📊 Performance Monitoring

### Memory Usage

```bash
adb shell dumpsys meminfo com.moneymind.app
```

### CPU Usage

```bash
adb shell top -n 1 | findstr "moneymind"
```

### FPS Counter

Abilita in Android Studio:
- Tools → Android → Layout Inspector
- View → Show FPS

---

## 🎯 Test Success Criteria

✅ **App si avvia senza crash**
✅ **Onboarding flow completo funziona**
✅ **Dashboard mostra statistiche (anche se a 0)**
✅ **Navigation tra tabs funziona**
✅ **Pull-to-refresh funziona su tutte le pagine**
✅ **Account default viene creato automaticamente**
✅ **Toggle visibilità valori funziona**
✅ **Filtri transazioni si aprono/chiudono**
✅ **Dialogs conferma eliminazione appaiono**

---

## 🚀 Next Steps Dopo Test

1. **Fix Errori Trovati** durante il test
2. **Aggiungere Transazioni di Test** (manualmente nel DB o via UI)
3. **Implementare Add/Edit Transaction Pages** (FASE 2)
4. **Test con Dati Reali** (importa CSV o crea manualmente)
5. **Performance Testing** con 100+ transazioni

---

## 📝 Report Template

Dopo il test, compila questo report:

```
# MoneyMindApp - Test Report

**Data**: 20 Ottobre 2025
**Device**: Android Emulator Pixel 7 API 33
**Build**: Debug net8.0-android

## ✅ Test Passati
- [ ] App avvio
- [ ] Onboarding flow
- [ ] Dashboard
- [ ] Transactions page
- [ ] Accounts page
- [ ] Navigation

## ❌ Errori Trovati
1. [Descrizione errore]
   - Steps to reproduce
   - Log output
   - Fix proposto

## 📊 Performance
- Tempo avvio: X secondi
- Memory usage: X MB
- Crash: Si/No

## 💡 Note
[Osservazioni generali...]
```

---

**Pronto per il test! Segui gli step 1-5 in ordine. Buona fortuna! 🚀**
