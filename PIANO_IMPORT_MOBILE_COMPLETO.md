# 🎯 PIANO COMPLETO: IMPORT SYSTEM MOBILE (come Desktop)

> **Obiettivo**: Portare su Android/iOS l'intero sistema di importazione desktop con configurazioni, wizard step-by-step, e funzionalità "Nuova", "Carica", "Modifica", "Elimina".

---

## 📋 EXECUTIVE SUMMARY

### Sistema Desktop Analizzato

Il sistema desktop ha un **wizard integrato a 4 step** con gestione avanzata configurazioni:

```
STEP 0: File Selection (selezione file CSV/Excel)
STEP 1: Configuration Management (lista config + 4 bottoni)
STEP 2: Header Selection (selezione riga intestazioni)
STEP 3: Column Mapping (mapping colonne + salvataggio config)
STEP 4: Validation (preview verde/rosso + import)
```

**4 Bottoni Chiave**:
1. **"Nuova Configurazione"** → Crea nuova config (flow completo Step 2→3→4)
2. **"Carica Selezionata"** → Applica config esistente (SALTA a Step 4)
3. **"Modifica"** → Modifica config esistente (va a Step 3)
4. **"Elimina"** → Elimina config (conferma + refresh lista)

### Obiettivo Mobile

Replicare **esattamente** la stessa UX e logica su .NET MAUI, adattando per touch/mobile.

---

## 🏗️ ARCHITETTURA MOBILE - PLAN

### OPZIONE A: Shell Navigation Multi-Page (❌ Troppo complessa)

Ogni step = pagina separata con navigation stack.

**Problemi**:
- Troppi back/forward
- Stato complesso tra pagine
- UX frammentata

### OPZIONE B: Single Page con CarouselView (**✅ CONSIGLIATA**)

Una `ImportWizardPage` con `CarouselView` per i 4 step.

**Vantaggi**:
- Stato centralizzato in un ViewModel
- Swipe nativo tra step
- Indicator grafico progress
- Bottoni "Indietro/Avanti" coerenti

### OPZIONE C: Shell Navigation + Modal per Config Management (**✅ SCELTA FINALE**)

**Perché questa è la migliore**:
- `ImportWizardPage` (Shell navigation) per step 2-3-4
- `ConfigSelectionPage` (modal) per gestione configurazioni con i 4 bottoni
- Mantiene la stessa logica desktop: "Carica Selezionata" skippa step, "Modifica" va a mapping

**Flow**:
```
MainPage → Click "Importa"
    ↓
ImportConfigSelectionPage (MODAL)
    • Lista configurazioni salvate
    • [➕ Nuova Configurazione]
    • [📂 Carica Selezionata]
    • [✏️ Modifica]
    • [🗑️ Elimina]
    ↓
    Se "Nuova" → ImportHeaderSelectionPage (step 2)
    Se "Carica" → ImportValidationPage (step 4) ← SKIP!
    Se "Modifica" → ImportColumnMappingPage (step 3)
    ↓
ImportHeaderSelectionPage (step 2)
    ↓
ImportColumnMappingPage (step 3)
    ↓
ImportValidationPage (step 4)
    ↓
Import DB → Torna a MainPage con refresh
```

---

## 📦 STRUTTURA FILES MOBILE

### 1. Models (ESISTENTI + NUOVI)

```
Models/
├── ConfigurazioneImportazione.cs          # ✅ ESISTE
├── ImportExportModels.cs                  # ✅ ESISTE (con HeaderRowNumber)
├── FilePreviewRow.cs                      # ✅ ESISTE
└── TransactionValidationRow.cs            # ⭐ NUOVO (per step validation)
```

**NUOVO: TransactionValidationRow.cs**
```csharp
public class TransactionValidationRow
{
    public int RowNumber { get; set; }
    public string Data { get; set; } = "";
    public string Importo { get; set; } = "";
    public string Descrizione { get; set; } = "";
    public bool HasErrors { get; set; }
    public string ErrorMessage { get; set; } = "";

    // UI Properties
    public Color RowColor => HasErrors ? Colors.LightCoral : Colors.LightGreen;
    public string StatusIcon => HasErrors ? "❌" : "✅";
}
```

---

### 2. Services (ESISTENTI + MODIFICHE)

```
Services/ImportExport/
├── IConfigurazioneImportazioneService.cs  # ✅ ESISTE
├── ConfigurazioneImportazioneService.cs   # ✅ ESISTE (con preset)
├── IImportExportService.cs                # ✅ ESISTE
├── ImportExportService.cs                 # ✅ ESISTE (con header custom)
└── IImportValidationService.cs            # ⭐ NUOVO
    └── ImportValidationService.cs         # ⭐ NUOVO
```

**NUOVO: IImportValidationService.cs**
```csharp
public interface IImportValidationService
{
    /// <summary>
    /// Valida tutte le righe del file prima dell'import
    /// </summary>
    Task<List<TransactionValidationRow>> ValidateFileAsync(
        string filePath,
        ColumnMapping mapping,
        int maxRows = 1000);

    /// <summary>
    /// Importa solo le righe valide nel database
    /// </summary>
    Task<ImportResult> ImportValidRowsAsync(
        List<TransactionValidationRow> validRows);
}
```

**NUOVO: ImportValidationService.cs**
```csharp
public class ImportValidationService : IImportValidationService
{
    private readonly IImportExportService _importExportService;
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    public async Task<List<TransactionValidationRow>> ValidateFileAsync(
        string filePath,
        ColumnMapping mapping,
        int maxRows = 1000)
    {
        var validationRows = new List<TransactionValidationRow>();

        // 1. Leggi file con header row custom
        var rows = await _importExportService.ReadFileAsync(
            filePath,
            mapping.HasHeader,
            mapping.HeaderRowNumber);

        var rowNumber = mapping.HasHeader ? mapping.HeaderRowNumber + 1 : 1;

        foreach (var row in rows.Take(maxRows))
        {
            var validationRow = new TransactionValidationRow
            {
                RowNumber = rowNumber
            };

            try
            {
                // 2. Valida Data
                if (mapping.DataColumn >= 0 && mapping.DataColumn < row.Length)
                {
                    var dataStr = row[mapping.DataColumn];
                    if (string.IsNullOrWhiteSpace(dataStr))
                    {
                        validationRow.HasErrors = true;
                        validationRow.ErrorMessage = "Data mancante";
                    }
                    else if (!DateTime.TryParseExact(dataStr, mapping.DateFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        validationRow.HasErrors = true;
                        validationRow.ErrorMessage = "Formato data non valido";
                    }
                    else
                    {
                        validationRow.Data = dataStr;
                    }
                }

                // 3. Valida Importo
                if (mapping.ImportoColumn >= 0 && mapping.ImportoColumn < row.Length)
                {
                    var importoStr = row[mapping.ImportoColumn];
                    if (string.IsNullOrWhiteSpace(importoStr))
                    {
                        validationRow.HasErrors = true;
                        validationRow.ErrorMessage = "Importo mancante";
                    }
                    else
                    {
                        // Usa parsing robusto esistente
                        var normalized = NormalizeDecimalString(importoStr, mapping.DecimalSeparator);
                        if (!decimal.TryParse(normalized, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out _))
                        {
                            validationRow.HasErrors = true;
                            validationRow.ErrorMessage = "Formato importo non valido";
                        }
                        else
                        {
                            validationRow.Importo = importoStr;
                        }
                    }
                }

                // 4. Valida Descrizione
                if (mapping.DescrizioneColumn >= 0 && mapping.DescrizioneColumn < row.Length)
                {
                    var descrizioneStr = row[mapping.DescrizioneColumn];
                    if (string.IsNullOrWhiteSpace(descrizioneStr))
                    {
                        validationRow.HasErrors = true;
                        validationRow.ErrorMessage = "Descrizione mancante";
                    }
                    else
                    {
                        validationRow.Descrizione = descrizioneStr;
                    }
                }
            }
            catch (Exception ex)
            {
                validationRow.HasErrors = true;
                validationRow.ErrorMessage = $"Errore: {ex.Message}";
            }

            validationRows.Add(validationRow);
            rowNumber++;
        }

        return validationRows;
    }

    public async Task<ImportResult> ImportValidRowsAsync(
        List<TransactionValidationRow> validRows)
    {
        var result = new ImportResult();
        result.TotalRows = validRows.Count;

        foreach (var validRow in validRows.Where(r => !r.HasErrors))
        {
            try
            {
                var transaction = new Transaction
                {
                    Data = DateTime.Parse(validRow.Data),
                    Importo = decimal.Parse(NormalizeDecimalString(validRow.Importo, ",")),
                    Descrizione = validRow.Descrizione.Trim()
                };

                await _databaseService.InsertTransactionAsync(transaction);
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Riga {validRow.RowNumber}: {ex.Message}");
            }
        }

        result.Success = result.ImportedCount > 0;
        return result;
    }

    private string NormalizeDecimalString(string input, string decimalSeparator)
    {
        // Riutilizza logica esistente in ImportExportService
        // (già implementata)
        return input;
    }
}
```

---

### 3. ViewModels (ESISTENTI + NUOVI)

```
ViewModels/
├── ImportConfigSelectionViewModel.cs      # ✅ ESISTE (con preset)
├── ImportHeaderSelectionViewModel.cs      # ✅ ESISTE
├── ImportViewModel.cs                     # ✅ ESISTE (mapping colonne)
└── ImportValidationViewModel.cs           # ⭐ NUOVO (step validation)
```

**MODIFICHE A ImportConfigSelectionViewModel.cs**

Aggiungere i 4 bottoni:

```csharp
public partial class ImportConfigSelectionViewModel : ObservableObject
{
    // ... esistente ...

    [ObservableProperty]
    private ConfigurazioneImportazione? configurazioneSelezionata;

    [ObservableProperty]
    private bool hasSelection;

    partial void OnConfigurazioneSelezionataChanged(ConfigurazioneImportazione? value)
    {
        HasSelection = value != null;
    }

    // ⭐ NUOVO: Nuova Configurazione
    [RelayCommand]
    private async Task CreateNewConfigurationAsync()
    {
        // Vai direttamente a step header selection (step 2)
        var navigationParameter = new Dictionary<string, object>
        {
            { "IsNewConfiguration", true }
        };

        await Shell.Current.GoToAsync("importHeaderSelection", navigationParameter);
    }

    // ⭐ NUOVO: Carica Selezionata (SKIP a validation!)
    [RelayCommand]
    private async Task LoadSelectedConfigurationAsync()
    {
        if (ConfigurazioneSelezionata == null)
        {
            await Shell.Current.DisplayAlert("⚠️ Attenzione",
                "Seleziona una configurazione da caricare", "OK");
            return;
        }

        // Aggiorna ultimo utilizzo
        await _configService.AggiornaUltimoUtilizzoAsync(ConfigurazioneSelezionata.Nome);

        // SKIP direttamente a validation (step 4)!
        var navigationParameter = new Dictionary<string, object>
        {
            { "Configurazione", ConfigurazioneSelezionata },
            { "FilePath", _selectedFilePath },  // Path file selezionato precedentemente
            { "SkipToValidation", true }        // Flag per skip
        };

        await Shell.Current.GoToAsync("importValidation", navigationParameter);
    }

    // ⭐ NUOVO: Modifica
    [RelayCommand]
    private async Task EditSelectedConfigurationAsync()
    {
        if (ConfigurazioneSelezionata == null)
        {
            await Shell.Current.DisplayAlert("⚠️ Attenzione",
                "Seleziona una configurazione da modificare", "OK");
            return;
        }

        // Vai a column mapping (step 3) con config pre-caricata
        var navigationParameter = new Dictionary<string, object>
        {
            { "Configurazione", ConfigurazioneSelezionata },
            { "FilePath", _selectedFilePath },
            { "IsEditMode", true }  // Flag per edit
        };

        await Shell.Current.GoToAsync("importColumnMapping", navigationParameter);
    }

    // ⭐ NUOVO: Elimina
    [RelayCommand]
    private async Task DeleteSelectedConfigurationAsync()
    {
        if (ConfigurazioneSelezionata == null) return;

        // Non eliminare preset
        if (ConfigurazioneSelezionata.IsPreset)
        {
            await Shell.Current.DisplayAlert("⚠️ Attenzione",
                "Non puoi eliminare una configurazione preset.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare '{ConfigurazioneSelezionata.Nome}'?\n" +
            "Questa operazione non può essere annullata.",
            "Elimina",
            "Annulla");

        if (confirm)
        {
            await _configService.EliminaConfigurazioneAsync(ConfigurazioneSelezionata.Nome);

            // Reset selezione
            ConfigurazioneSelezionata = null;

            // Refresh lista
            await LoadConfigurazioniAsync();

            StatusMessage = "✅ Configurazione eliminata";
        }
    }
}
```

**NUOVO: ImportValidationViewModel.cs**

```csharp
[QueryProperty(nameof(Configurazione), "Configurazione")]
[QueryProperty(nameof(FilePath), "FilePath")]
[QueryProperty(nameof(SkipToValidation), "SkipToValidation")]
[QueryProperty(nameof(ColumnMapping), "ColumnMapping")]
public partial class ImportValidationViewModel : ObservableObject
{
    private readonly IImportValidationService _validationService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ConfigurazioneImportazione? configurazione;

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private bool skipToValidation;

    [ObservableProperty]
    private ColumnMapping? columnMapping;

    [ObservableProperty]
    private ObservableCollection<TransactionValidationRow> validationRows = new();

    [ObservableProperty]
    private int validCount;

    [ObservableProperty]
    private int errorCount;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool canImport;

    public string ValidationSummary =>
        $"✅ {ValidCount} valide | ❌ {ErrorCount} errori | 📊 Totale: {TotalCount}";

    public async Task InitializeAsync()
    {
        await ValidateFileAsync();
    }

    partial void OnConfigurazioneChanged(ConfigurazioneImportazione? value)
    {
        if (value != null && SkipToValidation)
        {
            // Se arriva da "Carica Selezionata", usa mapping dalla configurazione
            ColumnMapping = value.ToColumnMapping();
        }
    }

    [RelayCommand]
    private async Task ValidateFileAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Validazione in corso...";

            var mapping = ColumnMapping ?? Configurazione?.ToColumnMapping();

            if (mapping == null || string.IsNullOrEmpty(FilePath))
            {
                StatusMessage = "❌ Mapping o file non disponibili";
                return;
            }

            // Esegui validazione
            var rows = await _validationService.ValidateFileAsync(FilePath, mapping, 1000);

            ValidationRows.Clear();
            foreach (var row in rows)
            {
                ValidationRows.Add(row);
            }

            // Calcola statistiche
            ValidCount = rows.Count(r => !r.HasErrors);
            ErrorCount = rows.Count(r => r.HasErrors);
            TotalCount = rows.Count;

            CanImport = ValidCount > 0;
            StatusMessage = ValidationSummary;

            _loggingService.LogInfo($"Validation completed: {ValidCount} valid, {ErrorCount} errors");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during validation", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
            CanImport = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (!CanImport)
        {
            await Shell.Current.DisplayAlert("⚠️ Attenzione",
                "Non ci sono righe valide da importare", "OK");
            return;
        }

        // Se ci sono errori, chiedi conferma
        if (ErrorCount > 0)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Conferma Importazione",
                $"Trovate {ErrorCount} righe con errori.\n" +
                $"Vuoi procedere importando solo le {ValidCount} righe valide?",
                "Importa",
                "Annulla");

            if (!confirm) return;
        }
        else
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Conferma Importazione",
                $"Importare {ValidCount} transazioni?",
                "Importa",
                "Annulla");

            if (!confirm) return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Importazione in corso...";

            // Importa solo righe valide
            var validRows = ValidationRows.Where(r => !r.HasErrors).ToList();
            var result = await _validationService.ImportValidRowsAsync(validRows);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert(
                    "✅ Import Completato",
                    $"Importate: {result.ImportedCount}\n" +
                    $"Errori: {result.ErrorCount}",
                    "OK");

                // Torna alla main page con refresh
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "❌ Errore",
                    string.Join("\n", result.Errors.Take(5)),
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during import", ex);
            await Shell.Current.DisplayAlert("❌ Errore", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
```

---

### 4. Views (MODIFICHE + NUOVE)

```
Views/
├── ImportConfigSelectionPage.xaml         # ✅ ESISTE (modificare UI)
├── ImportHeaderSelectionPage.xaml         # ✅ ESISTE
├── ImportPage.xaml                        # ✅ ESISTE (rinominare in ImportColumnMappingPage)
└── ImportValidationPage.xaml              # ⭐ NUOVO (step 4 validation)
```

**MODIFICHE A ImportConfigSelectionPage.xaml**

Aggiungere UI per i 4 bottoni:

```xml
<ContentPage xmlns="..."
             Title="Importa - Seleziona Configurazione">

    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="20">

            <!-- Header -->
            <Frame BackgroundColor="{StaticResource Primary}">
                <Label Text="📋 Gestione Configurazioni Import"
                       FontSize="20" FontAttributes="Bold" TextColor="White"/>
            </Frame>

            <!-- Bottoni Azione -->
            <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto"
                  ColumnSpacing="12" RowSpacing="12">

                <!-- ⭐ Bottone 1: Nuova Configurazione -->
                <Button Grid.Row="0" Grid.Column="0"
                        Text="➕ Nuova"
                        Command="{Binding CreateNewConfigurationCommand}"
                        BackgroundColor="{StaticResource Secondary}"/>

                <!-- ⭐ Bottone 2: Carica Selezionata -->
                <Button Grid.Row="0" Grid.Column="1"
                        Text="📂 Carica"
                        Command="{Binding LoadSelectedConfigurationCommand}"
                        IsEnabled="{Binding HasSelection}"
                        BackgroundColor="{StaticResource Primary}"/>

                <!-- ⭐ Bottone 3: Modifica -->
                <Button Grid.Row="1" Grid.Column="0"
                        Text="✏️ Modifica"
                        Command="{Binding EditSelectedConfigurationCommand}"
                        IsEnabled="{Binding HasSelection}"
                        BackgroundColor="Orange"/>

                <!-- ⭐ Bottone 4: Elimina -->
                <Button Grid.Row="1" Grid.Column="1"
                        Text="🗑️ Elimina"
                        Command="{Binding DeleteSelectedConfigurationCommand}"
                        IsEnabled="{Binding HasSelection}"
                        BackgroundColor="{StaticResource Danger}"/>
            </Grid>

            <!-- Lista Configurazioni -->
            <Label Text="Configurazioni Disponibili:"
                   FontSize="16" FontAttributes="Bold"/>

            <CollectionView ItemsSource="{Binding Configurazioni}"
                            SelectionMode="Single"
                            SelectedItem="{Binding ConfigurazioneSelezionata}">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:ConfigurazioneImportazione">
                        <Frame Padding="12" Margin="0,4" CornerRadius="8"
                               BackgroundColor="{AppThemeBinding Light=White, Dark={StaticResource Gray900}}"
                               BorderColor="{Binding IsSelected, Converter={StaticResource BoolToColorConverter},
                                             ConverterParameter='{StaticResource Primary}|Transparent'}">

                            <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*,Auto">

                                <!-- Nome -->
                                <HorizontalStackLayout Grid.Row="0" Spacing="8">
                                    <Label Text="{Binding IsPreset, Converter={StaticResource BoolToStringConverter},
                                                  ConverterParameter='⭐|📝'}"
                                           FontSize="16"/>
                                    <Label Text="{Binding Nome}"
                                           FontSize="16" FontAttributes="Bold"/>
                                </HorizontalStackLayout>

                                <!-- Info -->
                                <Label Grid.Row="1">
                                    <Label.FormattedText>
                                        <FormattedString>
                                            <Span Text="📍 Riga: " FontAttributes="Bold"/>
                                            <Span Text="{Binding RigaIntestazione}"/>
                                            <Span Text=" | Sep: "/>
                                            <Span Text="{Binding Separatore}"/>
                                        </FormattedString>
                                    </Label.FormattedText>
                                </Label>

                                <!-- Ultimo utilizzo -->
                                <Label Grid.Row="2"
                                       Text="{Binding UltimoUtilizzo, StringFormat='Ultimo uso: {0:dd/MM/yyyy HH:mm}'}"
                                       FontSize="11" TextColor="Gray"/>

                                <!-- Checkbox Selected -->
                                <CheckBox Grid.Column="1" Grid.RowSpan="3"
                                          IsChecked="{Binding IsSelected}"
                                          VerticalOptions="Center"/>
                            </Grid>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <!-- Status -->
            <Label Text="{Binding StatusMessage}"
                   HorizontalTextAlignment="Center"
                   TextColor="{StaticResource Primary}"/>

            <!-- Cancel -->
            <Button Text="Annulla"
                    Command="{Binding CancelCommand}"
                    BackgroundColor="Gray"/>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

**NUOVO: ImportValidationPage.xaml**

```xml
<ContentPage xmlns="..."
             Title="Importa - Validazione"
             x:DataType="viewmodels:ImportValidationViewModel">

    <Grid RowDefinitions="Auto,*,Auto">

        <!-- Header con statistiche -->
        <Frame Grid.Row="0" BackgroundColor="{StaticResource Primary}"
               Padding="16" Margin="16,16,16,0" CornerRadius="12">
            <VerticalStackLayout Spacing="8">
                <Label Text="📊 Validazione Dati"
                       FontSize="20" FontAttributes="Bold" TextColor="White"/>

                <Label Text="{Binding ValidationSummary}"
                       FontSize="14" TextColor="White"/>
            </VerticalStackLayout>
        </Frame>

        <!-- Lista validazione con colori -->
        <CollectionView Grid.Row="1"
                        ItemsSource="{Binding ValidationRows}"
                        Margin="16,8">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:TransactionValidationRow">
                    <Frame Padding="12" Margin="0,4" CornerRadius="8"
                           BackgroundColor="{Binding RowColor}">
                        <Grid ColumnDefinitions="40,*,80" RowDefinitions="Auto,Auto"
                              ColumnSpacing="8" RowSpacing="4">

                            <!-- Icon -->
                            <Label Grid.RowSpan="2"
                                   Text="{Binding StatusIcon}"
                                   FontSize="20"
                                   VerticalOptions="Center"/>

                            <!-- Riga num -->
                            <Label Grid.Column="1"
                                   Text="{Binding RowNumber, StringFormat='Riga {0}'}"
                                   FontSize="11" TextColor="Gray"/>

                            <!-- Data transazione -->
                            <Label Grid.Column="1" Grid.Row="1">
                                <Label.FormattedText>
                                    <FormattedString>
                                        <Span Text="{Binding Data}" FontAttributes="Bold"/>
                                        <Span Text=" - "/>
                                        <Span Text="{Binding Descrizione}"/>
                                    </FormattedString>
                                </Label.FormattedText>
                            </Label>

                            <!-- Importo -->
                            <Label Grid.Column="2" Grid.RowSpan="2"
                                   Text="{Binding Importo}"
                                   FontSize="16" FontAttributes="Bold"
                                   VerticalOptions="Center"
                                   HorizontalOptions="End"/>

                            <!-- Errore (se presente) -->
                            <Label Grid.Column="1" Grid.ColumnSpan="2" Grid.Row="2"
                                   Text="{Binding ErrorMessage}"
                                   TextColor="DarkRed"
                                   FontSize="11"
                                   IsVisible="{Binding HasErrors}"/>
                        </Grid>
                    </Frame>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <!-- Bottoni azione -->
        <Grid Grid.Row="2" Padding="16" ColumnDefinitions="*,*" ColumnSpacing="12">
            <Button Grid.Column="0"
                    Text="⬅️ Indietro"
                    Command="{Binding BackCommand}"
                    BackgroundColor="Gray"/>

            <Button Grid.Column="1"
                    Text="✅ Conferma Import"
                    Command="{Binding ConfirmImportCommand}"
                    IsEnabled="{Binding CanImport}"
                    BackgroundColor="{StaticResource Primary}"/>
        </Grid>

        <!-- Loading overlay -->
        <ActivityIndicator Grid.RowSpan="3"
                           IsRunning="{Binding IsLoading}"
                           IsVisible="{Binding IsLoading}"
                           Color="{StaticResource Primary}"/>
    </Grid>
</ContentPage>
```

---

## 🔀 FLOW DIAGRAM COMPLETO MOBILE

```
┌─────────────────────────────────────────────────────────────────┐
│ MainPage                                                        │
│ [Bottone "Importa"]                                             │
└─────────────────┬───────────────────────────────────────────────┘
                  │ Click "Importa"
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: ImportConfigSelectionPage (MODAL)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ ┌─────────────────────────────────────────────────┐            │
│ │ FILE SELECTION (prima dei bottoni)             │            │
│ │ [📂 Scegli File CSV/Excel]                      │            │
│ │ SelectedFile: estratto_conto.csv                │            │
│ └─────────────────────────────────────────────────┘            │
│                                                                  │
│ ┌─────────────────────────────────────────────────┐            │
│ │ 4 BOTTONI AZIONE                                │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ [➕ Nuova]    [📂 Carica]                       │            │
│ │ [✏️ Modifica]  [🗑️ Elimina]                     │            │
│ └─────────────────────────────────────────────────┘            │
│                                                                  │
│ ┌─────────────────────────────────────────────────┐            │
│ │ LISTA CONFIGURAZIONI                            │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ ⭐ BCC - Banca di Credito Cooperativo           │            │
│ │    📍 Riga: 1 | Sep: ;                          │            │
│ │    Ultimo uso: 10/01/2026 14:30                 │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ ⭐ Intesa San Paolo                              │            │
│ │    📍 Riga: 12 | Sep: ;                         │            │
│ │    Ultimo uso: 08/01/2026 10:15                 │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ 📝 Config_MiaBanca_20260105                     │            │
│ │    📍 Riga: 8 | Sep: ;                          │            │
│ │    Ultimo uso: 05/01/2026 16:45                 │            │
│ └─────────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────────┘
         │                    │                    │
         │ Click "Nuova"      │ Click "Carica"     │ Click "Modifica"
         │                    │                    │
         ▼                    │                    │
┌────────────────────────┐   │                    │
│ STEP 2:                │   │                    │
│ ImportHeaderSelection  │   │                    │
│ Page                   │   │                    │
├────────────────────────┤   │                    │
│ [📂 File già caricato] │   │                    │
│                        │   │                    │
│ Anteprima 20 righe:    │   │                    │
│ 001  Info banca...     │   │                    │
│ 002  Periodo: ...      │   │                    │
│ ...                    │   │                    │
│ 012  Data;Importo;... ←│   │                    │
│ 013  01/01/2026;...    │   │                    │
│                        │   │                    │
│ Riga header: [12]      │   │                    │
│ ☑️ Ha intestazioni     │   │                    │
│                        │   │                    │
│ [Avanti →]             │   │                    │
└────────┬───────────────┘   │                    │
         │                    │                    │
         ▼                    │                    ▼
┌────────────────────────────┐│    ┌──────────────────────────────┐
│ STEP 3:                    ││    │ STEP 3:                      │
│ ImportColumnMappingPage    ││    │ ImportColumnMappingPage      │
│ (ex ImportPage)            ││    │ (EDIT MODE)                  │
├────────────────────────────┤│    ├──────────────────────────────┤
│ Mapping Colonne:           ││    │ Mapping PRE-CARICATO:        │
│                            ││    │                              │
│ Data:        [Auto ✅]     ││    │ Data:        [Col 0 ✅]      │
│ Importo:     [Auto ✅]     ││    │ Importo:     [Col 2 ✅]      │
│ Descrizione: [Auto ✅]     ││    │ Descrizione: [Col 4 ✅]      │
│                            ││    │                              │
│ Formato:                   ││    │ Formato:                     │
│ • Data: dd/MM/yyyy         ││    │ • Data: dd/MM/yyyy           │
│ • Decimali: ,              ││    │ • Decimali: ,                │
│                            ││    │                              │
│ Nome Config:               ││    │ Nome Config:                 │
│ [Config_BCC_20260110_1430] ││    │ [Config_MiaBanca_20260105]   │
│                            ││    │ (Può cambiare o lasciare)    │
│ [👁️ Anteprima]             ││    │                              │
│ [Salva e Continua →]       ││    │ [Salva Modifiche →]          │
└────────┬───────────────────┘│    └──────────┬───────────────────┘
         │ Salva config       │               │ Sovrascrive config
         │                    │               │
         ▼                    ▼               ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 4: ImportValidationPage                                   │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐            │
│ │ 📊 Validazione Dati                             │            │
│ │ ✅ 95 valide | ❌ 5 errori | 📊 Totale: 100     │            │
│ └─────────────────────────────────────────────────┘            │
│                                                                  │
│ Lista Transazioni (scorrevole):                                │
│ ┌─────────────────────────────────────────────────┐            │
│ │ ✅ Riga 11                       € 100,50       │ ← VERDE    │
│ │    01/01/2026 - Spesa supermercato              │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ ✅ Riga 12                       € -50,00       │ ← VERDE    │
│ │    02/01/2026 - Pagamento bolletta              │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ ❌ Riga 13                       N/A            │ ← ROSSO    │
│ │    ERRORE - Data non valida                     │            │
│ ├─────────────────────────────────────────────────┤            │
│ │ ✅ Riga 14                       € 1.500,00     │ ← VERDE    │
│ │    05/01/2026 - Stipendio gennaio               │            │
│ └─────────────────────────────────────────────────┘            │
│                                                                  │
│ [⬅️ Indietro]           [✅ Conferma Import]                    │
│                          (disabilitato se 0 valide)             │
└─────────────────────────────────────────────────────────────────┘
                                  │
                                  │ Click "Conferma Import"
                                  │
                                  ▼
                        ┌──────────────────────┐
                        │ MessageBox Conferma  │
                        │ "Importare 95        │
                        │  transazioni?"       │
                        │ [Sì] [No]            │
                        └──────┬───────────────┘
                               │ Sì
                               ▼
                    ┌─────────────────────────┐
                    │ Import nel Database     │
                    │ (solo righe valide)     │
                    └──────┬──────────────────┘
                           │
                           ▼
                 ┌──────────────────────────────┐
                 │ MessageBox Successo          │
                 │ "✅ Importate 95 transazioni"│
                 │ [OK]                         │
                 └──────┬───────────────────────┘
                        │
                        ▼
              ┌──────────────────────┐
              │ Torna a MainPage     │
              │ con refresh stats    │
              └──────────────────────┘
```

---

## 🎯 LOGICA SKIP STEP (Carica Selezionata)

Quando user clicca "Carica Selezionata", il sistema **salta step 2 e 3** e va direttamente a validazione.

**Implementazione**:

```csharp
// ImportConfigSelectionViewModel.cs
[RelayCommand]
private async Task LoadSelectedConfigurationAsync()
{
    // ...

    var navigationParameter = new Dictionary<string, object>
    {
        { "Configurazione", ConfigurazioneSelezionata },
        { "FilePath", _selectedFilePath },
        { "SkipToValidation", true }  // ⭐ Flag chiave
    };

    // SKIP diretto a validation
    await Shell.Current.GoToAsync("importValidation", navigationParameter);
}

// ImportValidationViewModel.cs
partial void OnConfigurazioneChanged(ConfigurazioneImportazione? value)
{
    if (value != null && SkipToValidation)
    {
        // Usa mapping dalla configurazione caricata
        ColumnMapping = value.ToColumnMapping();

        // Auto-valida immediatamente
        _ = ValidateFileAsync();
    }
}
```

**Comportamento**:
1. User carica config esistente
2. Sistema va direttamente a `ImportValidationPage`
3. ViewModel auto-carica mapping da config
4. Esegue validazione automaticamente
5. Mostra risultati (verde/rosso)
6. User può importare subito

**Nessun passaggio manuale richiesto!** ✅

---

## 📝 REGISTRAZIONE SERVIZI E ROUTE

### MauiProgram.cs

```csharp
// Services
services.AddSingleton<IConfigurazioneImportazioneService, ConfigurazioneImportazioneService>();
services.AddSingleton<IImportExportService, ImportExportService>();
services.AddSingleton<IImportValidationService, ImportValidationService>();  // ⭐ NUOVO

// ViewModels
services.AddTransient<ImportConfigSelectionViewModel>();
services.AddTransient<ImportHeaderSelectionViewModel>();
services.AddTransient<ImportViewModel>();  // Rinominato in ImportColumnMappingViewModel
services.AddTransient<ImportValidationViewModel>();  // ⭐ NUOVO

// Pages
services.AddTransient<ImportConfigSelectionPage>();
services.AddTransient<ImportHeaderSelectionPage>();
services.AddTransient<ImportColumnMappingPage>();  // Rinominato da ImportPage
services.AddTransient<ImportValidationPage>();  // ⭐ NUOVO
```

### AppShell.xaml.cs

```csharp
// Import wizard routes
Routing.RegisterRoute("importConfigSelection", typeof(ImportConfigSelectionPage));
Routing.RegisterRoute("importHeaderSelection", typeof(ImportHeaderSelectionPage));
Routing.RegisterRoute("importColumnMapping", typeof(ImportColumnMappingPage));
Routing.RegisterRoute("importValidation", typeof(ImportValidationPage));  // ⭐ NUOVO
```

### MainPage Navigation

```csharp
// Click bottone "Importa"
await Shell.Current.GoToAsync("importConfigSelection");
```

---

## 🧪 TESTING PLAN

### Test Case 1: Flusso Completo Nuova Config

**Steps**:
1. Click "Importa" → ImportConfigSelectionPage
2. Click "📂 Scegli File" → Seleziona `estratto_bcc.csv`
3. Click "➕ Nuova" → ImportHeaderSelectionPage
4. Vedi anteprima 20 righe, click riga 1 (header)
5. Click "Avanti" → ImportColumnMappingPage
6. Auto-mapping funziona → Data=0, Importo=2, Descrizione=3
7. Nome config: "Config_BCC_20260110"
8. Click "Salva e Continua" → ImportValidationPage
9. Vedi 95 righe verdi, 5 rosse
10. Click "Conferma Import" → MessageBox conferma
11. Import completato → Torna a MainPage
12. Verifica: 95 transazioni nel DB

**Expected**: ✅ Tutto funziona, configurazione salvata

### Test Case 2: Carica Config Esistente (SKIP)

**Steps**:
1. Click "Importa" → ImportConfigSelectionPage
2. Click "📂 Scegli File" → Seleziona `estratto_bcc_feb.csv` (stesso formato)
3. Seleziona config "Config_BCC_20260110"
4. Click "📂 Carica" → **SKIP diretto a ImportValidationPage** ⭐
5. Validazione auto-eseguita, vedi 102 righe verdi
6. Click "Conferma Import"
7. Import completato

**Expected**: ✅ Skip step 2 e 3, validazione immediata

### Test Case 3: Modifica Config

**Steps**:
1. Click "Importa" → ImportConfigSelectionPage
2. Click "📂 Scegli File" → Seleziona `estratto_bcc_nuovoformato.csv`
3. Seleziona config "Config_BCC_20260110"
4. Click "✏️ Modifica" → ImportColumnMappingPage
5. Vedi mapping pre-caricato (Data=0, Importo=2, Descrizione=3)
6. BCC ha cambiato formato: cambia Descrizione da 3 a 4
7. Lascia nome "Config_BCC_20260110" (sovrascrive)
8. Click "Salva Modifiche" → ImportValidationPage
9. Validazione OK
10. Import

**Expected**: ✅ Configurazione sovrascritta con nuovo mapping

### Test Case 4: Elimina Config

**Steps**:
1. Click "Importa" → ImportConfigSelectionPage
2. Seleziona config "Config_MiaBanca_20250105" (custom, non preset)
3. Click "🗑️ Elimina"
4. MessageBox conferma: "Sei sicuro?"
5. Click "Elimina" → Config eliminata
6. Lista si refresh, config sparita

**Expected**: ✅ Config eliminata, file JSON rimosso

### Test Case 5: Tentativo Elimina Preset

**Steps**:
1. Seleziona config "⭐ BCC - Banca di Credito Cooperativo" (preset)
2. Click "🗑️ Elimina"
3. MessageBox: "Non puoi eliminare preset"

**Expected**: ✅ Preset protetto da eliminazione

---

## 📊 COMPARAZIONE DESKTOP vs MOBILE

| Feature | Desktop | Mobile | Note Mobile |
|---------|---------|--------|-------------|
| **Wizard integrato** | ✅ Single dialog | ✅ Shell navigation | Più nativo per mobile |
| **Step 1: Config Selection** | ✅ | ✅ | Stessa logica |
| **Step 2: Header Selection** | ✅ | ✅ | Anteprima scrollabile |
| **Step 3: Column Mapping** | ✅ | ✅ | Auto-mapping identico |
| **Step 4: Validation** | ✅ DataGrid | ✅ CollectionView | Verde/rosso identico |
| **Bottone "Nuova Config"** | ✅ | ✅ | Stesso flow |
| **Bottone "Carica Selezionata"** | ✅ Skip step 2-3 | ✅ Skip step 2-3 | ⭐ Logica identica |
| **Bottone "Modifica"** | ✅ Va a step 3 | ✅ Va a step 3 | Stesso comportamento |
| **Bottone "Elimina"** | ✅ Conferma + delete | ✅ Conferma + delete | Stessa UX |
| **Preset Banche** | ✅ 2 preset | ✅ 9 preset | Mobile ha più preset |
| **Configurazioni JSON** | ✅ %APPDATA% | ✅ FileSystem.AppDataDirectory | Storage diverso, stessa struttura |
| **Auto-mapping** | ✅ Keywords IT/EN | ✅ Keywords IT/EN | Algoritmo identico |
| **Parsing Importi** | ✅ IT/USA format | ✅ IT/USA format | Funzione identica |
| **Validazione real-time** | ✅ Verde/rosso | ✅ Verde/rosso | Stessa UI |
| **Import finale** | ✅ DB SQLite | ✅ DB SQLite | Stesso database |

**Differenze chiave**:
- **Desktop**: Tutto in una Window con step panels
- **Mobile**: Shell navigation tra pages (più nativo)
- **Desktop**: DataGrid per liste
- **Mobile**: CollectionView (touch-optimized)
- **Desktop**: MessageBox dialogs
- **Mobile**: DisplayAlert (MAUI standard)

**Logica identica**: ✅
**UX adattata mobile**: ✅

---

## 🚧 IMPLEMENTAZIONE PHASED

### Phase 1: Validation Service (1 ora)
1. Creare `IImportValidationService.cs`
2. Implementare `ImportValidationService.cs`
3. Creare model `TransactionValidationRow.cs`
4. Unit test validation

### Phase 2: Validation ViewModel + Page (2 ore)
1. Creare `ImportValidationViewModel.cs`
2. Creare `ImportValidationPage.xaml`
3. Implementare logica skip (SkipToValidation flag)
4. Test navigation

### Phase 3: Modifiche Config Selection (1 ora)
1. Modificare `ImportConfigSelectionViewModel.cs`:
   - Aggiungere 4 command (Nuova, Carica, Modifica, Elimina)
   - Implementare logica skip
2. Modificare `ImportConfigSelectionPage.xaml`:
   - Aggiungere UI 4 bottoni
   - Migliorare lista configurazioni

### Phase 4: Rinominare Import → ColumnMapping (30 min)
1. Rinominare `ImportPage.xaml` → `ImportColumnMappingPage.xaml`
2. Rinominare `ImportViewModel.cs` → `ImportColumnMappingViewModel.cs`
3. Update route registration
4. Update references

### Phase 5: Registrazione Servizi (15 min)
1. Registrare `ImportValidationService` in DI
2. Registrare `ImportValidationViewModel`
3. Registrare `ImportValidationPage`
4. Registrare route "importValidation"

### Phase 6: Testing Integration (2 ore)
1. Test flow completo: Nuova Config
2. Test flow skip: Carica Selezionata
3. Test flow edit: Modifica
4. Test eliminazione config
5. Test validazione con errori
6. Test import finale

**TOTALE STIMA**: ~7 ore di sviluppo

---

## 🎯 PRIORITÀ IMPLEMENTAZIONE

### MUST HAVE (Fase 1)
- ✅ `ImportValidationService` + `TransactionValidationRow`
- ✅ `ImportValidationViewModel` + `ImportValidationPage`
- ✅ Bottoni "Nuova" e "Carica Selezionata" funzionanti
- ✅ Logica skip step funzionante

### SHOULD HAVE (Fase 2)
- ✅ Bottone "Modifica" funzionante
- ✅ Bottone "Elimina" funzionante
- ✅ Validazione colori verde/rosso perfetta
- ✅ Auto-mapping intelligente

### NICE TO HAVE (Fase 3)
- ⭐ Swipe gesture tra step (CarouselView)
- ⭐ Progress indicator step (1/4, 2/4, ecc.)
- ⭐ Animazioni transition tra step
- ⭐ Salvataggio automatico "Vuoi salvare config?"

---

## 📚 RIFERIMENTI

### Files Desktop da Studiare
- `C:\Users\rober\Documents\MoneyMind\Views\ImportDialog.xaml`
- `C:\Users\rober\Documents\MoneyMind\Views\ImportDialog.xaml.vb`
- `C:\Users\rober\Documents\MoneyMind\Services\ConfigurazioneImportazioneService.vb`

### Files Mobile da Creare/Modificare
- `ViewModels/ImportValidationViewModel.cs` ← **NUOVO**
- `Views/ImportValidationPage.xaml` ← **NUOVO**
- `Services/ImportExport/ImportValidationService.cs` ← **NUOVO**
- `ViewModels/ImportConfigSelectionViewModel.cs` ← **MODIFICARE**
- `Views/ImportConfigSelectionPage.xaml` ← **MODIFICARE**

### Documentazione Esistente
- `IMPORT_SYSTEM_COMPLETE.md` ← Sistema base già implementato
- Questo file ← Piano completo per completamento

---

## ✅ CHECKLIST FINALE

### Pre-implementazione
- [ ] Studiare a fondo ImportDialog.xaml.vb desktop (fatto ✅)
- [ ] Comprendere flow wizard desktop (fatto ✅)
- [ ] Comprendere logica skip step (fatto ✅)
- [ ] Pianificare architettura mobile (fatto ✅)

### Implementazione
- [ ] Creare `TransactionValidationRow` model
- [ ] Creare `IImportValidationService` interface
- [ ] Implementare `ImportValidationService`
- [ ] Creare `ImportValidationViewModel`
- [ ] Creare `ImportValidationPage.xaml`
- [ ] Modificare `ImportConfigSelectionViewModel` (4 bottoni)
- [ ] Modificare `ImportConfigSelectionPage.xaml` (UI bottoni)
- [ ] Rinominare `ImportPage` → `ImportColumnMappingPage`
- [ ] Registrare servizi in DI
- [ ] Registrare route in AppShell

### Testing
- [ ] Test flow "Nuova Configurazione"
- [ ] Test flow "Carica Selezionata" (skip funziona?)
- [ ] Test flow "Modifica"
- [ ] Test "Elimina" config custom
- [ ] Test "Elimina" preset (deve fallire)
- [ ] Test validazione con righe errate
- [ ] Test import finale nel DB
- [ ] Test navigazione back completa

### Documentazione
- [ ] Aggiornare `STATO_ARTE.md`
- [ ] Creare guide utente per import
- [ ] Screenshots flow mobile

---

## 🎉 CONCLUSIONE

Questo piano porta su mobile **TUTTA** la logica desktop di importazione, mantenendo:

✅ **Wizard step-by-step** identico
✅ **4 bottoni** (Nuova, Carica, Modifica, Elimina)
✅ **Logica skip** per "Carica Selezionata"
✅ **Validazione real-time** verde/rosso
✅ **Auto-mapping** intelligente
✅ **Parsing robusto** formati IT/USA
✅ **Configurazioni salvate** riutilizzabili
✅ **9 preset banche** italiane

**Differenze**: Solo UI adattata per touch/mobile (CollectionView vs DataGrid, DisplayAlert vs MessageBox).

**Logica business**: IDENTICA al desktop! ✅

---

**Pronto per implementazione!** 🚀
