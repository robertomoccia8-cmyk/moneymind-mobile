# Privacy Policy - MoneyMindApp

**Ultima Modifica**: 2025-01-XX
**Versione**: 1.0

---

## 📋 Informazioni Generali

MoneyMind è un'applicazione per la gestione delle finanze personali che rispetta la tua privacy.

**Principio Fondamentale**: I tuoi dati finanziari restano **SOLO sul tuo dispositivo**. Non vengono mai caricati su server cloud di terze parti.

---

## 🔐 Dati Raccolti

### 1. Dati Finanziari (100% Locali)
- **Transazioni**: Data, descrizione, importo, causale
- **Conti correnti**: Nome, saldo iniziale, icona, colore
- **Configurazioni**: Periodo stipendiale, eccezioni mesi
- **Impostazioni**: Tema, valuta, preferenze UI

**Storage**: Database SQLite locale (`/data/data/com.moneymind.app/files/`)
**Cloud**: **MAI caricati** su server esterni
**Backup**: Solo su richiesta utente (export file locale o sync WiFi)

### 2. Beta License Data (Minimi Necessari)
- **Email**: Per verifica licenza beta
- **Device Fingerprint**: Hash anonimo (CPU+Model+MAC) per prevenire condivisione licenza
- **Activation Date**: Data attivazione
- **Subscription Type**: Base/Plus/Admin

**Storage**: Google Sheets (backend beta testers) - **SOLO license data, NO transazioni finanziarie**
**Uso**: Validazione licenza all'avvio app
**Retention**: Fino a revoca licenza o fine beta

### 3. Dati Tecnici (Anonimi)
- **Crash Reports**: Stack trace errori (se abilitato)
- **App Version**: Versione app installata
- **Device Info**: Modello, OS version (anonimi)

**Storage**: App Center / Firebase Crashlytics (se implementato)
**Uso**: Migliorare stabilità app
**Opt-Out**: Disabilitabile in Settings

### 4. Analytics (Opzionali)
- **Eventi**: Pagine visitate, funzionalità usate
- **Timing**: Durata sessioni

**Storage**: Firebase Analytics (se implementato)
**Uso**: Capire come migliorare UX
**Opt-Out**: Disabilitabile in Settings
**NO dati sensibili**: MAI trackato importo transazioni, descrizioni, etc.

---

## 🚫 Dati NON Raccolti

- ❌ Importi transazioni
- ❌ Descrizioni transazioni
- ❌ Causali
- ❌ Saldi conti
- ❌ Password/PIN
- ❌ Posizione GPS
- ❌ Contatti
- ❌ Fotocamera (tranne se usi scan ricevute - opzionale)
- ❌ Microfono

---

## 🔒 Come Proteggiamo i Tuoi Dati

### Encryption
- **Database**: Opzionale SQLCipher encryption (AES-256)
- **API Keys**: SecureStorage (Keychain iOS / KeyStore Android)
- **Biometric**: Face ID / Touch ID per bloccare app
- **Network**: HTTPS only per API calls

### Local-Only Architecture
- Database SQLite nel sandbox app
- No cloud backup automatico
- Sync WiFi: solo rete locale (no internet)
- Export file: controllo utente completo

### Auto-Lock
- App si blocca dopo 5 minuti inattività
- Richiede Face ID / Touch ID per sblocco

---

## 🌍 Sincronizzazione Desktop-Mobile

### WiFi Sync
- **Come funziona**: App mobile crea server HTTP locale, desktop si connette via WiFi
- **Dati trasmessi**: Solo su rete locale (LAN), MAI internet
- **Encryption**: HTTP (LAN trusted) - opzionale HTTPS se implementato
- **Storage**: Nessun server intermedio, diretto device-to-device

### File Export/Import
- **Formato**: `.mmsync` (JSON compresso)
- **Trasferimento**: Manuale (USB, Email, Share)
- **Encryption**: Opzionale password-protected backup

---

## 📱 Permissions Richieste

| Permission | Uso | Quando | Opt-In |
|------------|-----|--------|--------|
| **Storage (Read)** | Importare file CSV bancari | Tap "Importa" | Obbligatorio per import |
| **Storage (Write)** | Esportare file Excel/CSV | Tap "Esporta" | Obbligatorio per export |
| **Internet** | Beta license check, updates | Avvio app | Auto-granted |
| **WiFi State** | WiFi Sync | Tap "Sincronizza" | Auto-granted |
| **Biometric** | Face ID / Touch ID | Setup iniziale | Opzionale |
| **Camera** | Scan ricevute (se implementato) | Tap "Scan" | Opzionale |

**Nota**: Permissions richieste solo quando necessarie (just-in-time).

---

## 🔄 Condivisione Dati con Terze Parti

### Google (Beta License Only)
- **Cosa**: Email, device fingerprint, activation date
- **Perché**: Verifica licenza beta
- **Quando**: Avvio app (ogni 7 giorni se offline)
- **Privacy**: Google Sheets privato, accesso limitato admin
- **Opt-Out**: Non possibile durante beta (obbligatorio per uso app)

### Nessun'altra Terza Parte
- ❌ No advertising networks
- ❌ No analytics trackers (se non abilitati da utente)
- ❌ No social media SDKs
- ❌ No data brokers

---

## 🇪🇺 GDPR Compliance (EU Users)

### I Tuoi Diritti

1. **Right to Access**: Esporta tutti i tuoi dati (Export Excel/JSON)
2. **Right to Rectification**: Modifica dati direttamente nell'app
3. **Right to Erasure**: Settings → "Elimina Tutti i Dati"
4. **Right to Data Portability**: Export formato JSON standard
5. **Right to Object**: Disabilita analytics/crash reports in Settings

### Data Controller
**Nome**: [TUO NOME/AZIENDA]
**Email**: privacy@moneymind.app (da configurare)
**Paese**: Italia

### Data Retention
- **Transazioni**: Fino a eliminazione manuale utente
- **Beta License**: Fino a fine beta o revoca
- **Crash Logs**: 90 giorni
- **Analytics**: 14 mesi (Google default)

---

## 👶 Children's Privacy

MoneyMind **NON è destinata a minori di 13 anni**. Non raccogliamo consapevolmente dati di bambini. Se sei genitore e scopri che tuo figlio ha fornito dati, contattaci per rimozione immediata.

---

## 🔔 Modifiche alla Privacy Policy

Ti notificheremo cambiamenti significativi tramite:
- Notifica in-app all'apertura
- Email (se fornita per beta license)
- Banner nella schermata Settings

**Ultima modifica mostrata** in Settings → Privacy Policy.

---

## 📧 Contatti

Per domande su questa Privacy Policy:

**Email**: privacy@moneymind.app
**GitHub Issues**: https://github.com/[username]/moneymind/issues
**Support**: support@moneymind.app

**Risposta entro**: 48 ore (giorni lavorativi)

---

## ✅ Accettazione

Usando MoneyMind, accetti questa Privacy Policy.

Se non accetti, **non usare l'app** e contattaci per eliminazione dati eventuali.

---

## 📄 Versioni Precedenti

- **v1.0** (2025-01-XX): Prima release

---

**Questo è un template**. Personalizza con:
- Nome azienda/developer
- Email contatto reali
- Link GitHub repository
- Dettagli specifici implementazione analytics/crash reporting
- Legal counsel review (consigliato!)
