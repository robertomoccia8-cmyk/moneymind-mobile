# TEST PLAN - Sincronizzazione WiFi Desktop ↔ Mobile

## 📋 PANORAMICA

Questo documento descrive il piano di test per verificare la sincronizzazione bidirezionale tra:
- **Desktop**: MoneyMind WPF (VB.NET) - `C:\Users\rober\Documents\MoneyMind\`
- **Mobile**: MoneyMindApp MAUI (C#) - `C:\Users\rober\Documents\MoneyMindApp\`

### Scenari di Test

1. **Test Emulatore** (stesso PC + hotspot virtuale)
2. **Test Cellulare Fisico** (via hotspot mobile)

---

## ⚙️ PREREQUISITI

### Software Richiesto

- ✅ **Desktop App**: MoneyMind Desktop già installata e funzionante
- ✅ **Mobile App**: MoneyMindApp installata su emulatore Android
- ✅ **ADB**: Android Debug Bridge configurato
  - Path: `C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe`

### Configurazione Rete

#### Scenario 1: Emulatore (stesso PC)
- **Emulatore**: IP tipico `10.0.2.15` (rete interna Android)
- **Host Machine (Desktop)**: Accessibile dall'emulatore via `10.0.2.2`
- **PROBLEMA**: L'emulatore è in rete NAT, NON raggiungibile direttamente dal desktop
- **SOLUZIONE**: Usare **port forwarding** con `adb reverse`

#### Scenario 2: Cellulare Fisico (via hotspot)
- **Cellulare**: Attiva hotspot WiFi → tipico IP `192.168.43.1` (Android)
- **Desktop**: Si connette all'hotspot del cellulare
- **Vantaggi**: Rete diretta, nessun problema di NAT

---

## 🔬 TEST 1: EMULATORE → DESKTOP

### Setup

1. **Avvia Emulatore Android**
   ```bash
   # Verifica che l'emulatore sia in esecuzione
   "C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" devices
   ```

2. **Configura Port Forwarding (Mobile → Desktop)**
   ```bash
   # Redireziona porta 8765 dall'host all'emulatore
   # Questo permette al desktop di raggiungere il server mobile via localhost:8765
   "C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe" forward tcp:8765 tcp:8765
   ```

3. **Crea Dati di Test sul DESKTOP**
   - Apri MoneyMind Desktop
   - Crea 5 transazioni di test:
     ```
     Data: 01/01/2025 | Importo: +1500 € | Descrizione: "Stipendio Test"
     Data: 02/01/2025 | Importo: -50 € | Descrizione: "Spesa Supermercato"
     Data: 03/01/2025 | Importo: -30 € | Descrizione: "Benzina"
     Data: 04/01/2025 | Importo: -15 € | Descrizione: "Caffè"
     Data: 05/01/2025 | Importo: -100 € | Descrizione: "Bolletta Luce"
     ```

### Esecuzione Test Desktop → Emulatore

#### Passo 1: Avvia Server Mobile

1. **Apri MoneyMindApp su emulatore**
2. **Vai su**: Menu → Impostazioni → Sincronizzazione WiFi
3. **Premi**: "Avvia Server"
4. **Annota l'IP**: Dovrebbe mostrare qualcosa tipo `10.0.2.15` (ma useremo `localhost:8765`)

#### Passo 2: Connetti Desktop a Mobile

1. **Apri MoneyMind Desktop**
2. **Vai su**: Menu → Impostazioni → Sincronizzazione WiFi
3. **Inserisci IP**: `127.0.0.1` o `localhost` (grazie al port forward)
4. **Porta**: `8765`
5. **Premi**: "Test Connessione" → Dovrebbe dire ✅ "Connesso!"

#### Passo 3: Esegui Sincronizzazione Desktop → Mobile

1. **Seleziona Direzione**: Desktop → Mobile
2. **Seleziona Modalità**:
   - **SOSTITUISCI** (per primo test, mobile è vuoto)
3. **Seleziona Conti**: Tutti i conti
4. **Premi**: "Sincronizza"
5. **Attendi**: Backup automatico + copia transazioni
6. **Verifica Risultato**: Desktop dovrebbe mostrare:
   ```
   ✅ Sincronizzazione Completata!

   Desktop → Mobile: 5 transazioni inviate
   Mobile → Desktop: 0 transazioni ricevute
   ```

#### Passo 4: Verifica su Mobile

1. **Apri MoneyMindApp su emulatore**
2. **Vai alla Dashboard**
3. **Verifica**:
   - Saldo totale aggiornato
   - 5 transazioni visibili
   - Date e importi corretti

#### Checkpoint Test 1 ✅

- [ ] Server mobile avviato correttamente
- [ ] Desktop si connette al server mobile (via port forward)
- [ ] 5 transazioni sincronizzate da Desktop → Emulatore
- [ ] Transazioni visibili nell'app mobile
- [ ] Backup automatico creato su mobile

---

## 🔬 TEST 2: DESKTOP → EMULATORE (Direzione Inversa)

### Setup

Stessi prerequisiti del Test 1, ma ora aggiungiamo dati sul MOBILE.

### Esecuzione Test Mobile → Desktop

#### Passo 1: Crea Nuove Transazioni su MOBILE

1. **Apri MoneyMindApp su emulatore**
2. **Vai su**: Transazioni → Aggiungi
3. **Crea 3 nuove transazioni**:
   ```
   Data: 06/01/2025 | Importo: -25 € | Descrizione: "Pizza Mobile"
   Data: 07/01/2025 | Importo: -40 € | Descrizione: "Farmacia Mobile"
   Data: 08/01/2025 | Importo: -10 € | Descrizione: "Parcheggio Mobile"
   ```

#### Passo 2: Sincronizza Mobile → Desktop

1. **Apri MoneyMind Desktop**
2. **Vai su**: Sincronizzazione WiFi
3. **IP**: `127.0.0.1:8765` (già configurato)
4. **Direzione**: Mobile → Desktop
5. **Modalità**: **UNISCI** (per mantenere le 5 transazioni esistenti sul Desktop)
6. **Premi**: "Sincronizza"
7. **Verifica Risultato**:
   ```
   ✅ Sincronizzazione Completata!

   Desktop → Mobile: 0 transazioni inviate
   Mobile → Desktop: 3 nuove transazioni importate
   ```

#### Passo 3: Verifica su Desktop

1. **Controlla elenco transazioni Desktop**
2. **Verifica**: Ora dovrebbe avere **8 transazioni totali**:
   - 5 originali create sul Desktop
   - 3 nuove importate dal Mobile

#### Checkpoint Test 2 ✅

- [ ] 3 transazioni create su mobile
- [ ] Sincronizzazione Mobile → Desktop completata
- [ ] Desktop ora ha 8 transazioni totali (5 + 3)
- [ ] Nessun duplicato creato (modalità UNISCI)
- [ ] Backup automatico creato su desktop

---

## 🔬 TEST 3: CELLULARE FISICO → DESKTOP (via Hotspot)

### Setup

#### Passo 1: Attiva Hotspot su Cellulare

1. **Sul cellulare**: Vai su Impostazioni → Hotspot WiFi
2. **Attiva hotspot**
3. **Annota**:
   - Nome rete (SSID): `_______________________`
   - Password: `_______________________`
   - IP hotspot (tipico): `192.168.43.1` (Android) o `172.20.10.1` (iOS)

#### Passo 2: Connetti Desktop all'Hotspot

1. **Sul PC**: Vai su Impostazioni WiFi
2. **Connetti all'hotspot del cellulare**
3. **Verifica connessione**:
   ```bash
   ping 192.168.43.1
   ```

#### Passo 3: Installa App su Cellulare

1. **Build & Deploy**:
   ```bash
   cd C:\Users\rober\Documents\MoneyMindApp
   dotnet build -c Release
   ```

2. **Installa APK su cellulare**:
   - Metodo 1: Via USB + ADB
   - Metodo 2: Copia APK su telefono + installa manualmente

### Esecuzione Test Desktop → Cellulare

#### Passo 1: Avvia Server su Cellulare

1. **Apri MoneyMindApp su cellulare fisico**
2. **Vai su**: Menu → Sincronizzazione WiFi
3. **Premi**: "Avvia Server"
4. **Annota IP**: Dovrebbe mostrare `192.168.43.1` (IP hotspot)
5. **Verifica**: Server attivo ✅

#### Passo 2: Connetti Desktop a Cellulare

1. **Apri MoneyMind Desktop** (collegato all'hotspot del cellulare)
2. **Vai su**: Sincronizzazione WiFi
3. **Inserisci IP**: `192.168.43.1` (IP hotspot del cellulare)
4. **Porta**: `8765`
5. **Premi**: "Test Connessione"
   - ✅ Dovrebbe mostrare: "Connesso! Server mobile raggiungibile"
   - ❌ Se fallisce: Verifica firewall/permissions

#### Passo 3: Esegui Sync Desktop → Cellulare

1. **Direzione**: Desktop → Mobile
2. **Modalità**: **SOSTITUISCI** (assumendo cellulare nuovo/vuoto)
3. **Seleziona conti**: Tutti
4. **Premi**: "Sincronizza"
5. **Attendi**: Completamento (può richiedere 10-30 sec)
6. **Risultato Atteso**:
   ```
   ✅ Desktop → Mobile: N transazioni sincronizzate
   ```

#### Passo 4: Verifica su Cellulare

1. **Apri MoneyMindApp**
2. **Vai alla Dashboard**
3. **Verifica**:
   - Tutte le transazioni presenti
   - Saldo corretto
   - Multi-conto sincronizzato (se configurato)

#### Checkpoint Test 3 ✅

- [ ] Hotspot attivo su cellulare
- [ ] Desktop connesso a hotspot
- [ ] App installata su cellulare fisico
- [ ] Server mobile avviato (IP: 192.168.43.1)
- [ ] Desktop si connette con successo
- [ ] Transazioni sincronizzate Desktop → Cellulare
- [ ] Verifica visiva su cellulare: dati corretti

---

## 🔬 TEST 4: CELLULARE → DESKTOP (via Hotspot)

### Esecuzione

#### Passo 1: Crea Dati su Cellulare

1. **Sul cellulare**: Apri MoneyMindApp
2. **Aggiungi nuove transazioni** (es. 5 transazioni diverse da quelle sul Desktop)

#### Passo 2: Sincronizza Cellulare → Desktop

1. **Assicurati**: Server mobile ancora attivo
2. **Sul Desktop**: Apri Sync Dialog
3. **Direzione**: Mobile → Desktop
4. **Modalità**: **UNISCI** (per evitare di perdere dati Desktop)
5. **Premi**: "Sincronizza"

#### Passo 3: Verifica Risultati

1. **Desktop**: Controlla che le nuove transazioni dal cellulare siano importate
2. **Verifica**: Nessun duplicato (algoritmo: stessa data + descrizione)

#### Checkpoint Test 4 ✅

- [ ] Transazioni create su cellulare
- [ ] Sincronizzazione Cellulare → Desktop completata
- [ ] Desktop riceve nuove transazioni
- [ ] Nessun duplicato creato (UNISCI)
- [ ] Backup automatico creato

---

## 🧪 TEST AVANZATI (Opzionali)

### Test 5: Modalità SOLO NUOVE

1. **Scenario**: Desktop ha transazioni fino al 15/01/2025
2. **Mobile**: Ha transazioni fino al 20/01/2025
3. **Sincronizza**: Mobile → Desktop, modalità **SOLO NUOVE**
4. **Atteso**: Solo transazioni dal 16/01 in poi vengono copiate

### Test 6: Avviso Classificazioni (Desktop)

1. **Scenario**: Desktop ha transazioni classificate con MacroCategoria/Categoria
2. **Sincronizza**: Mobile → Desktop, modalità **SOSTITUISCI**
3. **Atteso**: Desktop mostra avviso:
   ```
   ⚠️ ATTENZIONE!
   Hai N transazioni classificate che verranno perse!
   Vuoi procedere?
   ```
4. **Nota**: Mobile NON supporta classificazioni

### Test 7: Ripristino Backup

1. **Scenario**: Dopo una sync con SOSTITUISCI, utente vuole annullare
2. **Azione**: Vai su Backup → Ripristina ultimo backup
3. **Atteso**: Dati precedenti ripristinati
4. **Verifica**: Transazioni tornano allo stato pre-sync

### Test 8: Multi-Conto

1. **Desktop**: Configura 2 conti correnti (es. "Personale", "Aziendale")
2. **Crea transazioni** su entrambi i conti
3. **Sincronizza**: Desktop → Mobile (tutti i conti)
4. **Verifica Mobile**: Entrambi i conti visibili e con transazioni corrette

### Test 9: Gestione Duplicati

1. **Crea stessa transazione** su Desktop e Mobile:
   ```
   Data: 10/01/2025
   Importo: -50 €
   Descrizione: "Test Duplicato"
   ```
2. **Sincronizza**: Mobile → Desktop, modalità **UNISCI**
3. **Atteso**: Solo 1 transazione importata (algoritmo duplicati: data + descrizione identiche)
4. **Verifica**: Nessun doppio

### Test 10: Stress Test (Grandi Volumi)

1. **Scenario**: Desktop ha 1000+ transazioni
2. **Sincronizza**: Desktop → Mobile
3. **Misura**:
   - Tempo sincronizzazione
   - Memoria usata
   - Nessun crash
4. **Performance attesa**: ~30-60 sec per 1000 transazioni

---

## 📊 REPORT FINALE

### Template Report Test

```markdown
## Test Completato: [DATA]

### Ambiente
- Desktop App Version: _______
- Mobile App Version: _______
- Android Version: _______
- Scenario: [Emulatore / Cellulare Fisico]

### Test Eseguiti

#### Test 1: Desktop → Emulatore
- Status: ✅ / ❌
- Transazioni inviate: _______
- Durata: _______ sec
- Note: _______________________________

#### Test 2: Emulatore → Desktop
- Status: ✅ / ❌
- Transazioni ricevute: _______
- Duplicati rilevati: _______
- Note: _______________________________

#### Test 3: Desktop → Cellulare (Hotspot)
- Status: ✅ / ❌
- Hotspot IP: _______
- Connessione: ✅ / ❌
- Transazioni sincronizzate: _______
- Note: _______________________________

#### Test 4: Cellulare → Desktop (Hotspot)
- Status: ✅ / ❌
- Modalità: UNISCI / SOSTITUISCI / SOLO NUOVE
- Transazioni importate: _______
- Note: _______________________________

### Problemi Riscontrati

1. [Descrizione problema]
   - Severità: Alta / Media / Bassa
   - Workaround: _______________________________

2. [...]

### Conclusioni

- **Test Superati**: _____ / _____
- **Test Falliti**: _____ / _____
- **Raccomandazioni**: _______________________________

### Screenshot

[Allegare screenshot di:]
- Mobile: Server attivo con IP
- Desktop: Dialog sync connesso
- Mobile: Transazioni dopo sync
- Desktop: Report sync completato
```

---

## 🛠️ TROUBLESHOOTING

### Problema 1: Emulatore Non Raggiungibile da Desktop

**Sintomo**: Desktop non riesce a connettersi all'emulatore

**Soluzione**:
```bash
# Usa port forwarding
adb forward tcp:8765 tcp:8765

# Poi su Desktop, usa: localhost:8765
```

### Problema 2: Cellulare Hotspot - Desktop Non si Connette

**Sintomo**: "Impossibile connettersi al server mobile"

**Checklist**:
1. Verifica che il Desktop sia connesso all'hotspot WiFi del cellulare
2. Prova a fare ping all'IP del cellulare:
   ```bash
   ping 192.168.43.1
   ```
3. Verifica che il firewall del cellulare non blocchi la porta 8765
4. Riavvia il server mobile
5. Prova con IP alternativo (su Android: Impostazioni → Info → Stato → Indirizzo IP)

### Problema 3: Backup Non Creato

**Sintomo**: Sync parte ma nessun backup viene creato

**Soluzione**:
1. Verifica permissions storage
2. Controlla log app:
   ```bash
   adb logcat -d | findstr "Backup"
   ```
3. Libera spazio su dispositivo (backup richiede ~10-50 MB)

### Problema 4: Duplicati Non Rilevati Correttamente

**Sintomo**: Modalità UNISCI crea duplicati

**Causa**: Algoritmo duplicati usa **Data + Descrizione**
- Importo NON considerato (per gestire arrotondamenti)
- Causale NON considerata (spesso vuota)

**Soluzione**:
- Verifica che le descrizioni siano identiche (case-insensitive)
- Se necessario, usa modalità SOSTITUISCI invece di UNISCI

### Problema 5: Server Mobile Crasha

**Sintomo**: Server si arresta durante la sincronizzazione

**Debug**:
```bash
# Controlla log crash
adb logcat -d -s "AndroidRuntime:E"

# Controlla memoria disponibile
adb shell dumpsys meminfo com.moneymind.app
```

---

## 📝 CHECKLIST PRE-TEST

Prima di iniziare i test, verifica:

- [ ] App Desktop compilata e funzionante
- [ ] App Mobile compilata e installata su emulatore
- [ ] ADB configurato e raggiungibile
- [ ] Emulatore Android avviato
- [ ] Dati di test creati su Desktop (5-10 transazioni)
- [ ] Hotspot cellulare configurato (per test fisico)
- [ ] Spazio disco sufficiente per backup (~100 MB)
- [ ] WiFi/Hotspot attivi
- [ ] Porte firewall aperte (8765)

---

## 🎯 OBIETTIVI TEST

### Obiettivi Funzionali

- ✅ Sync Desktop → Mobile funziona
- ✅ Sync Mobile → Desktop funziona
- ✅ 3 modalità funzionano (SOSTITUISCI, UNISCI, SOLO NUOVE)
- ✅ Backup automatico creato prima di ogni sync
- ✅ Algoritmo duplicati funziona correttamente
- ✅ Multi-conto supportato
- ✅ Avviso classificazioni mostrato (Desktop)

### Obiettivi Non-Funzionali

- ⚡ Performance: < 1 sec per 100 transazioni
- 🔒 Sicurezza: Backup creato SEMPRE prima di SOSTITUISCI
- 📱 UX: Messaggi chiari, progress bar visibile
- 🌐 Rete: Funziona su WiFi casa + Hotspot mobile
- 💾 Memoria: Nessun memory leak

### Metriche di Successo

| Metrica | Target | Attuale |
|---------|--------|---------|
| Sync Success Rate | > 95% | _____ |
| Tempo medio sync (100 tx) | < 1 sec | _____ |
| Duplicati rilevati correttamente | 100% | _____ |
| Backup creati | 100% | _____ |
| Crash durante sync | 0% | _____ |

---

## 📞 CONTATTI & SUPPORTO

Se riscontri problemi durante i test:

1. **Controlla log**:
   ```bash
   # Mobile
   adb logcat -d -s "MoneyMindApp:V"

   # Desktop
   Apri C:\Users\rober\AppData\Local\MoneyMind\Logs\app.log
   ```

2. **Crea issue GitHub** con:
   - Descrizione problema
   - Log completo
   - Screenshot
   - Passi per riprodurre

3. **Documenta** tutto nel report finale

---

**Versione**: 1.0
**Data Creazione**: 25/11/2025
**Autore**: Claude Code
**Status**: ✅ Pronto per esecuzione

---

## 🚀 PROSSIMI PASSI

Dopo aver completato tutti i test:

1. **Compila Report Finale** (template sopra)
2. **Crea Demo Video** (registra sync in azione)
3. **Aggiorna STATO_ARTE.md** con esito test
4. **Se tutto OK**: Procedi con deployment (vedi `DEPLOYMENT.md`)
5. **Se problemi**: Apri issue e fixa bug prioritari

Buon test! 🎉
