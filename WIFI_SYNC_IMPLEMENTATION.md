# WiFi Sync - Specifiche Tecniche di Implementazione (v3)

> **Documento di riferimento per l'implementazione della sincronizzazione WiFi Desktop ↔ Mobile**
>
> **Data**: 24 Novembre 2025
> **Versione**: 3.0 (Aggiunta 3 modalità sync + gestione classificazioni)
> **Status**: DA IMPLEMENTARE

---

## 1. ARCHITETTURA DATABASE (IDENTICA SU ENTRAMBE LE PIATTAFORME)

### 1.1 Struttura File Database

```
📁 AppData/MoneyMind/
├── MoneyMind_Global.db          ← Database globale (conti, settings)
├── MoneyMind_Conto_001.db       ← Transazioni Conto 1
├── MoneyMind_Conto_002.db       ← Transazioni Conto 2
├── MoneyMind_Conto_003.db       ← Transazioni Conto 3
└── ...
```

### 1.2 Path per Piattaforma

| Piattaforma | Path |
|-------------|------|
| **Windows Desktop** | `%APPDATA%\MoneyMind\` (es: `C:\Users\rober\AppData\Roaming\MoneyMind\`) |
| **Android** | `/data/data/com.moneymind.app/files/` (via `FileSystem.AppDataDirectory`) |
| **iOS** | `Library/` (via `FileSystem.AppDataDirectory`) |

### 1.3 Tabelle Database Globale (MoneyMind_Global.db)

| Tabella | Desktop | Mobile | Sync |
|---------|---------|--------|------|
| ContiCorrenti | ✅ | ✅ | ✅ SÌ |
| AppSettings/ImpostazioniGlobali | ✅ | ✅ | ✅ SÌ |
| Pattern | ✅ | ❌ | ❌ NO (solo desktop) |
| PatternPersonalizzati | ✅ | ❌ | ❌ NO (solo desktop) |
| MacroCategorie | ✅ | ❌ | ❌ NO (solo desktop) |
| SalaryExceptions | ✅ | ✅ | ✅ SÌ |

### 1.4 Tabelle Database Conto (MoneyMind_Conto_XXX.db)

| Tabella | Desktop | Mobile | Sync |
|---------|---------|--------|------|
| Transazioni | ✅ | ✅ | ✅ SÌ (campi core) |
| Budget | ✅ | ❌ | ❌ NO |
| Obiettivi | ✅ | ❌ | ❌ NO |
| ConfigurazioneStipendi | ✅ | ❌ | ❌ NO (usa Global) |

### 1.5 Campi Transazioni - Mapping

| Campo Desktop | Campo Mobile | Sync | Note |
|---------------|--------------|------|------|
| ID | Id | ❌ NO | PK locale, rigenerato |
| Data | Data | ✅ SÌ | Core |
| Importo | Importo | ✅ SÌ | Core |
| Descrizione | Descrizione | ✅ SÌ | Core |
| Causale | Causale | ✅ SÌ | Core |
| MacroCategoria | - | ❌ NO | Solo desktop |
| Categoria | - | ❌ NO | Solo desktop |
| DataInserimento | CreatedAt | ✅ SÌ | Per controllo temporale |
| DataModifica | ModifiedAt | ✅ SÌ | Per controllo temporale |
| - | AccountId | ❌ NO | Solo mobile (implicito nel file) |
| - | Note | ❌ NO | Solo mobile |

---

## 2. FLUSSO SINCRONIZZAZIONE - SCELTA UTENTE

### 2.1 Principio Fondamentale

**L'UTENTE SCEGLIE SEMPRE**:
1. **DIREZIONE**: Desktop → Mobile OPPURE Mobile → Desktop
2. **MODALITÀ**: SOSTITUISCI, UNISCI, SOLO NUOVE
3. **CONTI**: Tutti i conti OPPURE singolo conto

---

### 2.2 Le 3 Modalità di Sincronizzazione

```
┌─────────────────────────────────────────────────────────────────┐
│        MODALITÀ SINCRONIZZAZIONE                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ○ SOSTITUISCI (Consigliato per primo sync)                     │
│    Cancella tutti i dati sulla destinazione e copia             │
│    quelli dalla sorgente                                        │
│                                                                 │
│  ○ UNISCI (Consigliato per uso quotidiano)                      │
│    Mantiene i dati esistenti e aggiunge solo le                 │
│    transazioni non duplicate                                    │
│    (Duplicato = stessa data + stessa descrizione)               │
│                                                                 │
│  ○ SOLO NUOVE (Per aggiornamenti parziali)                      │
│    Copia solo transazioni con data successiva                   │
│    all'ultima transazione sulla destinazione                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Quando Usare Ogni Modalità

| Modalità | Scenario Tipico | Note |
|----------|-----------------|------|
| **SOSTITUISCI** | Primo sync, reset completo, dati corrotti | ⚠️ Cancella tutto sulla destinazione |
| **UNISCI** | Uso quotidiano, entrambi i dispositivi usati | ✅ Preserva dati esistenti |
| **SOLO NUOVE** | Aggiornamento veloce, sai che un dispositivo è più avanti | ⚡ Più veloce |

### 2.4 Criterio di Duplicazione (per UNISCI)

Una transazione è considerata **DUPLICATA** se:

```
DUPLICATO = Data identica + Descrizione identica (case-insensitive, trimmed)
```

```csharp
bool IsDuplicate(Transaction source, Transaction dest)
{
    return source.Data.Date == dest.Data.Date &&
           source.Descrizione.Trim().Equals(
               dest.Descrizione.Trim(),
               StringComparison.OrdinalIgnoreCase);
}
```

**Esempio**:
```
Sorgente:     23/11/2025 | "Spesa Conad"
Destinazione: 23/11/2025 | "spesa conad"
Risultato: DUPLICATO ✅ (stessa data, descrizione uguale case-insensitive)

Sorgente:     23/11/2025 | "Spesa Conad"
Destinazione: 23/11/2025 | "Spesa Esselunga"
Risultato: NON duplicato ❌ (descrizione diversa)
```

### 2.5 Scenari d'Uso Tipici

| Scenario | Direzione | Modalità Consigliata |
|----------|-----------|----------------------|
| **Primo utilizzo mobile** | Desktop → Mobile | SOSTITUISCI |
| **Primo utilizzo desktop** | Mobile → Desktop | SOSTITUISCI (con avviso) |
| **Aggiornamento dopo viaggio** | Mobile → Desktop | UNISCI |
| **Aggiornamento dopo lavoro** | Desktop → Mobile | UNISCI |
| **Sync rapido (solo ultimi giorni)** | Qualsiasi | SOLO NUOVE |

---

## 2B. GESTIONE CLASSIFICAZIONI DESKTOP (CRITICO!)

### 2B.1 Il Problema

**Desktop ha classificazioni, Mobile NO**:

```
DESKTOP:
23/11/2025 | -45.00€ | "Spesa Conad" | MacroCategoria: "Spesa" | Categoria: "Alimentari"

MOBILE:
23/11/2025 | -45.00€ | "Spesa Conad" | (nessuna classificazione)
```

### 2B.2 Matrice Comportamento per Direzione e Modalità

| Direzione | Modalità | Classificazioni | Note |
|-----------|----------|-----------------|------|
| Desktop → Mobile | SOSTITUISCI | ✅ OK | Mobile non ha classificazioni |
| Desktop → Mobile | UNISCI | ✅ OK | Mobile non ha classificazioni |
| Desktop → Mobile | SOLO NUOVE | ✅ OK | Mobile non ha classificazioni |
| **Mobile → Desktop** | **SOSTITUISCI** | ⚠️ **PERSE!** | Richiede avviso esplicito |
| Mobile → Desktop | UNISCI | ✅ OK | Non tocca esistenti |
| Mobile → Desktop | SOLO NUOVE | ✅ OK | Aggiunge solo nuove |

### 2B.3 Avviso Obbligatorio: Mobile → Desktop con SOSTITUISCI

Quando l'utente seleziona `Mobile → Desktop` + `SOSTITUISCI`, mostrare:

```
┌─────────────────────────────────────────────────────────────────┐
│  🛑 ATTENZIONE - OPERAZIONE DISTRUTTIVA                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Stai per SOSTITUIRE i dati Desktop con quelli del Mobile.      │
│                                                                 │
│  Sul Desktop hai:                                               │
│  • 245 transazioni classificate                                 │
│  • 12 macro-categorie utilizzate                                │
│                                                                 │
│  TUTTE LE CLASSIFICAZIONI VERRANNO PERSE!                       │
│                                                                 │
│  💾 Verrà creato un backup automatico prima di procedere.       │
│  Potrai ripristinarlo per recuperare i dati.                    │
│                                                                 │
│  💡 Consiglio: Usa UNISCI per mantenere le classificazioni      │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │   ANNULLA   │  │ USA UNISCI  │  │ SOSTITUISCI (CON BACKUP)│ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2B.4 Calcolo Statistiche per Avviso

Per mostrare l'avviso, il Desktop deve calcolare:

```csharp
public class ClassificationStats
{
    public int TotalTransactions { get; set; }
    public int ClassifiedTransactions { get; set; }  // MacroCategoria != ""
    public int UniqueMacroCategories { get; set; }
    public int UniqueCategories { get; set; }
}

// Query Desktop
SELECT
    COUNT(*) as TotalTransactions,
    SUM(CASE WHEN MacroCategoria != '' THEN 1 ELSE 0 END) as ClassifiedTransactions,
    COUNT(DISTINCT MacroCategoria) as UniqueMacroCategories,
    COUNT(DISTINCT Categoria) as UniqueCategories
FROM Transazioni
WHERE MacroCategoria != '' OR Categoria != '';
```

### 2B.5 Flusso Ripristino Backup

Se l'utente si pente dopo SOSTITUISCI:

```
PRIMA DELLA SYNC:
Desktop: 245 transazioni classificate
         ↓
    [BACKUP AUTOMATICO] → backups/MoneyMind_Backup_YYYYMMDD_HHMMSS/
         ↓
DOPO SYNC (SOSTITUISCI Mobile → Desktop):
Desktop: 245 transazioni SENZA classificazioni
         ↓
SE UTENTE RIPRISTINA BACKUP:
Desktop: 245 transazioni classificate ✅ (tutto recuperato)
```

### 2.3 Opzioni di Sincronizzazione

```
┌─────────────────────────────────────────────────────────────────┐
│  Seleziona cosa sincronizzare:                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ○ TUTTI I CONTI                                                │
│    Sincronizza tutti i conti correnti e le transazioni          │
│                                                                 │
│  ○ SOLO UN CONTO SPECIFICO                                      │
│    ┌─────────────────────────────────────┐                      │
│    │ Conto Principale           ▼        │                      │
│    └─────────────────────────────────────┘                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. CONTROLLO TEMPORALE E AVVISI

### 3.1 Informazioni da Confrontare (Per Ogni Conto)

Prima di procedere, il sistema confronta:

| Metrica | Sorgente | Destinazione | Azione |
|---------|----------|--------------|--------|
| Transazione più recente (Data) | 23/11/2025 | 15/11/2025 | ✅ OK (sorgente più recente) |
| Transazione più recente (Data) | 15/11/2025 | 23/11/2025 | ⚠️ AVVISO (destinazione più recente) |
| Numero transazioni | 150 | 120 | ℹ️ Info |
| Ultima modifica DB | timestamp | timestamp | ℹ️ Info |

### 3.2 Avviso Dati Più Recenti sulla Destinazione

```
┌─────────────────────────────────────────────────────────────────┐
│  ⚠️ ATTENZIONE                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Il MOBILE contiene transazioni più recenti del DESKTOP!        │
│                                                                 │
│  Desktop:                                                       │
│  • Ultima transazione: 15/11/2025                               │
│  • Totale transazioni: 120                                      │
│                                                                 │
│  Mobile:                                                        │
│  • Ultima transazione: 23/11/2025                               │
│  • Totale transazioni: 150                                      │
│                                                                 │
│  Se procedi, PERDERAI le 30 transazioni più recenti sul Mobile! │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────────────────────────┐  │
│  │    ANNULLA      │  │  PROCEDI COMUNQUE (BACKUP CREATO)   │  │
│  └─────────────────┘  └─────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Logica Controllo Temporale

```csharp
public class SyncComparisonResult
{
    public int SourceAccountId { get; set; }
    public string SourceAccountName { get; set; }

    // Sorgente (da copiare)
    public DateTime? SourceLatestTransactionDate { get; set; }
    public int SourceTransactionCount { get; set; }
    public DateTime? SourceLastModified { get; set; }

    // Destinazione (da sovrascrivere)
    public DateTime? DestLatestTransactionDate { get; set; }
    public int DestTransactionCount { get; set; }
    public DateTime? DestLastModified { get; set; }

    // Analisi
    public bool DestinationHasNewerData =>
        DestLatestTransactionDate > SourceLatestTransactionDate;

    public int TransactionDifference =>
        DestTransactionCount - SourceTransactionCount;

    public bool RequiresWarning =>
        DestinationHasNewerData || TransactionDifference > 0;
}
```

---

## 4. BACKUP PRE-SINCRONIZZAZIONE (OBBLIGATORIO)

### 4.1 Flusso con Backup

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   1. AVVIA   │────►│  2. ANALISI  │────►│  3. BACKUP   │────►│   4. SYNC    │
│     SYNC     │     │  CONFRONTO   │     │ AUTOMATICO   │     │   EFFETTIVO  │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
                            │                    │
                            ▼                    ▼
                     ┌──────────────┐     ┌──────────────┐
                     │   MOSTRA     │     │   SALVA IN   │
                     │   AVVISI     │     │   /backups/  │
                     └──────────────┘     └──────────────┘
```

### 4.2 Struttura Backup

```
📁 AppData/MoneyMind/backups/
└── 📁 MoneyMind_Backup_20251123_143052/
    ├── MoneyMind_Global.db
    ├── MoneyMind_Conto_001.db
    ├── MoneyMind_Conto_002.db
    └── backup_info.json
```

**backup_info.json**:
```json
{
    "created_at": "2025-11-23T14:30:52",
    "reason": "pre_sync",
    "sync_direction": "desktop_to_mobile",
    "accounts_backed_up": [
        {
            "id": 1,
            "name": "Conto Principale",
            "transaction_count": 150,
            "latest_transaction": "2025-11-23"
        }
    ],
    "app_version": "1.0.0",
    "platform": "Android"
}
```

### 4.3 Conferma Backup Prima di Procedere

```
┌─────────────────────────────────────────────────────────────────┐
│  💾 BACKUP NECESSARIO                                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Prima di sincronizzare, verrà creato un backup dei tuoi dati.  │
│                                                                 │
│  Dati da salvare:                                               │
│  • Conto Principale (150 transazioni)                           │
│  • Conto Secondario (45 transazioni)                            │
│                                                                 │
│  Spazio richiesto: ~2.5 MB                                      │
│  Posizione: /backups/MoneyMind_Backup_20251123_143052/          │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────────────────────────┐  │
│  │    ANNULLA      │  │      CREA BACKUP E PROCEDI          │  │
│  └─────────────────┘  └─────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. API ENDPOINTS (Mobile = Server)

### 5.1 Architettura

```
┌─────────────────┐                              ┌─────────────────┐
│     DESKTOP     │         WiFi / Hotspot       │     MOBILE      │
│   (VB.NET WPF)  │                              │   (.NET MAUI)   │
│                 │      HTTP REST API           │                 │
│   HTTP Client   │◄────────────────────────────►│   HTTP Server   │
│                 │      Port 8765               │   (Kestrel)     │
└─────────────────┘                              └─────────────────┘
```

**NOTA**: Il Desktop è sempre il CLIENT, il Mobile è sempre il SERVER.
- Per "Desktop → Mobile": Desktop invia POST con dati, Mobile li riceve e salva
- Per "Mobile → Desktop": Desktop fa GET, riceve dati, li salva localmente

### 5.2 Endpoints

#### GET /ping
Verifica connessione.

**Response**:
```json
{
    "status": "ok",
    "app": "MoneyMind Mobile",
    "version": "1.0.0",
    "timestamp": "2025-11-23T14:30:00"
}
```

#### GET /accounts
Lista conti disponibili con statistiche per confronto.

**Response**:
```json
{
    "success": true,
    "accounts": [
        {
            "id": 1,
            "nome": "Conto Principale",
            "saldoIniziale": 1000.00,
            "transactionCount": 150,
            "latestTransactionDate": "2025-11-23",
            "latestModifiedAt": "2025-11-23T10:30:00",
            "databaseFile": "MoneyMind_Conto_001.db"
        },
        {
            "id": 2,
            "nome": "Conto Secondario",
            "saldoIniziale": 500.00,
            "transactionCount": 45,
            "latestTransactionDate": "2025-11-20",
            "latestModifiedAt": "2025-11-20T15:00:00",
            "databaseFile": "MoneyMind_Conto_002.db"
        }
    ]
}
```

#### GET /transactions/{accountId}
Scarica tutte le transazioni di un conto (Mobile → Desktop).

**Response**:
```json
{
    "success": true,
    "accountId": 1,
    "accountName": "Conto Principale",
    "saldoIniziale": 1000.00,
    "transactionCount": 150,
    "transactions": [
        {
            "data": "2025-11-23",
            "importo": -45.50,
            "descrizione": "Spesa supermercato",
            "causale": "Alimentari",
            "createdAt": "2025-11-23T10:30:00",
            "modifiedAt": null
        }
    ]
}
```

#### POST /sync/prepare
Prepara la sincronizzazione: crea backup e restituisce confronto.

**Request**:
```json
{
    "direction": "desktop_to_mobile",
    "accountIds": [1, 2],
    "desktopAccounts": [
        {
            "id": 1,
            "nome": "Conto Principale",
            "saldoIniziale": 1000.00,
            "transactionCount": 120,
            "latestTransactionDate": "2025-11-15"
        }
    ]
}
```

**Response**:
```json
{
    "success": true,
    "backupCreated": true,
    "backupPath": "backups/MoneyMind_Backup_20251123_143052",
    "comparison": [
        {
            "accountId": 1,
            "accountName": "Conto Principale",
            "sourceTransactionCount": 120,
            "sourceLatestDate": "2025-11-15",
            "destTransactionCount": 150,
            "destLatestDate": "2025-11-23",
            "warning": true,
            "warningMessage": "Il Mobile ha 30 transazioni in più e dati più recenti (23/11 vs 15/11)"
        }
    ],
    "requiresConfirmation": true
}
```

#### POST /sync/execute
Esegue la sincronizzazione effettiva (Desktop → Mobile).

**Request**:
```json
{
    "direction": "desktop_to_mobile",
    "confirmed": true,
    "accounts": [
        {
            "id": 1,
            "nome": "Conto Principale",
            "saldoIniziale": 1000.00,
            "icona": "💳",
            "colore": "#512BD4",
            "transactions": [
                {
                    "data": "2025-11-15",
                    "importo": -30.00,
                    "descrizione": "Pranzo",
                    "causale": "Ristorazione",
                    "createdAt": "2025-11-15T12:00:00"
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
            "accountName": "Conto Principale",
            "previousTransactionCount": 150,
            "newTransactionCount": 120,
            "status": "replaced"
        }
    ],
    "message": "Sincronizzazione completata. 1 conto aggiornato."
}
```

---

## 6. LOGICA DI SINCRONIZZAZIONE

### 6.1 Desktop → Mobile (SOSTITUZIONE)

```
1. Desktop chiama GET /accounts per ottenere lista conti mobile
2. Desktop chiama POST /sync/prepare con i propri dati
3. Mobile crea backup
4. Mobile confronta e restituisce warnings
5. Se warnings, Desktop mostra all'utente e chiede conferma
6. Desktop chiama POST /sync/execute con conferma
7. Mobile:
   a. Per ogni conto ricevuto:
      - Se conto esiste: SVUOTA tabella Transazioni e inserisce nuove
      - Se conto non esiste: CREA conto e database
   b. Per conti non ricevuti: mantiene (non elimina)
8. Mobile restituisce risultato
```

### 6.2 Mobile → Desktop (DOWNLOAD)

```
1. Desktop chiama GET /accounts
2. Desktop chiama POST /sync/prepare (direction: mobile_to_desktop)
3. Per ogni conto selezionato:
   a. Desktop chiama GET /transactions/{accountId}
   b. Desktop salva localmente (backup + sostituzione)
4. Desktop restituisce risultato all'utente
```

### 6.3 Codice Sostituzione Transazioni (Mobile)

```csharp
public async Task<SyncResult> ExecuteSyncAsync(SyncExecuteRequest request)
{
    var results = new List<AccountSyncResult>();

    foreach (var accountData in request.Accounts)
    {
        // 1. Trova o crea il conto
        var account = await _globalDb.GetAccountByIdAsync(accountData.Id);
        if (account == null)
        {
            // Crea nuovo conto
            account = new BankAccount
            {
                Nome = accountData.Nome,
                SaldoIniziale = accountData.SaldoIniziale,
                Icona = accountData.Icona,
                Colore = accountData.Colore,
                CreatedAt = DateTime.Now
            };
            await _globalDb.InsertAccountAsync(account);
        }
        else
        {
            // Aggiorna dati conto esistente
            account.Nome = accountData.Nome;
            account.SaldoIniziale = accountData.SaldoIniziale;
            account.Icona = accountData.Icona;
            account.Colore = accountData.Colore;
            await _globalDb.UpdateAccountAsync(account);
        }

        // 2. Inizializza database conto
        await _accountDb.InitializeAsync(account.Id);

        // 3. Conta transazioni esistenti (per report)
        var existingCount = (await _accountDb.GetAllTransactionsAsync()).Count;

        // 4. SVUOTA tabella transazioni
        await _accountDb.DeleteAllTransactionsAsync();

        // 5. Inserisci nuove transazioni
        foreach (var tx in accountData.Transactions)
        {
            await _accountDb.InsertTransactionAsync(new Transaction
            {
                Data = DateTime.Parse(tx.Data),
                Importo = tx.Importo,
                Descrizione = tx.Descrizione,
                Causale = tx.Causale,
                AccountId = account.Id,
                CreatedAt = tx.CreatedAt ?? DateTime.Now,
                ModifiedAt = tx.ModifiedAt
            });
        }

        results.Add(new AccountSyncResult
        {
            AccountId = account.Id,
            AccountName = account.Nome,
            PreviousCount = existingCount,
            NewCount = accountData.Transactions.Count,
            Status = "replaced"
        });
    }

    return new SyncResult
    {
        Success = true,
        Results = results
    };
}
```

---

## 7. UI MOBILE - WiFiSyncPage

### 7.1 Schermata Principale

```
┌─────────────────────────────────────────────────────────────────┐
│ ←  Sincronizzazione WiFi                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ 📡 SERVER SYNC                                            │  │
│  │                                                           │  │
│  │ Status: ● ATTIVO                                          │  │
│  │ Indirizzo: 192.168.43.1:8765                       [📋]   │  │
│  │                                                           │  │
│  │ ┌─────────────────────────────────────────────────────┐   │  │
│  │ │              ⏹️  FERMA SERVER                        │   │  │
│  │ └─────────────────────────────────────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ℹ️ ISTRUZIONI                                             │  │
│  │                                                           │  │
│  │ 1. Assicurati che il server sia attivo (sopra)            │  │
│  │                                                           │  │
│  │ 2. Sul computer, apri MoneyMind Desktop                   │  │
│  │                                                           │  │
│  │ 3. Vai su Menu → Sincronizza WiFi                         │  │
│  │                                                           │  │
│  │ 4. Inserisci l'indirizzo: 192.168.43.1                    │  │
│  │                                                           │  │
│  │ 5. Scegli la direzione di sincronizzazione                │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ 📊 ULTIMO SYNC                                            │  │
│  │                                                           │  │
│  │ Data: 22/11/2025 15:30                                    │  │
│  │ Direzione: Desktop → Mobile                               │  │
│  │ Conti sincronizzati: 2                                    │  │
│  │ Transazioni ricevute: 195                                 │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ▼ Risoluzione Problemi                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 Durante Sync (Server Side)

```
┌─────────────────────────────────────────────────────────────────┐
│  🔄 SINCRONIZZAZIONE IN CORSO                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Desktop connesso: 192.168.43.100                               │
│                                                                 │
│  ⏳ Creazione backup...                          ✅ Completato   │
│  ⏳ Ricezione dati conto "Principale"...         ✅ 120 trans.   │
│  ⏳ Ricezione dati conto "Secondario"...         🔄 In corso...  │
│                                                                 │
│  ████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░  65%            │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                      ANNULLA                             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. UI DESKTOP - WiFiSyncDialog

### 8.1 Schermata Principale (con 3 Modalità)

```
┌─────────────────────────────────────────────────────────────────┐
│  Sincronizzazione WiFi                                     [X]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📱 CONNESSIONE AL MOBILE                                       │
│                                                                 │
│  Indirizzo IP del telefono:                                     │
│  ┌─────────────────────────────────────┐  ┌─────────────────┐  │
│  │ 192.168.43.1                        │  │     CONNETTI    │  │
│  └─────────────────────────────────────┘  └─────────────────┘  │
│                                                                 │
│  Status: ✅ Connesso a "Pixel 7"                                │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📋 DIREZIONE                                                   │
│                                                                 │
│  ○ 📥 DESKTOP → MOBILE                                          │
│  ○ 📤 MOBILE → DESKTOP                                          │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  🔄 MODALITÀ                                                    │
│                                                                 │
│  ○ SOSTITUISCI - Cancella tutto e copia da sorgente             │
│  ○ UNISCI - Aggiunge solo transazioni non duplicate             │
│  ○ SOLO NUOVE - Copia solo transazioni più recenti              │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📂 CONTI                                                       │
│                                                                 │
│  ○ Tutti i conti                                                │
│  ○ Solo: [Conto Principale        ▼]                            │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐           ┌───────────────────────────┐   │
│  │     ANNULLA     │           │      ▶ AVANTI             │   │
│  └─────────────────┘           └───────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Schermata Confronto e Avviso

```
┌─────────────────────────────────────────────────────────────────┐
│  Sincronizzazione WiFi                                     [X]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️ ATTENZIONE - VERIFICA DATI                                  │
│                                                                 │
│  Stai per copiare: DESKTOP → MOBILE                             │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ CONTO: Principale                                           ││
│  │                                                             ││
│  │           DESKTOP (sorgente)    MOBILE (destinazione)       ││
│  │ Transazioni:      120                  150                  ││
│  │ Ultima trans.:    15/11/2025           23/11/2025           ││
│  │                                                             ││
│  │ ⚠️ AVVISO: Il Mobile contiene 30 transazioni in più         ││
│  │    e dati più recenti! Verranno SOVRASCRITTI.               ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ CONTO: Secondario                                           ││
│  │                                                             ││
│  │           DESKTOP (sorgente)    MOBILE (destinazione)       ││
│  │ Transazioni:      45                   45                   ││
│  │ Ultima trans.:    20/11/2025           20/11/2025           ││
│  │                                                             ││
│  │ ✅ I dati sono allineati                                    ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  💾 Verrà creato un backup automatico prima di procedere        │
│                                                                 │
│  ┌─────────────────┐           ┌───────────────────────────┐   │
│  │     ANNULLA     │           │   PROCEDI CON BACKUP ▶    │   │
│  └─────────────────┘           └───────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. MODELLI DATI

### 9.1 Enum SyncMode

```csharp
/// <summary>
/// Modalità di sincronizzazione
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Cancella tutto sulla destinazione e copia dalla sorgente
    /// </summary>
    Replace,

    /// <summary>
    /// Mantiene esistenti e aggiunge solo non-duplicati
    /// </summary>
    Merge,

    /// <summary>
    /// Copia solo transazioni più recenti dell'ultima sulla destinazione
    /// </summary>
    NewOnly
}

/// <summary>
/// Direzione sincronizzazione
/// </summary>
public enum SyncDirection
{
    DesktopToMobile,
    MobileToDesktop
}
```

### 9.2 Models Mobile (C#)

```csharp
// Models/Sync/SyncModels.cs

namespace MoneyMindApp.Models.Sync;

/// <summary>
/// Transazione in formato sync (solo campi core)
/// </summary>
public class SyncTransaction
{
    public string Data { get; set; } = string.Empty;  // yyyy-MM-dd
    public decimal Importo { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public string Causale { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Conto in formato sync
/// </summary>
public class SyncAccount
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal SaldoIniziale { get; set; }
    public string? Icona { get; set; }
    public string? Colore { get; set; }
    public int TransactionCount { get; set; }
    public string? LatestTransactionDate { get; set; }  // yyyy-MM-dd
    public DateTime? LatestModifiedAt { get; set; }
    public string? DatabaseFile { get; set; }
    public List<SyncTransaction> Transactions { get; set; } = new();

    // Solo per Desktop (statistiche classificazione)
    public int ClassifiedCount { get; set; }
    public int UniqueMacroCategories { get; set; }
}

/// <summary>
/// Richiesta preparazione sync
/// </summary>
public class SyncPrepareRequest
{
    public SyncDirection Direction { get; set; }
    public SyncMode Mode { get; set; }
    public List<int>? AccountIds { get; set; }  // null = tutti
    public List<SyncAccount> SourceAccounts { get; set; } = new();
}

/// <summary>
/// Risposta preparazione sync con confronto
/// </summary>
public class SyncPrepareResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool BackupCreated { get; set; }
    public string? BackupPath { get; set; }
    public List<SyncComparison> Comparisons { get; set; } = new();
    public bool RequiresConfirmation { get; set; }
    public bool HasClassificationWarning { get; set; }  // Per Mobile→Desktop SOSTITUISCI
    public int TotalClassifiedTransactions { get; set; }
}

/// <summary>
/// Confronto per singolo conto
/// </summary>
public class SyncComparison
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;

    // Sorgente
    public int SourceTransactionCount { get; set; }
    public string? SourceLatestDate { get; set; }

    // Destinazione
    public int DestTransactionCount { get; set; }
    public string? DestLatestDate { get; set; }

    // Classificazioni (solo per Mobile→Desktop)
    public int DestClassifiedCount { get; set; }

    // Analisi
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }
}

/// <summary>
/// Richiesta esecuzione sync
/// </summary>
public class SyncExecuteRequest
{
    public SyncDirection Direction { get; set; }
    public SyncMode Mode { get; set; }
    public bool Confirmed { get; set; }
    public List<SyncAccount> Accounts { get; set; } = new();
}

/// <summary>
/// Risultato sync per conto
/// </summary>
public class SyncAccountResult
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int PreviousTransactionCount { get; set; }
    public int NewTransactionCount { get; set; }
    public int DuplicatesSkipped { get; set; }  // Per modalità UNISCI
    public int NewOnlyAdded { get; set; }       // Per modalità SOLO NUOVE
    public string Status { get; set; } = string.Empty;  // "replaced" | "merged" | "new_only" | "created" | "error"
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Risposta esecuzione sync
/// </summary>
public class SyncExecuteResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<SyncAccountResult> Results { get; set; } = new();
    public string? Message { get; set; }

    // Report sintetico
    public int TotalTransactionsProcessed { get; set; }
    public int TotalDuplicatesSkipped { get; set; }
    public int TotalNewAdded { get; set; }
}
```

---

## 10. CHECKLIST IMPLEMENTAZIONE

### FASE 1: Modelli e Utilities (Mobile)
- [ ] `Models/Sync/SyncModels.cs` - Tutti i modelli sync (inclusi enum SyncMode, SyncDirection)
- [ ] `Helpers/SyncHelper.cs` - Metodi utility:
  - [ ] `IsDuplicate(source, dest)` - Confronto Data + Descrizione
  - [ ] `CalculateSimilarity()` - (opzionale, per future evoluzioni)

### FASE 2: Backup Service (Mobile)
- [ ] `Services/Backup/IBackupService.cs` - Interface
- [ ] `Services/Backup/BackupService.cs` - Implementazione backup pre-sync

### FASE 3: WiFiSyncService Aggiornato (Mobile)
- [ ] Aggiornare `Services/Sync/WiFiSyncService.cs`:
  - [ ] GET /accounts (con statistiche)
  - [ ] GET /transactions/{accountId}
  - [ ] POST /sync/prepare (con mode)
  - [ ] POST /sync/execute (3 modalità)
  - [ ] Logica SOSTITUISCI (cancella + inserisci)
  - [ ] Logica UNISCI (skip duplicati)
  - [ ] Logica SOLO NUOVE (filtra per data)
  - [ ] Integrazione BackupService

### FASE 4: UI Mobile
- [ ] `Views/WiFiSyncPage.xaml` - UI server
- [ ] `ViewModels/WiFiSyncViewModel.cs` - ViewModel
- [ ] Navigazione da Settings

### FASE 5: Desktop VB.NET
- [ ] `Services/WiFiSyncClient.vb` - Client HTTP
- [ ] `Models/SyncModels.vb` - Modelli (inclusi enum)
- [ ] `Views/WiFiSyncDialog.xaml` + `.vb` - Dialog con:
  - [ ] Selezione DIREZIONE
  - [ ] Selezione MODALITÀ (3 opzioni)
  - [ ] Selezione CONTI
  - [ ] Avviso classificazioni (Mobile→Desktop + SOSTITUISCI)
  - [ ] Report sintetico finale
- [ ] Integrazione in MainWindow

### FASE 6: Testing
- [ ] Test backup pre-sync
- [ ] Test modalità SOSTITUISCI
- [ ] Test modalità UNISCI (verifica skip duplicati)
- [ ] Test modalità SOLO NUOVE
- [ ] Test avviso classificazioni Mobile→Desktop
- [ ] Test ripristino backup
- [ ] Test su hotspot Android/iPhone

---

## 11. PUNTI CRITICI DA RICORDARE

1. **3 MODALITÀ**: SOSTITUISCI, UNISCI, SOLO NUOVE
2. **DIREZIONE ESPLICITA**: L'utente DEVE sempre scegliere Desktop→Mobile o Mobile→Desktop
3. **BACKUP OBBLIGATORIO**: SEMPRE creare backup prima di procedere (ripristinabile)
4. **CRITERIO DUPLICATO**: Data + Descrizione identiche (case-insensitive, trimmed)
5. **AVVISO CLASSIFICAZIONI**: Mobile→Desktop + SOSTITUISCI = perdita classificazioni (avvisare!)
6. **REPORT SINTETICO**: Mostrare transazioni processate, duplicate saltate, nuove aggiunte
7. **IGNORA CLASSIFICAZIONI**: MacroCategoria/Categoria non vengono trasferite (Mobile non le ha)
8. **ACCOUNT MULTIPLI**: Supportare tutti i conti o singolo conto

---

## 12. RIEPILOGO COMPORTAMENTO PER MODALITÀ

### SOSTITUISCI (Replace)
```
1. Backup destinazione
2. Cancella TUTTE le transazioni sulla destinazione
3. Inserisce TUTTE le transazioni dalla sorgente
4. Report: "Sostituite X transazioni"
```

### UNISCI (Merge)
```
1. Backup destinazione
2. Per ogni transazione sorgente:
   - Se esiste duplicato (Data + Descrizione): SKIP
   - Se non esiste: INSERISCI
3. Report: "Aggiunte X nuove, Y duplicate saltate"
```

### SOLO NUOVE (NewOnly)
```
1. Trova ultima transazione sulla destinazione (per data)
2. Backup destinazione
3. Filtra sorgente: solo transazioni con Data > ultima destinazione
4. Per ogni transazione filtrata: INSERISCI
5. Report: "Aggiunte X transazioni più recenti di DD/MM/YYYY"
```

---

**Fine Documento - WIFI_SYNC_IMPLEMENTATION.md v3**
