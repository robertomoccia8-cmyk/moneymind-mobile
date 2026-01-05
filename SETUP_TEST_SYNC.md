# 🚀 SETUP TEST - Sincronizzazione WiFi

## ✅ STATO ATTUALE - Pronto per Test!

### Componenti Verificati

- ✅ **WiFiSyncService Mobile**: Implementato e funzionante
  - File: `Services/Sync/WiFiSyncService.cs`
  - Server HTTP su porta 8765
  - Supporta 3 modalità: SOSTITUISCI, UNISCI, SOLO NUOVE
  - Backup automatico pre-sync

- ✅ **WiFiSyncClient Desktop**: Implementato e funzionante
  - File: `C:\Users\rober\Documents\MoneyMind\Services\WiFiSyncClientService.vb`
  - Dialog: `C:\Users\rober\Documents\MoneyMind\Views\WiFiSyncDialog.xaml`
  - Client HTTP per connessione a mobile

- ✅ **Emulatore Android**: Attivo e pronto
  - Device ID: `emulator-5554`
  - App installata: `com.moneymind.app`
  - Activity: `crc64399a401da9d21b0a.MainActivity`
  - Port forwarding configurato: `localhost:8765` → `emulator:8765`

---

## 🎯 COME INIZIARE I TEST

### Metodo 1: Script Automatico (Consigliato)

**File**: `test-sync.bat`

```batch
cd C:\Users\rober\Documents\MoneyMindApp
test-sync.bat
```

**Menu Opzioni**:
1. Verifica Dispositivi
2. Setup Port Forwarding
3. Avvia App Mobile
4. Verifica Log
5. Test Connessione
6. Screenshot
7. Installa APK
8. **Test Completo Desktop → Mobile** ← Usa questa!
9. Pulisci Dati App

### Metodo 2: Manuale Rapido (5 minuti)

#### Step 1: Port Forwarding
```bash
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" forward tcp:8765 tcp:8765
```

#### Step 2: Avvia App Mobile
```bash
"C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" shell am start -n com.moneymind.app/crc64399a401da9d21b0a.MainActivity
```

#### Step 3: Avvia Server Mobile
- Sull'emulatore: Menu → Impostazioni → Sincronizzazione WiFi → **Avvia Server**

#### Step 4: Connetti Desktop
- Su Desktop: Menu → Impostazioni → Sincronizzazione WiFi
- IP: `localhost` o `127.0.0.1`
- Porta: `8765`
- Premi: **Test Connessione**

#### Step 5: Sincronizza
- Direzione: **Desktop → Mobile**
- Modalità: **SOSTITUISCI** (prima volta) o **UNISCI** (aggiornamenti)
- Premi: **Sincronizza**

---

## 📁 DOCUMENTI DISPONIBILI

### Guide Complete

1. **`TEST_SYNC_PLAN.md`** - Piano test completo con 10 scenari
   - Test 1-4: Sync base emulatore/cellulare
   - Test 5-10: Scenari avanzati (duplicati, multi-conto, stress test)
   - Template report finale
   - Troubleshooting dettagliato

2. **`QUICK_TEST_SYNC.md`** - Guida rapida 5 minuti
   - Test emulatore express
   - Test cellulare fisico via hotspot
   - Comandi essenziali
   - Fix problemi comuni

3. **`SYNC_STRATEGY.md`** - Architettura tecnica
   - Flusso sincronizzazione
   - Modelli sync (3 modalità)
   - Gestione duplicati
   - Avvisi classificazioni

4. **`SETUP_TEST_SYNC.md`** (questo file) - Riepilogo setup

### Script Automazione

- **`test-sync.bat`** - Utility test completa
  - 9 operazioni automatizzate
  - Menu interattivo
  - Test end-to-end

---

## 🧪 SCENARI DI TEST PRIORITARI

### Priorità ALTA (Testa subito!)

#### Test A: Desktop → Emulatore (Port Forward)
```
Setup: Emulatore stesso PC
Rete: localhost:8765 via adb forward
Direzione: Desktop → Mobile
Modalità: SOSTITUISCI
Dati: 3-5 transazioni test
Durata: 2-3 minuti
```

**Obiettivo**: Verificare che sync base funzioni

#### Test B: Emulatore → Desktop
```
Setup: Crea 2-3 transazioni su mobile
Direzione: Mobile → Desktop
Modalità: UNISCI
Durata: 2-3 minuti
```

**Obiettivo**: Verificare sync bidirezionale

### Priorità MEDIA (Dopo test A e B)

#### Test C: Cellulare Fisico via Hotspot
```
Setup: Hotspot cellulare attivo, PC connesso
IP: 192.168.43.1 (tipico Android)
Direzione: Desktop → Cellulare
Modalità: SOSTITUISCI
Durata: 5-10 minuti (include setup hotspot)
```

**Obiettivo**: Verificare sync WiFi reale

#### Test D: Multi-Conto
```
Setup: 2 conti correnti su Desktop
Direzione: Desktop → Mobile
Conti: Seleziona tutti
Modalità: SOSTITUISCI
```

**Obiettivo**: Verificare sync multi-account

### Priorità BASSA (Opzionale)

- Test duplicati
- Test stress (1000+ transazioni)
- Test ripristino backup
- Test classificazioni

---

## 🛠️ COMANDI ESSENZIALI

### ADB Path
```batch
SET ADB=C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe
```

### Verifica Dispositivi
```bash
%ADB% devices
```

### Port Forwarding (CRITICO per emulatore!)
```bash
%ADB% forward tcp:8765 tcp:8765
```

### Avvia App
```bash
%ADB% shell am start -n com.moneymind.app/crc64399a401da9d21b0a.MainActivity
```

### Log App
```bash
%ADB% logcat -d -s "MoneyMindApp:V"
```

### Test Connessione
```bash
curl http://localhost:8765/ping
```

### Screenshot Debug
```bash
%ADB% shell screencap -p /sdcard/screen.png
%ADB% pull /sdcard/screen.png screenshot.png
```

---

## 🔍 VERIFICA PRE-TEST

Prima di iniziare, verifica:

### Emulatore
- [ ] Emulatore attivo (`adb devices` mostra device)
- [ ] App installata (`adb shell pm list packages | findstr moneymind`)
- [ ] Port forwarding configurato (`adb forward tcp:8765 tcp:8765`)

### Desktop
- [ ] MoneyMind Desktop funzionante
- [ ] Dati test creati (3-5 transazioni)
- [ ] Dialog WiFi Sync accessibile (Menu → Impostazioni)

### Cellulare Fisico (per test hotspot)
- [ ] Hotspot attivo
- [ ] PC connesso all'hotspot
- [ ] IP hotspot annotato (`ipconfig` su Windows)
- [ ] Ping funziona (`ping 192.168.43.1`)

---

## 📊 ENDPOINT API MOBILE

Server mobile espone i seguenti endpoint:

### `GET /ping`
```json
{
  "status": "ok",
  "timestamp": "2025-01-25T14:30:00",
  "device": "Pixel 5",
  "platform": "Android"
}
```

### `GET /info`
```json
{
  "appName": "MoneyMind",
  "appVersion": "1.0.0",
  "device": "Pixel 5",
  "platform": "Android",
  "serverPort": 8765,
  "isRunning": true
}
```

### `GET /accounts`
```json
{
  "success": true,
  "accounts": [
    {
      "id": 1,
      "nome": "Conto Principale",
      "saldoIniziale": 1000.00,
      "transactionCount": 25,
      "latestTransactionDate": "2025-01-20"
    }
  ]
}
```

### `GET /transactions/{accountId}`
```json
{
  "success": true,
  "accountId": 1,
  "transactions": [
    {
      "data": "2025-01-20",
      "descrizione": "Spesa supermercato",
      "causale": "",
      "importo": -50.00,
      "createdAt": "2025-01-20T14:30:00"
    }
  ],
  "count": 25
}
```

### `POST /sync/prepare`
**Request**:
```json
{
  "direction": "DesktopToMobile",
  "mode": "Replace",
  "sourceAccounts": [
    {
      "id": 1,
      "nome": "Conto 1",
      "transactionCount": 10,
      "latestTransactionDate": "2025-01-20"
    }
  ]
}
```

**Response**:
```json
{
  "success": true,
  "backupCreated": true,
  "backupPath": "/storage/emulated/0/Android/data/.../backups/...",
  "comparisons": [
    {
      "accountId": 1,
      "sourceTransactionCount": 10,
      "destTransactionCount": 5,
      "hasWarning": true,
      "warningMessage": "Dest ha dati più recenti!"
    }
  ],
  "requiresConfirmation": true
}
```

### `POST /sync/execute`
**Request**:
```json
{
  "direction": "DesktopToMobile",
  "mode": "Replace",
  "confirmed": true,
  "accounts": [
    {
      "id": 1,
      "nome": "Conto 1",
      "transactions": [
        {
          "data": "2025-01-20",
          "descrizione": "Test",
          "causale": "",
          "importo": -50.00
        }
      ]
    }
  ]
}
```

**Response**:
```json
{
  "success": true,
  "results": [
    {
      "accountId": 1,
      "accountName": "Conto 1",
      "status": "replaced",
      "previousTransactionCount": 5,
      "newTransactionCount": 10,
      "duplicatesSkipped": 0
    }
  ],
  "totalTransactionsProcessed": 10,
  "totalDuplicatesSkipped": 0,
  "totalNewAdded": 10,
  "message": "Sostituite transazioni in 1/1 conti"
}
```

---

## 🐛 TROUBLESHOOTING RAPIDO

### ❌ Desktop non si connette
**Fix**: Verifica port forwarding
```bash
adb forward tcp:8765 tcp:8765
```
Poi usa `localhost:8765` su Desktop

### ❌ Server mobile non raggiungibile
**Fix**:
1. Riavvia app mobile
2. Riavvia server nell'app (Stop → Start)
3. Verifica log: `adb logcat -d | findstr WiFi`

### ❌ App crasha all'avvio server
**Fix**:
1. Verifica permissions (Network)
2. Controlla log: `adb logcat -d -s "AndroidRuntime:E"`
3. Reinstalla app se necessario

### ❌ Hotspot cellulare non funziona
**Fix**:
1. Verifica IP cellulare: Impostazioni → Info → IP
2. Ping da PC: `ping [IP]`
3. Disattiva firewall temporaneamente
4. Prova IP alternativo: `192.168.137.1` o `172.20.10.1`

---

## 📋 CHECKLIST FINALE

Prima di dichiarare "Sync Funzionante", verifica:

### Funzionalità Core
- [ ] Desktop → Emulatore funziona
- [ ] Emulatore → Desktop funziona
- [ ] Desktop → Cellulare (hotspot) funziona
- [ ] Cellulare → Desktop (hotspot) funziona

### Modalità Sync
- [ ] SOSTITUISCI funziona
- [ ] UNISCI funziona
- [ ] SOLO NUOVE funziona

### Sicurezza
- [ ] Backup creato SEMPRE prima di sync
- [ ] Avviso mostrato se dati più recenti su dest
- [ ] Avviso classificazioni (Desktop) funziona

### UX
- [ ] Progress bar visibile durante sync
- [ ] Messaggi chiari (successo/errore)
- [ ] Report finale accurato (N transazioni sync'd)

### Performance
- [ ] Sync < 1 sec per 100 transazioni
- [ ] Nessun freeze UI
- [ ] Nessun crash

---

## 🎉 PROSSIMI PASSI

Dopo aver completato i test:

1. **✅ Compila Report Test**
   - Usa template in `TEST_SYNC_PLAN.md`
   - Includi screenshot
   - Documenta problemi riscontrati

2. **📝 Aggiorna STATO_ARTE.md**
   ```markdown
   ## ✅ Sincronizzazione WiFi - TESTATA

   - Desktop → Emulatore: ✅ Funziona
   - Emulatore → Desktop: ✅ Funziona
   - Desktop → Cellulare: ✅ Funziona
   - Cellulare → Desktop: ✅ Funziona

   Data Test: [DATA]
   Transazioni Test: [N]
   Problemi: [Nessuno / Lista]
   ```

3. **🚀 Procedi con Fase Successiva**
   - Vedi `ROADMAP.md` per prossima fase
   - Se sync OK → Procedi con Deployment
   - Se problemi → Fixa bug e ri-testa

---

## 📞 SUPPORTO

### Log Files
- **Mobile**: `adb logcat -d -s "MoneyMindApp:V"`
- **Desktop**: `C:\Users\rober\AppData\Local\MoneyMind\Logs\app.log`

### Comandi Debug
```bash
# Verifica porta aperta
netstat -an | findstr ":8765"

# Test endpoint
curl http://localhost:8765/ping
curl http://localhost:8765/info

# Log completo sync
adb logcat -d -s "MoneyMindApp:V" | findstr /i "sync"
```

---

**Versione**: 1.0
**Data**: 25/11/2025
**Status**: ✅ Pronto per Test

**Inizia con**: `test-sync.bat` → Opzione 8 → "Test Completo Desktop → Mobile"

Buon test! 🚀
