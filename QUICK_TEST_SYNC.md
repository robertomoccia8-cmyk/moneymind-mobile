# 🚀 QUICK TEST - Sincronizzazione WiFi (5 Minuti)

> **Guida rapida** per testare subito la sincronizzazione Desktop ↔ Mobile

---

## 🎯 TEST RAPIDO: EMULATORE (stesso PC)

### ⏱️ Tempo Stimato: 5 minuti

### Step 1️⃣: Prepara Emulatore (30 sec)

```bash
# 1. Verifica emulatore attivo
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" devices

# 2. Configura port forwarding (CRITICO per emulatore!)
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" forward tcp:8765 tcp:8765
```

✅ **Output Atteso**:
```
List of devices attached
emulator-5554   device
```

### Step 2️⃣: Avvia Server Mobile (1 min)

1. **Apri MoneyMindApp su emulatore**
2. **Menu** → Impostazioni → **Sincronizzazione WiFi**
3. **Premi**: 🔵 **"Avvia Server"**
4. **Verifica**: Stato diventa **🟢 Server attivo**
5. **IP Mostrato**: `10.0.2.15` (normale per emulatore, ignoralo)

### Step 3️⃣: Crea Dati Test Desktop (1 min)

1. **Apri MoneyMind Desktop**
2. **Aggiungi 3 transazioni veloci**:
   ```
   01/01/2025 | +1500 € | "Test Stipendio"
   02/01/2025 | -50 €   | "Test Spesa"
   03/01/2025 | -30 €   | "Test Benzina"
   ```

### Step 4️⃣: Connetti Desktop → Mobile (1 min)

1. **Su Desktop**: Menu → Impostazioni → **Sincronizzazione WiFi**
2. **Inserisci IP**: `127.0.0.1` o `localhost` ← **IMPORTANTE!**
3. **Porta**: `8765`
4. **Premi**: 📡 **"Ping"** o **"Test Connessione"**

✅ **Output Atteso**:
```
✅ Connesso! Server mobile raggiungibile.
```

❌ **Se fallisce**:
```bash
# Ri-esegui port forward
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" forward tcp:8765 tcp:8765

# Verifica server mobile ancora attivo (tornare all'app e controllare)
```

### Step 5️⃣: Sincronizza Desktop → Mobile (1 min)

1. **Direzione**: Desktop → Mobile
2. **Modalità**: **SOSTITUISCI** (mobile è vuoto)
3. **Conti**: Seleziona tutti
4. **Premi**: 🔄 **"Sincronizza"**
5. **Attendi**: 5-10 secondi

✅ **Output Atteso**:
```
✅ Sincronizzazione Completata!

Desktop → Mobile: 3 transazioni sincronizzate
Backup creato automaticamente
```

### Step 6️⃣: Verifica su Mobile (30 sec)

1. **Apri MoneyMindApp su emulatore**
2. **Vai alla Dashboard**
3. **Verifica**:
   - ✅ Saldo aggiornato: `1420 €` (1500 - 50 - 30)
   - ✅ 3 transazioni visibili
   - ✅ Descrizioni corrette

---

## 🎉 TEST SUPERATO!

Hai appena sincronizzato con successo Desktop → Emulatore via WiFi Sync!

### 🔄 Ora Prova il Contrario: Mobile → Desktop

#### Quick Steps:

1. **Su emulatore**: Aggiungi 2 nuove transazioni
   ```
   04/01/2025 | -20 € | "Pizza Mobile"
   05/01/2025 | -10 € | "Caffè Mobile"
   ```

2. **Su Desktop**: Sync Dialog → **Mobile → Desktop** + **UNISCI**

3. **Verifica Desktop**: Ora hai **5 transazioni totali** (3 + 2)

✅ **Se vedi tutte e 5 le transazioni**: Sync bidirezionale funziona! 🎊

---

## 📱 TEST RAPIDO: CELLULARE FISICO (via Hotspot)

### ⏱️ Tempo Stimato: 10 minuti

### Step 1️⃣: Attiva Hotspot Cellulare (2 min)

1. **Sul cellulare**:
   - Impostazioni → **Hotspot WiFi**
   - ✅ **Attiva Hotspot**
   - 📝 **Annota IP**: Tipico `192.168.43.1` (Android) o `172.20.10.1` (iOS)

2. **Sul PC**:
   - Connetti all'hotspot del cellulare
   - ✅ Verifica connessione:
     ```bash
     ping 192.168.43.1
     ```

### Step 2️⃣: Deploy App su Cellulare (5 min)

#### Opzione A: Via USB + ADB (Veloce)

```bash
cd C:\Users\rober\Documents\MoneyMindApp

# Build APK
dotnet build -c Release -f net8.0-android

# Trova APK compilato
# Tipico path: bin\Release\net8.0-android\com.moneymind.app-Signed.apk

# Installa su cellulare (collegato via USB)
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" install -r "bin\Release\net8.0-android\com.moneymind.app-Signed.apk"
```

#### Opzione B: Manuale (Più Lenta)

1. Copia APK su cellulare via USB o cloud
2. Sul cellulare: Apri file manager → APK → Installa

### Step 3️⃣: Avvia Server su Cellulare (1 min)

1. **Apri MoneyMindApp su cellulare**
2. **Menu** → Sincronizzazione WiFi
3. **Premi**: **"Avvia Server"**
4. **IP Mostrato**: Dovrebbe essere `192.168.43.1` (IP hotspot)

### Step 4️⃣: Connetti Desktop (1 min)

1. **Su Desktop**: Sync Dialog
2. **Inserisci IP**: `192.168.43.1` ← **IP hotspot cellulare**
3. **Premi**: **"Ping"**

✅ **Se connesso**: Procedi con sync come prima!

---

## 🐛 TROUBLESHOOTING EXPRESS

### ❌ Desktop non si connette a Emulatore

**Causa**: Port forward non attivo

**Fix**:
```bash
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" forward tcp:8765 tcp:8765
```

Poi usa `localhost:8765` su Desktop.

---

### ❌ Desktop non si connette a Cellulare (Hotspot)

**Causa**: PC non sulla rete hotspot, o firewall

**Fix**:
1. Verifica PC connesso all'hotspot:
   ```bash
   ping 192.168.43.1
   ```
2. Se fallisce: Riconnetti WiFi
3. Se ancora fallisce: Disattiva firewall temporaneamente
4. Verifica IP cellulare (potrebbe non essere 192.168.43.1):
   - Android: Impostazioni → Info → Stato → Indirizzo IP
   - iOS: Impostazioni → WiFi → Info (i) → IP

---

### ❌ Server Mobile Crasha

**Causa**: Permissions mancanti

**Fix**:
```bash
# Controlla log crash
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" logcat -d | findstr /i "crash"

# Rilancia app
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" shell am start -n com.moneymind.app/crc64d7669b0d2e8b2a89.MainActivity
```

---

### ❌ Transazioni Non Visibili su Mobile

**Causa**: App non ricaricata dopo sync

**Fix**:
1. Chiudi app mobile completamente
2. Riapri MoneyMindApp
3. Vai alla Dashboard → Refresh manuale

---

## 📊 CHECKLIST RAPIDA

Prima di iniziare i test, verifica:

### Emulatore Test
- [ ] Emulatore Android attivo
- [ ] ADB configurato
- [ ] Port forward attivo (`adb forward tcp:8765 tcp:8765`)
- [ ] App mobile installata su emulatore
- [ ] App desktop funzionante

### Cellulare Fisico Test
- [ ] Hotspot cellulare attivo
- [ ] PC connesso all'hotspot
- [ ] App mobile installata su cellulare
- [ ] IP hotspot annotato (es. `192.168.43.1`)
- [ ] Desktop raggiunge cellulare (`ping` funziona)

---

## 📝 COMANDI UTILI

### ADB Express Commands

```bash
# Path ADB (usa questo per tutti i comandi)
SET ADB="C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe"

# Verifica dispositivi connessi
%ADB% devices

# Port forward per emulatore
%ADB% forward tcp:8765 tcp:8765

# Installa APK
%ADB% install -r "path\to\app.apk"

# Avvia app
%ADB% shell am start -n com.moneymind.app/crc64d7669b0d2e8b2a89.MainActivity

# Verifica log app
%ADB% logcat -d -s "MoneyMindApp:V"

# Screenshot (per debug)
%ADB% shell screencap /sdcard/screen.png
%ADB% pull /sdcard/screen.png
```

### Network Testing

```bash
# Test connessione hotspot
ping 192.168.43.1

# Test porta aperta (richiede telnet)
telnet 192.168.43.1 8765

# Verifica connessione WiFi
ipconfig | findstr /i "192.168"
```

---

## 🎯 RISULTATO ATTESO

Dopo aver completato entrambi i test rapidi:

✅ **Desktop → Emulatore**: Funziona via `localhost:8765`
✅ **Emulatore → Desktop**: Funziona via `localhost:8765`
✅ **Desktop → Cellulare**: Funziona via `192.168.43.1:8765`
✅ **Cellulare → Desktop**: Funziona via `192.168.43.1:8765`

**Transazioni sincronizzate correttamente in entrambe le direzioni!** 🎉

---

## 📖 DOCUMENTAZIONE COMPLETA

Per test avanzati e scenari complessi, vedi:

- **`TEST_SYNC_PLAN.md`** - Piano completo con 10 test dettagliati
- **`SYNC_STRATEGY.md`** - Architettura tecnica sync
- **`WIFI_SYNC_IMPLEMENTATION.md`** - Specifiche API e modelli

---

**Pronto per testare?** Segui gli step sopra e buon test! 🚀

Se tutto funziona, aggiorna `STATO_ARTE.md` con:
```markdown
## ✅ Sincronizzazione WiFi - TESTATA E FUNZIONANTE

- Desktop → Emulatore: ✅
- Emulatore → Desktop: ✅
- Desktop → Cellulare (Hotspot): ✅
- Cellulare → Desktop (Hotspot): ✅

Data Test: [TUA_DATA]
Tester: [TUO_NOME]
```

---

**Versione**: 1.0 Quick Start
**Ultimo Aggiornamento**: 25/11/2025
