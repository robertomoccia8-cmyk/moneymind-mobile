# 🚀 START HERE - MoneyMindApp Mobile

## 📍 Sei Qui
Questa è la directory del **nuovo progetto mobile** MoneyMindApp.

**Path**: `C:\Users\rober\Documents\MoneyMindApp`

---

## 📂 Cosa Trovi Qui

### 📘 Documentazione (Leggi Questi File)
1. **README.md** - Panoramica generale progetto + Quick setup
2. **CLAUDE.md** - ⭐ **LEGGI SEMPRE PRIMA!** Istruzioni complete per Claude, mapping Desktop→Mobile
3. **QUICK_START.md** - ⚡ Step 1-9 per creare MVP funzionante (Dashboard base)
4. **ROADMAP.md** - Piano completo 11 settimane (Fase 1→11)
5. **PROJECT_STRUCTURE.md** - Architettura dettagliata + patterns
6. **UI_UX_GUIDELINES.md** - Design system moderno (Material 3)
7. **FILES_TO_CREATE.md** - Checklist 86 files da creare

### 📁 Struttura Cartelle (Vuote, da Popolare)
```
MoneyMindApp/
├── Models/              # ← Crea qui Transaction.cs, BankAccount.cs, etc
├── ViewModels/          # ← Crea qui MainViewModel.cs, TransactionsViewModel.cs, etc
├── Views/               # ← Crea qui MainPage.xaml, TransactionsPage.xaml, etc
├── Services/            # ← Crea qui DatabaseService.cs, AccountService.cs, etc
│   ├── Database/
│   ├── Repositories/
│   ├── Business/
│   └── Platform/
├── DataAccess/          # ← Repository pattern base
├── Converters/          # ← XAML converters
├── Behaviors/           # ← XAML behaviors
├── Helpers/             # ← Utility classes
├── Extensions/          # ← Extension methods
├── Resources/           # ← Assets (images, fonts, styles)
│   ├── Images/
│   ├── Fonts/
│   └── Styles/
└── Platforms/           # ← Platform-specific code
    ├── Android/
    ├── iOS/
    └── Windows/
```

---

## 🎯 Cosa Fare Adesso

### Opzione A: Setup Iniziale (Se NON Hai Ancora Creato Progetto MAUI)

1. **Leggi QUICK_START.md** (Step 1-9)
2. **Esegui comandi**:
   ```bash
   cd "C:\Users\rober\Documents\MoneyMindApp"
   dotnet new maui -n MoneyMindApp -f net8.0
   # Segui tutti gli step in QUICK_START.md
   ```
3. **Test**: App "Hello World" funzionante su Android Emulator

### Opzione B: Sviluppo Incrementale (Se Hai Già Setup Base)

1. **Leggi ROADMAP.md** per scegliere fase successiva
2. **Leggi CLAUDE.md** per riferimenti Desktop→Mobile
3. **Usa FILES_TO_CREATE.md** come checklist
4. **Implementa feature by feature** (una settimana alla volta)

### Opzione C: Comando Claude per Iniziare

**Apri nuova sessione Claude in questa directory e scrivi**:

```
"Inizia FASE 1 da QUICK_START.md: crea progetto MAUI base + DatabaseService + MainViewModel + Dashboard UI.
Usa CLAUDE.md come riferimento per mappare codice da C:\Users\rober\Documents\MoneyMind (desktop).
Segui esattamente gli step 1-9 di QUICK_START.md."
```

---

## 📖 Guida Lettura Documenti

### Per Capire il Progetto (30 min)
1. README.md (10 min) - Overview generale
2. PROJECT_STRUCTURE.md (10 min) - Architettura layers
3. UI_UX_GUIDELINES.md (10 min) - Design principles

### Per Iniziare a Codare (1 ora)
1. CLAUDE.md (30 min) - **FONDAMENTALE!** Mapping Desktop→Mobile
2. QUICK_START.md (30 min) - Step operativi immediati

### Per Pianificare (1 ora)
1. ROADMAP.md (30 min) - Piano 11 settimane
2. FILES_TO_CREATE.md (30 min) - Checklist dettagliata

---

## 🧠 Concetti Chiave da Ricordare

### 1. Database Compatibilità Desktop
- **Stesso schema** desktop (MoneyMind_Global.db + MoneyMind_Conto_XXX.db)
- **Path diverso**: Android `/data/data/.../files/`, Desktop `%APPDATA%\MoneyMind`
- **Sync futura**: Cloud via Google Drive API

### 2. NO Classificazioni Mobile
- ❌ Pattern matching
- ❌ MacroCategoria/Categoria
- ❌ AI classification
- ✅ Solo transazioni "grezze" (Data, Importo, Descrizione)

### 3. Architettura MVVM
- **Model**: Entity classes (Transaction, BankAccount)
- **View**: XAML UI (MainPage, TransactionsPage)
- **ViewModel**: Logic + Data Binding (MainViewModel, TransactionsViewModel)
- **Service**: Business logic (DatabaseService, AccountService)

### 4. Stack Tecnologico
- **.NET MAUI 8.0** - Framework cross-platform
- **SQLite-net-pcl** - Database ORM
- **CommunityToolkit.Mvvm** - MVVM helpers
- **LiveChartsCore** - Grafici moderni
- **ClosedXML** - Export Excel

### 5. Target Piattaforme
- **Android 7.0+** (API 24) - Priorità massima
- **iOS 11+** - Secondario (richiede Mac)
- **Windows 10+** - Bonus (stesso codebase)

---

## 📋 Checklist Pre-Requisiti

Verifica di avere:
- [x] Visual Studio 2022 (17.8+)
- [x] Android Studio (SDK installato)
- [ ] .NET 8.0 SDK
- [ ] Workload MAUI installato (`dotnet workload install maui`)
- [ ] Android Emulator configurato (Pixel 5 API 30+)

**Test**:
```bash
dotnet --version  # Output: 8.0.x
echo %ANDROID_HOME%  # Output: C:\...\Android\Sdk
emulator -list-avds  # Output: lista emulatori
```

---

## 🎨 Design Philosophy

**"Poche schermate, tante informazioni, tutto fluido"**

### Principi UI/UX
1. **Information Density**: Ogni schermata mostra 3-5 metriche chiave
2. **Clarity First**: Icone universali, labels chiari, zero ambiguità
3. **Fluid Interactions**: Animazioni 200ms, swipe gestures, pull-to-refresh
4. **Contextual Actions**: FAB per azione primaria, SwipeView per azioni item

### Palette Colori (Material 3)
- **Primary**: `#6750A4` (Purple)
- **Success**: `#2E7D32` (Green - Entrate)
- **Error**: `#BA1A1A` (Red - Uscite)
- **Surface**: `#FFFBFE` (Light) / `#1C1B1F` (Dark)

---

## 🔗 Collegamenti Desktop

### App Desktop (NON Modificare!)
**Path**: `C:\Users\rober\Documents\MoneyMind`

Questa è l'app **desktop WPF** funzionante. NON toccare durante sviluppo mobile!

### Database Condiviso (Futuro)
Sync bidirezionale Desktop ↔ Mobile via cloud (Fase futura).

---

## 🆘 Help & Troubleshooting

### Errore: "Android SDK not found"
```bash
set ANDROID_HOME=C:\Users\rober\AppData\Local\Android\Sdk
set PATH=%PATH%;%ANDROID_HOME%\emulator;%ANDROID_HOME%\platform-tools
```

### Errore: "Workload MAUI not found"
```bash
dotnet workload install maui
```

### App Crasha all'Avvio
1. Check `MauiProgram.cs` - Services registrati?
2. View Output window - Stack trace?
3. Aggiungi try-catch in ViewModel methods

### Database Non Trovato
```bash
# Verifica path in DatabaseService
System.Diagnostics.Debug.WriteLine($"DB Path: {_dbPath}");

# Output Android: /data/data/com.moneymind.app/files/MoneyMind_Conto_001.db
```

---

## 📞 Comandi Utili Terminal

```bash
# Build progetto
dotnet build -f net8.0-android

# Run su emulator
dotnet run -f net8.0-android

# Clean build
dotnet clean

# List devices
adb devices

# View logs real-time
adb logcat | findstr MoneyMind

# Pull database da device
adb pull /data/data/com.moneymind.app/files/MoneyMind_Conto_001.db C:\Temp\

# Push database desktop su device (test)
adb push "C:\Users\rober\AppData\Roaming\MoneyMind\MoneyMind_Conto_001.db" /data/data/com.moneymind.app/files/
```

---

## 🎯 Obiettivo Finale

**App mobile moderna, fluida, ricca di informazioni** che permette di:
1. Visualizzare stats finanze (dashboard)
2. Gestire transazioni (lista, add, edit, delete)
3. Switchare tra conti multipli
4. Importare file bancari (CSV/Excel)
5. Esportare dati (Excel/CSV/PDF + Share)
6. Configurare periodi stipendiali
7. Rilevare duplicati
8. Analizzare grafici anno/confronto
9. Gestire impostazioni + beta license
10. Check aggiornamenti

**Timeline**: 11 settimane (1 fase/settimana)

**MVP (Minimum Viable Product)**: Fasi 1-3 (Dashboard + Transazioni + Multi-Conto) = 3 settimane

---

## 📚 Prossimi Passi Consigliati

### Giorno 1 (Setup)
1. Leggi README.md + CLAUDE.md (1 ora)
2. Verifica pre-requisiti (30 min)
3. Esegui `dotnet new maui` (QUICK_START.md Step 1-2)
4. Test "Hello World" su emulator (30 min)

### Giorno 2-3 (Core)
1. Crea Models (Transaction, BankAccount, AccountStatistics)
2. Implementa DatabaseService
3. Crea MainViewModel + MainPage
4. Test dashboard con dati mock

### Giorno 4-5 (Transazioni)
1. Crea TransactionsViewModel + TransactionsPage
2. Implementa lista virtualized
3. Add/Edit/Delete transazioni
4. Test con 100+ transazioni

### Settimana 2+ (Features)
Segui ROADMAP.md Fase 3-11.

---

## ✅ Success Criteria

Saprai di aver finito quando:
- [ ] App installabile su Android (APK/AAB)
- [ ] Dashboard mostra stats reali da DB
- [ ] Lista 1000+ transazioni scroll fluido (60fps)
- [ ] Import CSV funzionante
- [ ] Export Excel funzionante
- [ ] Grafici analytics renderizzano correttamente
- [ ] Beta license verificata all'avvio
- [ ] App funziona offline (grace 7 giorni)
- [ ] UI responsive portrait/landscape
- [ ] Dark theme implementato

---

## 🎉 Ready to Start?

**Comando per Claude**:
```
"Ho letto START_HERE.md. Voglio iniziare con QUICK_START.md Step 1-9.
Crea il progetto MAUI base + DatabaseService + MainViewModel + Dashboard UI.
Usa CLAUDE.md per mappare codice da Desktop MoneyMind."
```

**Buon coding! 🚀**
