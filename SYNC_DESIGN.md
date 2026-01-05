# 🔄 WiFi Sync - Design Completo (Desktop ↔ Mobile)

> **Data**: 27 Dicembre 2025
> **Obiettivo**: Sincronizzazione bidirezionale flessibile con gestione completa di tutti gli scenari possibili

---

## 📋 INDICE

1. [Scenari Identificati](#scenari-identificati)
2. [UX Flow Completo](#ux-flow-completo)
3. [Modelli Dati](#modelli-dati)
4. [Logica Matching Conti](#logica-matching-conti)
5. [Endpoint API](#endpoint-api)
6. [UI Desktop](#ui-desktop)
7. [UI Mobile](#ui-mobile)
8. [Gestione Conflitti](#gestione-conflitti)

---

## 🎯 SCENARI IDENTIFICATI

### **SCENARIO 1: Primo Setup - Desktop→Mobile**
**Situazione**:
- Desktop: 2 conti con 1000+ transazioni
- Mobile: 1 conto test vuoto

**Azione utente**:
- Seleziona "Copia TUTTO Desktop→Mobile"
- Modalità: Replace/Merge

**Risultato atteso**:
- Mobile avrà 2 conti identici al desktop con tutte le transazioni

---

### **SCENARIO 2: Conto esiste SOLO su Desktop**
**Situazione**:
- Desktop ha "Banca di Credito Cooperativo" (ID=1, 1235 trans)
- Mobile NON ha questo conto

**Opzioni disponibili**:
1. **Create on Mobile (New ID)**: Crea nuovo conto su Mobile con ID incrementale (es. ID=4)
2. **Create on Mobile (Same ID)**: Crea nuovo conto su Mobile con STESSO ID=1 (solo se ID libero)
3. **Link to existing Mobile account**: Mappa a conto Mobile esistente (es. "test" ID=1)
4. **Skip**: Non sincronizzare questo conto

**Gestione conflitto ID**:
- Se ID=1 già occupato su Mobile → Chiedi: "Usa ID=4" oppure "Sostituisci ID=1"

---

### **SCENARIO 3: Conto esiste SOLO su Mobile**
**Situazione**:
- Mobile ha "Carta di Credito" (ID=3, 150 trans)
- Desktop NON ha questo conto

**Opzioni disponibili**:
1. **Create on Desktop (New ID)**: Crea nuovo conto su Desktop con ID incrementale
2. **Create on Desktop (Same ID)**: Crea nuovo conto su Desktop con STESSO ID=3
3. **Link to existing Desktop account**: Mappa a conto Desktop esistente
4. **Skip**: Non sincronizzare questo conto

---

### **SCENARIO 4: Stesso conto su entrambi (ID e Nome uguali)**
**Situazione**:
- Desktop: "Conto Corrente" (ID=1, 500 trans)
- Mobile: "Conto Corrente" (ID=1, 300 trans)

**Opzioni disponibili**:
1. **Desktop→Mobile Replace**: Cancella Mobile, copia da Desktop (risultato: 500 trans)
2. **Desktop→Mobile Merge**: Unisci, skip duplicati (risultato: ~550 trans)
3. **Desktop→Mobile NewOnly**: Solo transazioni più recenti (risultato: 300 + nuove)
4. **Mobile→Desktop Replace**: Cancella Desktop, copia da Mobile (risultato: 300 trans) ⚠️ Perde classificazioni!
5. **Mobile→Desktop Merge**: Unisci, skip duplicati (risultato: ~550 trans)
6. **Mobile→Desktop NewOnly**: Solo transazioni più recenti (risultato: 500 + nuove)
7. **Skip**: Non sincronizzare

---

### **SCENARIO 5: Stesso ID, Nome DIVERSO**
**Situazione**:
- Desktop: "Banca Intesa" (ID=1)
- Mobile: "Conto Test" (ID=1)

**Problema**: Sono lo stesso conto rinominato o conti diversi?

**Soluzione**:
Chiedi all'utente:
```
⚠️ Possibile conflitto rilevato:
- Desktop ha "Banca Intesa" (ID=1, 1235 trans)
- Mobile ha "Conto Test" (ID=1, 0 trans)

Questi conti sono lo stesso?
○ Sì, sono lo stesso conto (rinominato) → Sincronizza normalmente
○ No, sono conti diversi → Mostra opzioni mapping
```

---

### **SCENARIO 6: Sincronizzazione Parziale**
**Situazione**:
- Desktop ha 3 conti: A, B, C
- Mobile ha 2 conti: B, D
- Utente vuole sincronizzare SOLO conto A e B

**Azione**:
- Tabella con checkbox per ogni conto
- Utente seleziona solo A e B
- Per A: Crea su Mobile
- Per B: Sceglie modalità Merge

---

### **SCENARIO 7: Sincronizzazione Bidirezionale**
**Situazione**:
- Desktop ha aggiunto transazioni al Conto A
- Mobile ha aggiunto transazioni al Conto B

**Azione**:
- Utente seleziona:
  - Conto A: Desktop→Mobile Merge
  - Conto B: Mobile→Desktop Merge
- Singola operazione con direzioni miste

---

### **SCENARIO 8: Conflitto ID - Creazione nuovo conto**
**Situazione**:
- Desktop: "Conto A" (ID=1), "Conto B" (ID=2)
- Mobile: "Conto C" (ID=1), "Conto D" (ID=3)
- Utente vuole creare "Conto A" su Mobile

**Problema**: ID=1 già occupato su Mobile!

**Soluzione**:
```
⚠️ Conflitto ID rilevato:
Vuoi creare "Conto A" (ID=1 su Desktop) su Mobile,
ma Mobile ha già un conto con ID=1 ("Conto C").

Scegli azione:
○ Crea con nuovo ID (suggerito: ID=4) [CONSIGLIATO]
○ Sostituisci conto Mobile ID=1 ("Conto C") con "Conto A"
○ Annulla operazione
```

---

## 🎨 UX FLOW COMPLETO

### **STEP 1: Connessione**

**Desktop/Mobile**:
```
┌─────────────────────────────────────────────┐
│  WiFi Sync                                  │
├─────────────────────────────────────────────┤
│  IP Mobile: [192.168.1.100    ] [Connetti] │
│                                             │
│  Stato: ⏳ Connessione in corso...         │
└─────────────────────────────────────────────┘

↓ Dopo connessione:

┌─────────────────────────────────────────────┐
│  WiFi Sync                                  │
├─────────────────────────────────────────────┤
│  ✅ Connesso a 192.168.1.100               │
│                                             │
│  📊 Rilevati:                              │
│     • 2 conti su Desktop (1268 trans)      │
│     • 1 conto su Mobile (0 trans)          │
│                                             │
│  [Disconnetti] [Continua →]                │
└─────────────────────────────────────────────┘
```

---

### **STEP 2: Vista Comparativa Conti**

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Seleziona Conti da Sincronizzare                                       │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────┬──────────────────┬─────────────────┬─────────────────┬─────────┐│
│  │ ☑  │ Nome Conto       │ Desktop         │ Mobile          │ Azione  ││
│  ├────┼──────────────────┼─────────────────┼─────────────────┼─────────┤│
│  │ ☑  │ Banca Credito    │ ✅ ID=1 (1235)  │ ❌ Non presente │[Dropdown│
]││
│  │ ☑  │ test             │ ✅ ID=2 (33)    │ ✅ ID=1 (0)     │[Dropdown]││
│  │ ☐  │ Carta Credito    │ ❌ Non presente │ ✅ ID=3 (150)   │[Dropdown]││
│  └────┴──────────────────┴─────────────────┴─────────────────┴─────────┘│
│                                                                          │
│  Quick Actions:                                                          │
│  [Desktop→Mobile (All)] [Mobile→Desktop (All)] [Smart Merge (All)]      │
│                                                                          │
│  [← Indietro] [Anteprima Modifiche →]                                   │
└──────────────────────────────────────────────────────────────────────────┘
```

**Dropdown Opzioni** (varia in base allo stato):

**Se conto esiste su entrambi**:
- Skip (non sincronizzare)
- Desktop→Mobile (Replace)
- Desktop→Mobile (Merge) ← DEFAULT
- Desktop→Mobile (NewOnly)
- Mobile→Desktop (Replace) ⚠️
- Mobile→Desktop (Merge)
- Mobile→Desktop (NewOnly)

**Se conto solo su Desktop**:
- Skip
- Create on Mobile (New ID)
- Create on Mobile (Same ID if available)
- Link to existing Mobile account...

**Se conto solo su Mobile**:
- Skip
- Create on Desktop (New ID)
- Create on Desktop (Same ID if available)
- Link to existing Desktop account...

---

### **STEP 3: Link to Existing (Dialog)**

Quando utente seleziona "Link to existing...":

```
┌──────────────────────────────────────────────────────────┐
│  Collega Conto a Conto Esistente                         │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Vuoi collegare "Banca Credito" (Desktop, ID=1, 1235)   │
│  a quale conto su Mobile?                                │
│                                                          │
│  ○ test (ID=1, 0 transazioni)                           │
│  ○ Carta Credito (ID=3, 150 transazioni)                │
│                                                          │
│  Dopo il collegamento, scegli modalità sync:            │
│  ○ Replace (cancella destinazione)                      │
│  ● Merge (unisci, mantieni entrambi)                    │
│  ○ NewOnly (solo nuove transazioni)                     │
│                                                          │
│  [Annulla] [Collega]                                    │
└──────────────────────────────────────────────────────────┘
```

---

### **STEP 4: Anteprima & Gestione Conflitti**

```
┌────────────────────────────────────────────────────────────────┐
│  Anteprima Sincronizzazione                                    │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Operazioni da eseguire:                                       │
│                                                                │
│  ✅ Banca Credito Coop                                        │
│     • Crea nuovo conto su Mobile (ID=4)                       │
│     • Copia 1235 transazioni Desktop→Mobile                   │
│                                                                │
│  ✅ test                                                       │
│     • Desktop→Mobile Merge                                    │
│     • Aggiungi 33 nuove transazioni (0 duplicati)             │
│     • Risultato finale: 33 transazioni su Mobile              │
│                                                                │
│  ⚠️  CONFLITTO: Banca Credito - Gestione ID                  │
│      Desktop usa ID=1, ma Mobile ID=1 è occupato da "test"    │
│      [Risolvi Conflitto...] ← Apre dialog STEP 4.1           │
│                                                                │
│  📊 Riepilogo:                                                │
│     • Conti da creare: 1                                      │
│     • Conti da aggiornare: 1                                  │
│     • Transazioni totali da copiare: 1268                     │
│     • Spazio richiesto: ~2.5 MB                               │
│                                                                │
│  ⚠️  ATTENZIONE:                                              │
│     • Nessuna transazione classificata verrà persa            │
│     • Verrà creato backup automatico prima di procedere       │
│                                                                │
│  [← Modifica Selezione] [Crea Backup e Procedi →]            │
└────────────────────────────────────────────────────────────────┘
```

**STEP 4.1: Dialog Risoluzione Conflitto ID**

```
┌─────────────────────────────────────────────────────────┐
│  ⚠️  Conflitto ID Rilevato                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Vuoi creare "Banca Credito Coop" (ID=1 su Desktop)    │
│  su Mobile, ma Mobile ha già un conto con ID=1:        │
│                                                         │
│  • "test" (ID=1, 0 transazioni)                        │
│                                                         │
│  Scegli come procedere:                                │
│                                                         │
│  ● Crea con nuovo ID (ID=4) [CONSIGLIATO]              │
│    ✓ Non modifica conti esistenti                      │
│    ✓ Più sicuro                                        │
│                                                         │
│  ○ Sostituisci "test" (ID=1) con "Banca Credito"       │
│    ⚠️  Cancellerà il conto "test" (0 trans verranno   │
│        perse, ma sono 0 quindi OK)                     │
│                                                         │
│  ○ Rinomina "test" a ID=4 e usa ID=1 per "Banca..."   │
│    ⚠️  Operazione complessa, sconsigliata             │
│                                                         │
│  [Annulla] [Applica Soluzione]                         │
└─────────────────────────────────────────────────────────┘
```

---

### **STEP 5: Esecuzione con Progress**

```
┌──────────────────────────────────────────────────────────┐
│  🔄 Sincronizzazione in corso...                        │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  ✅ Backup creato                    [██████████] 100%  │
│     /backup/pre_sync_20250127_183045.zip                │
│                                                          │
│  ⏳ Banca Credito Coop               [████████--]  80%  │
│     Creando conto (ID=4)... Copiando 1235 trans...      │
│                                                          │
│  ⏸️  test                             [----------]   0%  │
│     In attesa...                                        │
│                                                          │
│  📊 Progresso totale: 40% (502/1268 transazioni)        │
│                                                          │
│  [Annulla Sync]                                         │
└──────────────────────────────────────────────────────────┘
```

---

### **STEP 6: Risultato Finale**

```
┌──────────────────────────────────────────────────────────────┐
│  ✅ Sincronizzazione Completata!                            │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Risultati:                                                  │
│                                                              │
│  ✅ Banca Credito Coop                                      │
│     • Conto creato su Mobile (ID=4)                         │
│     • 1235 transazioni copiate                              │
│     • Ultima transazione: 2025-12-25                        │
│                                                              │
│  ✅ test                                                     │
│     • 33 nuove transazioni aggiunte su Mobile               │
│     • 0 duplicati trovati e saltati                         │
│     • Totale finale: 33 transazioni                         │
│                                                              │
│  📊 Riepilogo:                                              │
│     • Tempo impiegato: 2.3 secondi                          │
│     • Transazioni totali processate: 1268                   │
│     • Nuove transazioni: 1268                               │
│     • Duplicati saltati: 0                                  │
│                                                              │
│  💾 Backup: /backup/pre_sync_20250127_183045.zip           │
│                                                              │
│  [Chiudi] [Visualizza Log Dettagliato]                     │
└──────────────────────────────────────────────────────────────┘
```

---

## 📊 MODELLI DATI

### **AccountMapping** (Nuovo modello)

```csharp
public class AccountMapping
{
    public int DesktopAccountId { get; set; }
    public string DesktopAccountName { get; set; }
    public int DesktopTransactionCount { get; set; }

    public int? MobileAccountId { get; set; }  // null = non esiste su mobile
    public string? MobileAccountName { get; set; }
    public int? MobileTransactionCount { get; set; }

    public MappingStatus Status { get; set; }
    public SyncAction SelectedAction { get; set; }

    // Per conflitti ID
    public bool HasIdConflict { get; set; }
    public ConflictResolution? ConflictResolution { get; set; }
}

public enum MappingStatus
{
    BothExist,           // Conto esiste su entrambi
    DesktopOnly,         // Esiste solo su desktop
    MobileOnly,          // Esiste solo su mobile
    Linked,              // Collegato manualmente a conto diverso
    ConflictId           // Conflitto ID
}

public enum SyncAction
{
    Skip,
    DesktopToMobileReplace,
    DesktopToMobileMerge,
    DesktopToMobileNewOnly,
    MobileToDesktopReplace,
    MobileToDesktopMerge,
    MobileToDesktopNewOnly,
    CreateOnMobileNewId,
    CreateOnMobileSameId,
    CreateOnDesktopNewId,
    CreateOnDesktopSameId,
    LinkToExisting
}

public enum ConflictResolution
{
    UseNewId,            // Crea con nuovo ID incrementale
    ReplaceExisting,     // Sostituisci conto esistente
    RenameExisting       // Rinomina conto esistente e usa ID originale
}
```

---

## 🔍 LOGICA MATCHING CONTI

### **Auto-Matching Algorithm**

```csharp
public List<AccountMapping> AutoMatchAccounts(
    List<SyncAccount> desktopAccounts,
    List<SyncAccount> mobileAccounts)
{
    var mappings = new List<AccountMapping>();
    var mobileAccountsUsed = new HashSet<int>();

    foreach (var desktop in desktopAccounts)
    {
        var mobile = mobileAccounts.FirstOrDefault(m =>
            m.Id == desktop.Id &&
            m.Nome.Equals(desktop.Nome, StringComparison.OrdinalIgnoreCase));

        if (mobile != null)
        {
            // MATCH PERFETTO: Stesso ID e Nome
            mappings.Add(new AccountMapping
            {
                DesktopAccountId = desktop.Id,
                DesktopAccountName = desktop.Nome,
                DesktopTransactionCount = desktop.TransactionCount,
                MobileAccountId = mobile.Id,
                MobileAccountName = mobile.Nome,
                MobileTransactionCount = mobile.TransactionCount,
                Status = MappingStatus.BothExist,
                SelectedAction = SyncAction.DesktopToMobileMerge,  // Default
                HasIdConflict = false
            });
            mobileAccountsUsed.Add(mobile.Id);
        }
        else
        {
            // Controlla se esiste mobile con STESSO ID ma NOME diverso
            var mobileWithSameId = mobileAccounts.FirstOrDefault(m => m.Id == desktop.Id);

            if (mobileWithSameId != null)
            {
                // POSSIBILE CONFLITTO: Stesso ID, Nome diverso
                mappings.Add(new AccountMapping
                {
                    DesktopAccountId = desktop.Id,
                    DesktopAccountName = desktop.Nome,
                    DesktopTransactionCount = desktop.TransactionCount,
                    MobileAccountId = mobileWithSameId.Id,
                    MobileAccountName = mobileWithSameId.Nome,
                    MobileTransactionCount = mobileWithSameId.TransactionCount,
                    Status = MappingStatus.ConflictId,
                    SelectedAction = SyncAction.Skip,  // Richiede intervento utente
                    HasIdConflict = true
                });
                mobileAccountsUsed.Add(mobileWithSameId.Id);
            }
            else
            {
                // DESKTOP ONLY: Conto esiste solo su desktop
                // Controlla se creare con stesso ID causerebbe conflitto
                bool idConflict = mobileAccounts.Any(m => m.Id == desktop.Id);

                mappings.Add(new AccountMapping
                {
                    DesktopAccountId = desktop.Id,
                    DesktopAccountName = desktop.Nome,
                    DesktopTransactionCount = desktop.TransactionCount,
                    MobileAccountId = null,
                    MobileAccountName = null,
                    MobileTransactionCount = null,
                    Status = MappingStatus.DesktopOnly,
                    SelectedAction = idConflict
                        ? SyncAction.CreateOnMobileNewId
                        : SyncAction.CreateOnMobileSameId,
                    HasIdConflict = idConflict,
                    ConflictResolution = idConflict ? ConflictResolution.UseNewId : null
                });
            }
        }
    }

    // Aggiungi conti che esistono SOLO su mobile
    foreach (var mobile in mobileAccounts)
    {
        if (!mobileAccountsUsed.Contains(mobile.Id))
        {
            mappings.Add(new AccountMapping
            {
                DesktopAccountId = 0,  // N/A
                DesktopAccountName = null,
                DesktopTransactionCount = null,
                MobileAccountId = mobile.Id,
                MobileAccountName = mobile.Nome,
                MobileTransactionCount = mobile.TransactionCount,
                Status = MappingStatus.MobileOnly,
                SelectedAction = SyncAction.Skip,  // Default: non portare Mobile→Desktop
                HasIdConflict = false
            });
        }
    }

    return mappings;
}
```

---

## 🔌 ENDPOINT API

### **Nuovo: GET /accounts/detailed**

Restituisce conti con info dettagliate per matching:

```json
{
  "success": true,
  "accounts": [
    {
      "id": 1,
      "nome": "test",
      "saldoIniziale": 0.0,
      "icona": "💳",
      "colore": "#512BD4",
      "transactionCount": 0,
      "latestTransactionDate": null,
      "latestModifiedAt": null,
      "databaseFile": "MoneyMind_Conto_001.db",
      "classifiedCount": 0,
      "uniqueMacroCategories": 0
    }
  ],
  "nextAvailableId": 4  // ← NUOVO: Prossimo ID libero
}
```

### **Nuovo: POST /sync/validate-mappings**

Valida mappings prima di eseguire sync:

**Request**:
```json
{
  "mappings": [
    {
      "desktopAccountId": 1,
      "desktopAccountName": "Banca Credito",
      "mobileAccountId": null,
      "action": "CreateOnMobileNewId",
      "conflictResolution": "UseNewId"
    }
  ]
}
```

**Response**:
```json
{
  "success": true,
  "validationErrors": [],
  "warnings": [
    {
      "accountId": 1,
      "message": "Verrà creato nuovo conto con ID=4"
    }
  ],
  "estimatedDiskSpace": 2500000,  // bytes
  "estimatedTime": 2.3  // secondi
}
```

### **Modificato: POST /sync/execute**

Ora accetta `mappings` invece di semplice lista conti:

**Request**:
```json
{
  "mappings": [
    {
      "desktopAccountId": 1,
      "desktopAccountName": "Banca Credito",
      "mobileAccountId": null,
      "action": "CreateOnMobileNewId",
      "assignedMobileId": 4,
      "sourceTransactions": [ /* lista transazioni */ ]
    }
  ],
  "confirmed": true
}
```

---

## 🎨 UI DESKTOP (Modifiche)

### **Modificare: WiFiSyncViewModel.vb**

Aggiungere:
```vb
Public Property AccountMappings As ObservableCollection(Of AccountMapping)
Public Property NextAvailableMobileId As Integer
Public Property NextAvailableDesktopId As Integer

Public Async Function LoadAndMatchAccountsAsync() As Task
    ' Carica desktop accounts
    ' Carica mobile accounts
    ' Esegui auto-matching
    AccountMappings = AutoMatchAccounts(desktopAccounts, mobileAccounts)
End Function

Public Function CanProceedWithSync() As Boolean
    ' Verifica che non ci siano conflitti irrisolti
    Return Not AccountMappings.Any(Function(m)
        m.HasIdConflict AndAlso m.ConflictResolution Is Nothing)
End Function
```

---

## 📱 UI MOBILE (Da Creare)

### **WifiSyncPage.xaml** (Attualmente solo server)

Aggiungere TAB per CLIENT MODE:

```xml
<TabbedPage>
    <ContentPage Title="Server">
        <!-- UI esistente: Avvia/Arresta server -->
    </ContentPage>

    <ContentPage Title="Client">
        <StackLayout>
            <Label Text="Connetti a Desktop" />
            <Entry Placeholder="IP Desktop (es. 192.168.1.100)" />
            <Button Text="Connetti" Clicked="OnConnectToDesktop" />

            <!-- Dopo connessione, mostra tabella mapping -->
            <CollectionView ItemsSource="{Binding AccountMappings}">
                <!-- Lista conti con azioni -->
            </CollectionView>

            <Button Text="Sincronizza" Clicked="OnExecuteSync" />
        </StackLayout>
    </ContentPage>
</TabbedPage>
```

---

## ⚠️ GESTIONE CONFLITTI - Riepilogo

| Tipo Conflitto | Rilevamento | Soluzioni Disponibili |
|----------------|-------------|----------------------|
| **ID occupato su destinazione** | AutoMatch | UseNewId, ReplaceExisting, RenameExisting |
| **Stesso ID, nome diverso** | AutoMatch | Conferma=SameAccount, Conferma=DifferentAccounts |
| **Transazioni classificate perse** | PrepareSyncValidation | ShowWarning + RequireConfirmation |
| **Spazio insufficiente** | PrepareSyncValidation | Abort + ShowError |

---

## 🚀 IMPLEMENTAZIONE - Priorità

### **FASE 1: Fix Immediato (Oggi)**
- ✅ Fix bug DatabaseService singleton → FATTO
- ⏳ Fix app desktop mostra dati cached → Aggiungere refresh dopo connessione
- ⏳ Implementare AccountMapping model (C# mobile)
- ⏳ Implementare GET /accounts/detailed con nextAvailableId

### **FASE 2: Auto-Matching (Domani)**
- Implementare AutoMatchAccounts algoritmo (mobile)
- Modificare WiFiSyncViewModel (desktop) per usare AccountMappings
- UI Desktop: Mostrare tabella mapping invece di 2 liste separate

### **FASE 3: Gestione Conflitti**
- Dialog risoluzione conflitto ID
- Validazione pre-sync con warnings
- Preview dettagliata operazioni

### **FASE 4: UI Mobile Client**
- Tab "Client" in WifiSyncPage
- Connessione a Desktop
- Selezione conti e azioni

### **FASE 5: Testing Completo**
- Test tutti gli 8 scenari identificati
- Test edge cases
- Test rollback con backup

---

## 📝 NOTE IMPORTANTI

1. **Backup obbligatorio**: SEMPRE prima di ogni sync
2. **Transazioni classificate**: Solo Desktop, warning se Mobile→Desktop Replace
3. **ID assignment**: Mobile decide next ID, Desktop decide next ID separatamente
4. **Duplicati**: Rilevamento tramite Levenshtein (già implementato nel codice esistente)
5. **UI responsive**: Progress bar real-time durante sync
6. **Error handling**: Rollback automatico da backup se sync fallisce

---

**Fine documento - Versione 1.0**
