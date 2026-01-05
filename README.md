# MoneyMindApp - Mobile Multi-Platform

App mobile moderna per gestione finanze personali costruita con **.NET MAUI 8.0**.

## 🚀 Quick Start

### Prerequisiti
- **Windows**: Visual Studio 2022 17.8+ con workload ".NET Multi-platform App UI"
- **Android Studio**: Già installato (SDK, Emulator)
- **.NET 8.0 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0

### Setup Iniziale

```bash
# 1. Crea progetto MAUI
cd "C:\Users\rober\Documents\MoneyMindApp"
dotnet new maui -n MoneyMindApp -f net8.0

# 2. Installa pacchetti
dotnet add package sqlite-net-pcl --version 1.9.172
dotnet add package SQLitePCLRaw.bundle_green --version 2.1.8
dotnet add package CommunityToolkit.Mvvm --version 8.4.0
dotnet add package CommunityToolkit.Maui --version 9.0.0
dotnet add package LiveChartsCore.SkiaSharpView.Maui --version 2.0.0-rc3.3
dotnet add package ClosedXML --version 0.104.1

# 3. Build & Run Android
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

### Verifica Android SDK
```bash
# Controlla path SDK (deve essere configurato)
echo %ANDROID_HOME%
# Output atteso: C:\Users\rober\AppData\Local\Android\Sdk

# Lista emulatori disponibili
emulator -list-avds

# Avvia emulator (se non già avviato)
emulator -avd Pixel_5_API_30 &

# Verifica dispositivi connessi
adb devices
```

## 📱 Architettura

### Stack Tecnologico
- **.NET MAUI 8.0** - Framework multi-piattaforma
- **SQLite** - Database locale (compatibile con desktop)
- **MVVM Pattern** - CommunityToolkit.Mvvm
- **LiveCharts** - Grafici moderni
- **Material Design 3** - UI/UX guidelines

### Database Condiviso Desktop/Mobile
- Stesso schema identico applicazione desktop
- Path cross-platform (Android: `/data/data/.../files/`)
- No classificazioni (Pattern/MacroCategoria rimossi per mobile)
- Sincronizzazione futura via cloud

## 🎨 Design System

### Palette Colori (Material 3)
```
Primary:   #6750A4 (Purple)
Success:   #2E7D32 (Green - Entrate)
Error:     #BA1A1A (Red - Uscite)
Surface:   #FFFBFE (Light) / #1C1B1F (Dark)
```

### Typography
- Font principale: **Inter** (Google Fonts)
- Headline: 32sp Bold
- Body: 14sp Regular
- Caption: 12sp Regular

### Components
- **Cards** con elevation 2dp, corner radius 12dp
- **FAB** (Floating Action Button) per azioni primarie
- **Bottom Navigation** con 4-5 tab principali
- **Swipe Actions** su liste (elimina, modifica)
- **Pull-to-refresh** su tutte le liste
- **Skeleton loaders** durante caricamento dati

## 🏗️ Struttura App

### Navigazione (Shell)
```
AppShell
├── Dashboard (MainPage)           # Stats + Grafico ultimi 7 giorni
├── Transazioni (TransactionsPage) # Lista + Filtri + Ricerca
├── Strumenti (ToolsPage)          # Grid: Import, Export, Duplicati, Stipendi
├── Analisi (AnalyticsPage)        # Charts dettagliati
└── Impostazioni (SettingsPage)    # Settings + Admin + Aggiornamenti
```

### Dashboard (MainPage) - Schermat Principale
**Poche schermate, tante info**:

#### Header
- Logo + Nome conto attivo (tap per cambiare)
- Icona notifiche (badge aggiornamenti disponibili)

#### Stats Cards (4 card in grid 2x2)
```
┌─────────────────┬─────────────────┐
│ 💰 Saldo Totale │ 📈 Entrate Mese │
│   €2.450,00     │   €1.800,00     │
│   +5% vs mese   │   +120 vs mese  │
└─────────────────┴─────────────────┘
┌─────────────────┬─────────────────┐
│ 📉 Uscite Mese  │ 💾 Risparmio    │
│   €1.320,00     │   €480,00       │
│   -8% vs mese   │   27% di entrate│
└─────────────────┴─────────────────┘
```

#### Mini Chart
- Grafico linea ultimi 7 giorni (saldo giornaliero)
- Swipe left/right per navigare settimane

#### Ultime Transazioni (5 recenti)
- Lista compatta con tap per dettaglio
- Button "Vedi tutte" → TransactionsPage

#### FAB (Floating Action Button)
- **+** → Aggiungi transazione (modale rapido)

### TransactionsPage - Lista Intelligente
**Info dense, UX fluida**:

#### SearchBar + Chip Filters
- Ricerca live (descrizione, importo)
- Chip: "Tutte", "Entrate", "Uscite", "Questo mese", "Personalizza"
- Tap "Personalizza" → Bottom sheet filtri avanzati:
  - Periodo: DateRangePicker
  - Importo: Slider min/max
  - Ordinamento: Data, Importo, Descrizione

#### Lista Virtualized
```
[Data]        [Descrizione]           [Importo]
15 Ott 2025   Stipendio Acme Inc      +€1.800,00 🟢
14 Ott 2025   Spesa Esselunga         -€45,20    🔴
12 Ott 2025   Bolletta Enel           -€98,00    🔴
```
- SwipeView:
  - ← Swipe left = Elimina (conferma)
  - → Swipe right = Modifica (modale edit)
- Long press = Menu contestuale (Duplica, Dettagli)

#### Statistiche Bottom Bar
- "Totale visualizzato: €X | N transazioni"
- Aggiornato real-time con filtri

### ToolsPage - Hub Strumenti
**Grid 2x2 con card grandi**:
```
┌──────────────────┬──────────────────┐
│   📥 Importa     │   📤 Esporta     │
│   File bancari   │   CSV/Excel/PDF  │
│                  │                  │
└──────────────────┴──────────────────┘
┌──────────────────┬──────────────────┐
│   🔍 Duplicati   │   💰 Stipendi    │
│   Rileva duplicati│   Config periodi │
│                  │                  │
└──────────────────┴──────────────────┘
```
- Ogni card = tap → navigazione tool specifico
- Badge con info (es: "3 duplicati trovati")

### AnalyticsPage - Dashboard Grafici
**Tabbed interface con 3 tab**:

#### Tab 1: Mese Corrente
- Grafico barre: Entrate vs Uscite (giorno per giorno)
- Card stats: Media giornaliera, Max spesa singola, Categorie top (future)

#### Tab 2: Anno
- Grafico linea: Risparmio mensile (12 mesi)
- Grafico barre: Entrate/Uscite mensili
- Slider anno (navigazione anni passati)

#### Tab 3: Confronto
- Selector 2 anni (dropdown)
- Grafico sovrapposto: Anno 1 vs Anno 2
- Card delta: "Risparmiato +€X rispetto a [anno]"

### SettingsPage - Centro Controllo
**Sections collapsible**:

#### Sezione Conto
- Lista conti (swipe per eliminare, tap per switch)
- Button "Aggiungi Conto"

#### Sezione Licenza
- Badge subscription (Base/Plus/Admin)
- Email registrata
- Scadenza (se temporanea)
- Button "Gestisci Licenza" → web view admin panel

#### Sezione Tema
- Selector: Light / Dark / Auto (sistema)
- Preview colori in tempo reale

#### Sezione Avanzate
- Backup automatico (toggle + frequenza)
- Esporta database (share .db file)
- Cancella cache
- Log debug (solo Admin)

#### Sezione Info
- Versione app (tap 7 volte → Easter egg)
- Check aggiornamenti (badge se disponibili)
- Privacy policy (web view)
- Licenze open source

## 🔐 Sicurezza

### Beta License
- Verifica all'avvio (grace 7 giorni offline)
- Cache criptata (SecureStorage)
- Fingerprint dispositivo (Model + Manufacturer + Name)

### Database
- Opzionale: SQLCipher per encryption at rest
- Backup criptato prima upload cloud

### API Calls
- Timeout 10s
- Retry policy (3 tentativi)
- Fallback offline graceful

## 🎯 Funzionalità Chiave

### 1. Statistiche Real-Time
- Calcolo saldo senza classificazioni
- Aggiornamento live con data binding
- Comparazioni periodo precedente

### 2. Import Intelligente
- Auto-detect formato CSV (configurazioni salvate)
- Mapping colonne visuale (drag & drop)
- Preview pre-import (prime 10 righe)
- Salvataggio template per banca

### 3. Duplicate Detection
- Algoritmo: Data + Importo ± €0.01 + Levenshtein(Desc) > 80%
- Raggruppamento intelligente
- Selezione multipla per merge

### 4. Periodi Stipendiali
- Visualizzazione calendario con evidenziazione giorni
- Preview impatto su statistiche in real-time
- Eccezioni mesi specifici (Natale, ecc.)

### 5. Export Flessibile
- Formati: CSV (standard), Excel (formattato), PDF (stampabile)
- Filtri export (periodo, conto, range importi)
- Share nativo (WhatsApp, Email, Google Drive)

## 📊 Performance

### Ottimizzazioni
- **Virtualization**: Liste 1000+ elementi senza lag
- **Caching**: Statistiche in-memory (5 min TTL)
- **Pagination**: Caricamento incrementale (50 item/pagina)
- **Lazy Loading**: Immagini e chart su-demand
- **Database Indexing**: Index su Data, Importo

### Target Performance
- Cold start: < 2s
- Navigation: < 100ms
- Query 1000 transazioni: < 300ms
- Chart rendering: < 500ms

## 🧪 Testing

### Unit Tests (da implementare)
```bash
dotnet test MoneyMindApp.Tests/
```

### UI Test (manuale Android Emulator)
1. Avvia Pixel 5 API 30
2. Deploy app: `dotnet run -f net8.0-android`
3. Test scenari:
   - Dashboard load con 1000+ transazioni
   - Import CSV 500 righe
   - Switch tra 3 conti
   - Offline mode (airplane mode)
   - Rotate screen (portrait/landscape)

## 🚢 Deployment

### Android APK (Debug)
```bash
dotnet publish -f net8.0-android -c Debug
# Output: bin/Debug/net8.0-android/publish/com.moneymind.app-Signed.apk
```

### Android AAB (Release - Google Play)
```bash
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab
# Firma con keystore: jarsigner -keystore moneymind.keystore app.aab
```

### Windows MSIX (Desktop)
```bash
dotnet publish -f net8.0-windows10.0.19041.0 -c Release
# Output: bin/Release/net8.0-windows/publish/MoneyMindApp.msix
```

## 🐛 Troubleshooting

### Errore: "Android SDK not found"
```bash
# Imposta variabile ambiente
set ANDROID_HOME=C:\Users\rober\AppData\Local\Android\Sdk
set PATH=%PATH%;%ANDROID_HOME%\emulator;%ANDROID_HOME%\platform-tools
```

### Errore: "SQLite table not found"
- Elimina database: `adb shell rm /data/data/com.moneymind.app/files/MoneyMind_*.db`
- Riavvia app (schema ricreato automaticamente)

### Errore: "License verification failed"
- Verifica connessione internet
- Controlla cache licenza: `await SecureStorage.GetAsync("license_token")`
- Fallback grace period 7 giorni

## 📚 Risorse

### Documentazione
- [.NET MAUI Docs](https://learn.microsoft.com/dotnet/maui/)
- [Material Design 3](https://m3.material.io/)
- [LiveCharts](https://livecharts.dev/)

### Conversione VB → C#
- [CodeConvert.ai](https://www.codeconvert.ai/vb-to-csharp-converter)
- [Telerik Converter](https://converter.telerik.com/)

### Design Assets
- [Material Icons](https://fonts.google.com/icons)
- [Undraw Illustrations](https://undraw.co/)

## 🤝 Prossimi Step

1. **Apri nuova sessione Claude** in `MoneyMindApp/`
2. **Comando**: "Inizia FASE 1 da ROADMAP.md: setup progetto + DatabaseService"
3. **Segui**: ROADMAP.md per implementazione step-by-step
4. **Riferimenti**: CLAUDE.md (architettura), PROJECT_STRUCTURE.md (file organization)

---

**Nota**: Questo è un progetto separato dall'app desktop. L'app desktop continua a funzionare indipendentemente in `C:\Users\rober\Documents\MoneyMind`.
