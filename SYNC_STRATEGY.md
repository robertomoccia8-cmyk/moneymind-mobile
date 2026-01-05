# Sync Strategy - Desktop ↔ Mobile

> **📋 DOCUMENTO TECNICO COMPLETO**: Vedere **`WIFI_SYNC_IMPLEMENTATION.md`** per specifiche API, modelli e codice.

## 🎯 Obiettivo

Sincronizzare transazioni tra **Desktop WPF** e **Mobile MAUI** mantenendo dati **100% in locale** (NO cloud).

**Principio**: Dati sensibili solo in possesso dell'utente, mai su server terzi.

---

## 🏗️ ARCHITETTURA DATABASE (IDENTICA)

**Desktop E Mobile hanno la STESSA struttura database:**

```
📁 AppData/MoneyMind/
├── MoneyMind_Global.db          ← Conti correnti, Settings
├── MoneyMind_Conto_001.db       ← Transazioni Conto 1
├── MoneyMind_Conto_002.db       ← Transazioni Conto 2
└── ...
```

| Path | Piattaforma |
|------|-------------|
| `%APPDATA%\MoneyMind\` | Windows Desktop |
| `FileSystem.AppDataDirectory` | Android/iOS |

---

## ⚠️ DIFFERENZE CRITICHE DESKTOP vs MOBILE

### Campi Transazioni

| Campo | Desktop | Mobile | Sync |
|-------|---------|--------|------|
| ID | ✅ | ✅ | ❌ NO (PK locale) |
| Data | ✅ | ✅ | ✅ SÌ |
| Importo | ✅ | ✅ | ✅ SÌ |
| Descrizione | ✅ | ✅ | ✅ SÌ |
| Causale | ✅ | ✅ | ✅ SÌ |
| CreatedAt | ✅ | ✅ | ✅ SÌ |
| ModifiedAt | ✅ | ✅ | ✅ SÌ |
| **MacroCategoria** | ✅ | ❌ | ❌ IGNORARE |
| **Categoria** | ✅ | ❌ | ❌ IGNORARE |
| Note | ❌ | ✅ | ❌ Solo Mobile |
| AccountId | ❌ | ✅ | ❌ Solo Mobile |

### Regola Fondamentale

- **Desktop → Mobile**: SCARTARE MacroCategoria/Categoria
- **Mobile → Desktop**: Desktop ri-classifica automaticamente dopo import

---

## 🔄 FLUSSO SINCRONIZZAZIONE

### Principio: L'UTENTE SCEGLIE SEMPRE LA DIREZIONE

**NON esiste sync automatico bidirezionale!** L'utente deve scegliere:

```
┌─────────────────────────────────────────────────┐
│        SINCRONIZZAZIONE WiFi                    │
├─────────────────────────────────────────────────┤
│                                                 │
│  📥 DESKTOP → MOBILE                            │
│  Copia i dati dal computer al telefono          │
│                                                 │
│  📤 MOBILE → DESKTOP                            │
│  Copia i dati dal telefono al computer          │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Opzioni Disponibili

- **Tutti i conti**: Sincronizza tutti i conti correnti
- **Singolo conto**: Sincronizza solo un conto specifico

---

## 🛡️ SICUREZZA: BACKUP E AVVISI

### 1. Backup Pre-Sync (OBBLIGATORIO)

Prima di ogni sincronizzazione viene creato un backup automatico:

```
📁 backups/MoneyMind_Backup_20251123_143052/
├── MoneyMind_Global.db
├── MoneyMind_Conto_001.db
└── backup_info.json
```

### 2. Controllo Temporale

Il sistema confronta i dati PRIMA di procedere:

| Metrica | Sorgente | Destinazione | Azione |
|---------|----------|--------------|--------|
| Ultima transazione | 23/11 | 15/11 | ✅ OK |
| Ultima transazione | 15/11 | 23/11 | ⚠️ AVVISO |
| Num. transazioni | 120 | 150 | ⚠️ AVVISO |

### 3. Avviso Dati Più Recenti

Se la DESTINAZIONE ha dati più recenti della SORGENTE:

```
⚠️ ATTENZIONE

Il MOBILE contiene transazioni più recenti del DESKTOP!

Desktop: Ultima trans. 15/11/2025 (120 trans.)
Mobile:  Ultima trans. 23/11/2025 (150 trans.)

Se procedi, PERDERAI 30 transazioni sul Mobile!

[ANNULLA]  [PROCEDI CON BACKUP]
```

---

## 📡 ARCHITETTURA TECNICA

```
┌─────────────────┐                              ┌─────────────────┐
│     DESKTOP     │         WiFi / Hotspot       │     MOBILE      │
│   (VB.NET WPF)  │                              │   (.NET MAUI)   │
│                 │      HTTP REST API           │                 │
│   HTTP Client   │◄────────────────────────────►│   HTTP Server   │
│                 │      Port 8765               │   (Kestrel)     │
└─────────────────┘                              └─────────────────┘
```

**Desktop = CLIENT** | **Mobile = SERVER**

### Scenari Rete Supportati

1. **WiFi Casa/Ufficio**: Stessa rete
2. **Hotspot Android**: IP tipico `192.168.43.1`
3. **Hotspot iPhone**: IP tipico `172.20.10.1`
4. **Hotspot Windows**: IP tipico `192.168.137.1`

---

### 📱 Implementazione Mobile (Server)

**File**: `Services/Sync/WiFiSyncService.cs`

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MoneyMindApp.Services.Sync;

public interface IWiFiSyncService
{
    Task StartServerAsync();
    Task StopServerAsync();
    bool IsServerRunning { get; }
    string LocalIPAddress { get; }
}

public class WiFiSyncService : IWiFiSyncService
{
    private HttpListener? _listener;
    private readonly DatabaseService _databaseService;
    private readonly ILogger _logger;
    private const int PORT = 8765;

    public bool IsServerRunning => _listener?.IsListening ?? false;

    public string LocalIPAddress
    {
        get
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "N/A";
        }
    }

    public WiFiSyncService(DatabaseService databaseService, ILogger logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task StartServerAsync()
    {
        if (IsServerRunning)
            return;

        try
        {
            _listener = new HttpListener();

            // Bind a tutte le interfacce di rete (WiFi, Hotspot, etc)
            _listener.Prefixes.Add($"http://*:{PORT}/");
            _listener.Start();

            _logger.LogInfo($"WiFi Sync Server started on {LocalIPAddress}:{PORT}");

            // Gestisci richieste in background
            _ = Task.Run(async () => await HandleRequestsAsync());
        }
        catch (HttpListenerException ex)
        {
            _logger.LogError($"Failed to start server: {ex.Message}");
            throw new SyncException("Impossibile avviare server. Verifica permessi rete.");
        }
    }

    public async Task StopServerAsync()
    {
        if (_listener != null)
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
            _logger.LogInfo("WiFi Sync Server stopped");
        }
    }

    private async Task HandleRequestsAsync()
    {
        while (_listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                await ProcessRequestAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Request handling error: {ex.Message}");
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.ContentType = "application/json";

        try
        {
            switch (request.Url?.AbsolutePath)
            {
                case "/ping":
                    await SendJsonResponse(response, new { status = "ok", app = "MoneyMind Mobile" });
                    break;

                case "/transactions":
                    await HandleTransactionsEndpoint(request, response);
                    break;

                case "/sync":
                    await HandleSyncEndpoint(request, response);
                    break;

                default:
                    response.StatusCode = 404;
                    await SendJsonResponse(response, new { error = "Endpoint not found" });
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message });
        }
    }

    private async Task HandleTransactionsEndpoint(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod == "GET")
        {
            // Desktop richiede transazioni da Mobile
            var transactions = await _databaseService.GetTransactionsAsync();

            var syncData = transactions.Select(t => new
            {
                data = t.Data.ToString("yyyy-MM-dd"),
                descrizione = t.Descrizione,
                causale = t.Causale ?? "",
                importo = t.Importo
            }).ToList();

            await SendJsonResponse(response, new
            {
                success = true,
                count = syncData.Count,
                transactions = syncData
            });
        }
        else if (request.HttpMethod == "POST")
        {
            // Desktop invia transazioni a Mobile
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<SyncRequest>(body);

            var imported = await ImportTransactionsAsync(data.Transactions);

            await SendJsonResponse(response, new
            {
                success = true,
                imported = imported,
                skipped = data.Transactions.Count - imported
            });
        }
    }

    private async Task HandleSyncEndpoint(HttpListenerRequest request, HttpListenerResponse response)
    {
        // Sync bidirezionale: scambio transazioni
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var desktopData = JsonSerializer.Deserialize<SyncRequest>(body);

        // 1. Importa transazioni desktop su mobile
        var imported = await ImportTransactionsAsync(desktopData.Transactions);

        // 2. Restituisci transazioni mobile (che desktop non ha)
        var mobileTransactions = await _databaseService.GetTransactionsAsync();
        var newForDesktop = mobileTransactions
            .Where(m => !desktopData.Transactions.Any(d =>
                d.Data == m.Data.ToString("yyyy-MM-dd") &&
                Math.Abs(d.Importo - m.Importo) < 0.01m))
            .Select(t => new
            {
                data = t.Data.ToString("yyyy-MM-dd"),
                descrizione = t.Descrizione,
                causale = t.Causale ?? "",
                importo = t.Importo
            })
            .ToList();

        await SendJsonResponse(response, new
        {
            success = true,
            imported_to_mobile = imported,
            new_for_desktop = newForDesktop
        });
    }

    private async Task<int> ImportTransactionsAsync(List<SyncTransaction> transactions)
    {
        var imported = 0;

        foreach (var t in transactions)
        {
            // Check duplicato (stessa data + importo ± 0.01€)
            var exists = await _databaseService.ExistsAsync(
                DateTime.Parse(t.Data),
                t.Importo,
                t.Descrizione);

            if (!exists)
            {
                await _databaseService.InsertAsync(new Transaction
                {
                    Data = DateTime.Parse(t.Data),
                    Descrizione = t.Descrizione,
                    Causale = t.Causale,
                    Importo = t.Importo,
                    DataInserimento = DateTime.Now
                });
                imported++;
            }
        }

        return imported;
    }

    private async Task SendJsonResponse(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }
}

public class SyncRequest
{
    public List<SyncTransaction> Transactions { get; set; } = new();
}

public class SyncTransaction
{
    public string Data { get; set; } = "";
    public string Descrizione { get; set; } = "";
    public string Causale { get; set; } = "";
    public decimal Importo { get; set; }
}

public class SyncException : Exception
{
    public SyncException(string message) : base(message) { }
}
```

---

### 🖥️ Implementazione Desktop (Client)

**File Desktop**: `Services/WiFiSyncClient.vb`

```vb
Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json

Public Class WiFiSyncClient
    Private _httpClient As HttpClient

    Public Sub New()
        _httpClient = New HttpClient()
        _httpClient.Timeout = TimeSpan.FromSeconds(10)
    End Sub

    ''' <summary>
    ''' Ping mobile per verificare connessione
    ''' </summary>
    Public Async Function PingAsync(mobileIP As String) As Task(Of Boolean)
        Try
            Dim response = Await _httpClient.GetAsync($"http://{mobileIP}:8765/ping")
            Return response.IsSuccessStatusCode
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Scarica transazioni da mobile
    ''' </summary>
    Public Async Function DownloadTransactionsAsync(mobileIP As String) As Task(Of List(Of Transazione))
        Dim url = $"http://{mobileIP}:8765/transactions"
        Dim response = Await _httpClient.GetAsync(url)
        response.EnsureSuccessStatusCode()

        Dim json = Await response.Content.ReadAsStringAsync()
        Dim result = JsonConvert.DeserializeObject(Of SyncResponse)(json)

        Return result.Transactions.Select(Function(t) New Transazione With {
            .Data = Date.Parse(t.Data),
            .Descrizione = t.Descrizione,
            .Causale = t.Causale,
            .Importo = t.Importo
        }).ToList()
    End Function

    ''' <summary>
    ''' Carica transazioni su mobile
    ''' </summary>
    Public Async Function UploadTransactionsAsync(mobileIP As String, transazioni As List(Of Transazione)) As Task(Of Integer)
        Dim syncData = New With {
            .transactions = transazioni.Select(Function(t) New With {
                .data = t.Data.ToString("yyyy-MM-dd"),
                .descrizione = t.Descrizione,
                .causale = If(t.Causale, ""),
                .importo = t.Importo
            }).ToList()
        }

        Dim json = JsonConvert.SerializeObject(syncData)
        Dim content = New StringContent(json, Encoding.UTF8, "application/json")

        Dim response = Await _httpClient.PostAsync($"http://{mobileIP}:8765/transactions", content)
        response.EnsureSuccessStatusCode()

        Dim resultJson = Await response.Content.ReadAsStringAsync()
        Dim result = JsonConvert.DeserializeObject(Of ImportResult)(resultJson)

        Return result.Imported
    End Function

    ''' <summary>
    ''' Sync bidirezionale completo
    ''' </summary>
    Public Async Function SyncBidirectionalAsync(mobileIP As String) As Task(Of SyncResult)
        ' 1. Prendi tutte le transazioni desktop
        Dim desktopTransazioni = DatabaseService.GetAllTransazioni()

        ' 2. Invia a mobile + ricevi nuove da mobile
        Dim syncData = New With {
            .transactions = desktopTransazioni.Select(Function(t) New With {
                .data = t.Data.ToString("yyyy-MM-dd"),
                .descrizione = t.Descrizione,
                .causale = If(t.Causale, ""),
                .importo = t.Importo
            }).ToList()
        }

        Dim json = JsonConvert.SerializeObject(syncData)
        Dim content = New StringContent(json, Encoding.UTF8, "application/json")

        Dim response = Await _httpClient.PostAsync($"http://{mobileIP}:8765/sync", content)
        response.EnsureSuccessStatusCode()

        Dim resultJson = Await response.Content.ReadAsStringAsync()
        Dim result = JsonConvert.DeserializeObject(Of SyncResponse)(resultJson)

        ' 3. Importa transazioni nuove da mobile
        Dim imported = 0
        For Each t In result.NewForDesktop
            If Not DatabaseService.EsisteTransazione(Date.Parse(t.Data), t.Importo, t.Descrizione) Then
                DatabaseService.InserisciTransazione(New Transazione With {
                    .Data = Date.Parse(t.Data),
                    .Descrizione = t.Descrizione,
                    .Causale = t.Causale,
                    .Importo = t.Importo
                })
                imported += 1
            End If
        Next

        Return New SyncResult With {
            .ImportedToMobile = result.ImportedToMobile,
            .ImportedToDesktop = imported,
            .Success = True
        }
    End Function

    Private Class SyncResponse
        Public Property Success As Boolean
        Public Property Transactions As List(Of TransactionData)
        Public Property ImportedToMobile As Integer
        Public Property NewForDesktop As List(Of TransactionData)
    End Class

    Private Class TransactionData
        Public Property Data As String
        Public Property Descrizione As String
        Public Property Causale As String
        Public Property Importo As Decimal
    End Class

    Private Class ImportResult
        Public Property Success As Boolean
        Public Property Imported As Integer
        Public Property Skipped As Integer
    End Class
End Class

Public Class SyncResult
    Public Property Success As Boolean
    Public Property ImportedToMobile As Integer
    Public Property ImportedToDesktop As Integer
End Class
```

---

### 📱 UI Mobile - Sync Page

**File**: `Views/WiFiSyncPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MoneyMindApp.Views.WiFiSyncPage"
             Title="Sincronizzazione WiFi">

    <ScrollView Padding="20">
        <VerticalStackLayout Spacing="20">

            <!-- Status Server -->
            <Frame BorderColor="LightGray" HasShadow="True" Padding="16" CornerRadius="12">
                <VerticalStackLayout Spacing="12">
                    <Label Text="📡 Server WiFi Sync" FontSize="18" FontAttributes="Bold" />

                    <HorizontalStackLayout Spacing="8">
                        <Label Text="Status:" FontSize="14" />
                        <Label Text="{Binding ServerStatus}" FontSize="14" FontAttributes="Bold"
                               TextColor="{Binding ServerStatusColor}" />
                    </HorizontalStackLayout>

                    <HorizontalStackLayout Spacing="8" IsVisible="{Binding IsServerRunning}">
                        <Label Text="IP Address:" FontSize="14" />
                        <Label Text="{Binding LocalIPAddress}" FontSize="14" FontAttributes="Bold" />
                        <Button Text="📋" WidthRequest="40" HeightRequest="40"
                                Command="{Binding CopyIPCommand}"
                                ToolTipProperties.Text="Copia IP" />
                    </HorizontalStackLayout>

                    <Button Text="{Binding ServerButtonText}"
                            Command="{Binding ToggleServerCommand}"
                            BackgroundColor="{Binding ServerButtonColor}" />
                </VerticalStackLayout>
            </Frame>

            <!-- Istruzioni -->
            <Frame BorderColor="LightBlue" BackgroundColor="#E3F2FD" Padding="16" CornerRadius="12">
                <VerticalStackLayout Spacing="8">
                    <Label Text="ℹ️ Come Sincronizzare" FontSize="16" FontAttributes="Bold" />
                    <Label FontSize="14" LineHeight="1.4">
                        <Label.FormattedText>
                            <FormattedString>
                                <Span Text="1. " FontAttributes="Bold" />
                                <Span Text="Avvia il server su questo dispositivo (tasto sopra)&#10;" />
                                <Span Text="2. " FontAttributes="Bold" />
                                <Span Text="Connetti il computer alla stessa rete WiFi (o hotspot di questo telefono)&#10;" />
                                <Span Text="3. " FontAttributes="Bold" />
                                <Span Text="Sull'app desktop, clicca 'Sincronizza' e inserisci l'IP: " />
                                <Span Text="{Binding LocalIPAddress}" FontAttributes="Bold" TextColor="Blue" />
                            </FormattedString>
                        </Label.FormattedText>
                    </Label>
                </VerticalStackLayout>
            </Frame>

            <!-- QR Code per IP (Opzionale) -->
            <Frame BorderColor="LightGray" Padding="16" CornerRadius="12"
                   IsVisible="{Binding IsServerRunning}">
                <VerticalStackLayout Spacing="12" HorizontalOptions="Center">
                    <Label Text="📱 Scansiona QR Code" FontSize="16" FontAttributes="Bold"
                           HorizontalOptions="Center" />
                    <Image Source="{Binding QRCodeImage}" WidthRequest="200" HeightRequest="200" />
                    <Label Text="Scansiona con l'app desktop per connessione rapida"
                           FontSize="12" TextColor="Gray" HorizontalTextAlignment="Center" />
                </VerticalStackLayout>
            </Frame>

            <!-- Statistiche Sync -->
            <Frame BorderColor="LightGray" Padding="16" CornerRadius="12"
                   IsVisible="{Binding HasSyncHistory}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="📊 Ultima Sincronizzazione" FontSize="16" FontAttributes="Bold" />
                    <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto,Auto" RowSpacing="4">
                        <Label Grid.Row="0" Grid.Column="0" Text="Data:" />
                        <Label Grid.Row="0" Grid.Column="1" Text="{Binding LastSyncDate}" FontAttributes="Bold" />

                        <Label Grid.Row="1" Grid.Column="0" Text="Transazioni inviate:" />
                        <Label Grid.Row="1" Grid.Column="1" Text="{Binding LastSyncSent}" FontAttributes="Bold" />

                        <Label Grid.Row="2" Grid.Column="0" Text="Transazioni ricevute:" />
                        <Label Grid.Row="2" Grid.Column="1" Text="{Binding LastSyncReceived}" FontAttributes="Bold" />
                    </Grid>
                </VerticalStackLayout>
            </Frame>

            <!-- Troubleshooting -->
            <Expander>
                <Expander.Header>
                    <Label Text="🔧 Risoluzione Problemi" FontSize="14" TextColor="Gray" />
                </Expander.Header>
                <VerticalStackLayout Spacing="8" Padding="10">
                    <Label Text="• Assicurati che entrambi i dispositivi siano sulla stessa rete WiFi"
                           FontSize="12" />
                    <Label Text="• Se usi hotspot mobile, connetti il computer all'hotspot di questo telefono"
                           FontSize="12" />
                    <Label Text="• Verifica che il firewall del computer permetta connessioni alla porta 8765"
                           FontSize="12" />
                    <Label Text="• Prova a disabilitare/riabilitare il server"
                           FontSize="12" />
                    <Button Text="Test Connessione" Command="{Binding TestConnectionCommand}"
                            BackgroundColor="Orange" Margin="0,10,0,0" />
                </VerticalStackLayout>
            </Expander>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

---

### 🖥️ UI Desktop - Sync Dialog

**File Desktop**: `Views/WiFiSyncDialog.xaml` (WPF)

```xml
<Window x:Class="WiFiSyncDialog"
        Title="Sincronizzazione WiFi" Height="500" Width="600"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Margin="0,0,0,20">
            <TextBlock Text="📡 Sincronizzazione WiFi con Mobile" FontSize="18" FontWeight="Bold"/>
            <TextBlock Text="Assicurati che il telefono sia sulla stessa rete WiFi o che il computer sia connesso all'hotspot del telefono"
                       TextWrapping="Wrap" Foreground="Gray" Margin="0,5,0,0"/>
        </StackPanel>

        <!-- Input IP -->
        <StackPanel Grid.Row="1" Margin="0,0,0,20">
            <Label Content="Indirizzo IP del telefono:" FontWeight="Bold"/>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBox x:Name="TxtIPAddress" Grid.Column="0" Padding="8"
                         Text="192.168.43.1" FontSize="14"/>
                <Button Grid.Column="1" Content="📷 QR" Width="50" Margin="5,0,0,0"
                        Click="BtnScanQR_Click" ToolTip="Scansiona QR Code dal telefono"/>
                <Button Grid.Column="2" Content="Ping" Width="60" Margin="5,0,0,0"
                        Click="BtnPing_Click" Background="Orange"/>
            </Grid>
            <TextBlock x:Name="TxtPingResult" Margin="0,5,0,0" Foreground="Green"/>
        </StackPanel>

        <!-- Progress -->
        <Border Grid.Row="2" BorderBrush="LightGray" BorderThickness="1" CornerRadius="8" Padding="15">
            <ScrollViewer>
                <StackPanel>
                    <TextBlock Text="📋 Log Sincronizzazione:" FontWeight="Bold" Margin="0,0,0,10"/>
                    <TextBox x:Name="TxtLog" IsReadOnly="True" TextWrapping="Wrap"
                             VerticalScrollBarVisibility="Auto" BorderThickness="0"
                             FontFamily="Consolas" FontSize="12" Background="Transparent"/>

                    <ProgressBar x:Name="ProgressBar" Height="6" Margin="0,10,0,0"
                                 IsIndeterminate="False" Visibility="Collapsed"/>
                </StackPanel>
            </ScrollViewer>
        </Border>

        <!-- Buttons -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="Scarica da Mobile" Width="140" Height="35" Margin="0,0,10,0"
                    Click="BtnDownload_Click" Background="#2196F3" Foreground="White"/>
            <Button Content="Carica su Mobile" Width="140" Height="35" Margin="0,0,10,0"
                    Click="BtnUpload_Click" Background="#4CAF50" Foreground="White"/>
            <Button Content="⇄ Sincronizza" Width="140" Height="35" Margin="0,0,10,0"
                    Click="BtnSync_Click" Background="#9C27B0" Foreground="White"
                    FontWeight="Bold"/>
            <Button Content="Chiudi" Width="80" Height="35"
                    Click="BtnClose_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

**Code-Behind Desktop**:

```vb
Imports System.Windows

Public Class WiFiSyncDialog
    Private _syncClient As WiFiSyncClient

    Public Sub New()
        InitializeComponent()
        _syncClient = New WiFiSyncClient()

        ' Rileva IP automaticamente (se sulla stessa rete)
        TryAutoDetectIP()
    End Sub

    Private Async Sub BtnPing_Click(sender As Object, e As RoutedEventArgs)
        Dim ip = TxtIPAddress.Text.Trim()
        TxtPingResult.Text = "Connessione in corso..."

        Dim success = Await _syncClient.PingAsync(ip)

        If success Then
            TxtPingResult.Text = "✅ Connesso! Server mobile raggiungibile."
            TxtPingResult.Foreground = Brushes.Green
        Else
            TxtPingResult.Text = "❌ Impossibile connettersi. Verifica IP e rete."
            TxtPingResult.Foreground = Brushes.Red
        End If
    End Sub

    Private Async Sub BtnDownload_Click(sender As Object, e As RoutedEventArgs)
        Try
            ShowProgress("Scaricamento transazioni da mobile...")

            Dim ip = TxtIPAddress.Text.Trim()
            Dim transazioni = Await _syncClient.DownloadTransactionsAsync(ip)

            ' Importa nel database desktop
            Dim imported = 0
            For Each t In transazioni
                If Not DatabaseService.EsisteTransazione(t.Data, t.Importo, t.Descrizione) Then
                    DatabaseService.InserisciTransazione(t)
                    imported += 1
                End If
            Next

            HideProgress()
            LogMessage($"✅ Scaricate {transazioni.Count} transazioni, {imported} importate.")
            MessageBox.Show($"Sincronizzazione completata!{vbCrLf}{imported} nuove transazioni importate.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            HideProgress()
            LogMessage($"❌ Errore: {ex.Message}")
            MessageBox.Show($"Errore durante scaricamento:{vbCrLf}{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Async Sub BtnUpload_Click(sender As Object, e As RoutedEventArgs)
        Try
            ShowProgress("Caricamento transazioni su mobile...")

            Dim ip = TxtIPAddress.Text.Trim()
            Dim transazioni = DatabaseService.GetAllTransazioni()
            Dim imported = Await _syncClient.UploadTransactionsAsync(ip, transazioni)

            HideProgress()
            LogMessage($"✅ Caricate {transazioni.Count} transazioni, {imported} importate su mobile.")
            MessageBox.Show($"Sincronizzazione completata!{vbCrLf}{imported} transazioni caricate su mobile.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            HideProgress()
            LogMessage($"❌ Errore: {ex.Message}")
            MessageBox.Show($"Errore durante caricamento:{vbCrLf}{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Async Sub BtnSync_Click(sender As Object, e As RoutedEventArgs)
        Try
            ShowProgress("Sincronizzazione bidirezionale in corso...")

            Dim ip = TxtIPAddress.Text.Trim()
            Dim result = Await _syncClient.SyncBidirectionalAsync(ip)

            HideProgress()
            LogMessage($"✅ Sync completata! Desktop→Mobile: {result.ImportedToMobile}, Mobile→Desktop: {result.ImportedToDesktop}")
            MessageBox.Show($"Sincronizzazione bidirezionale completata!{vbCrLf}{vbCrLf}Desktop → Mobile: {result.ImportedToMobile} transazioni{vbCrLf}Mobile → Desktop: {result.ImportedToDesktop} transazioni", "Successo", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            HideProgress()
            LogMessage($"❌ Errore: {ex.Message}")
            MessageBox.Show($"Errore durante sincronizzazione:{vbCrLf}{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ShowProgress(message As String)
        LogMessage(message)
        ProgressBar.Visibility = Visibility.Visible
        ProgressBar.IsIndeterminate = True
    End Sub

    Private Sub HideProgress()
        ProgressBar.Visibility = Visibility.Collapsed
        ProgressBar.IsIndeterminate = False
    End Sub

    Private Sub LogMessage(message As String)
        TxtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{vbCrLf}")
        TxtLog.ScrollToEnd()
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub TryAutoDetectIP()
        ' Prova IP comuni per hotspot mobile
        Dim commonIPs = {"192.168.43.1", "192.168.137.1", "172.20.10.1"}

        For Each ip In commonIPs
            ' Prova ping veloce in background
            Task.Run(Async Function()
                If Await _syncClient.PingAsync(ip) Then
                    Dispatcher.Invoke(Sub()
                        TxtIPAddress.Text = ip
                        TxtPingResult.Text = $"✅ Auto-rilevato: {ip}"
                        TxtPingResult.Foreground = Brushes.Green
                    End Sub)
                    Exit For
                End If
            End Function)
        Next
    End Sub
End Class
```

---

## 🎉 Risposta: HOTSPOT Mobile → PC FUNZIONA!

**Scenario**:
1. Cellulare attiva **hotspot WiFi**
2. Computer si connette all'hotspot
3. App mobile avvia server (IP: `192.168.43.1` tipicamente)
4. App desktop si connette a quell'IP
5. Sync bidirezionale funziona perfettamente!

**IP Comuni Hotspot**:
- Android: `192.168.43.1` (default)
- iPhone: `172.20.10.1` (default)
- Windows Hotspot: `192.168.137.1`

---

## 📦 Metodo 2: File Export/Import (Backup)

### Desktop Export

**Button** in MainWindow: "Esporta per Mobile"

```vb
Private Sub BtnEsportaMobile_Click()
    Dim dialog = New SaveFileDialog With {
        .Filter = "MoneyMind Sync|*.mmsync|JSON|*.json",
        .FileName = $"MoneyMind_Export_{DateTime.Now:yyyyMMdd}.mmsync"
    }

    If dialog.ShowDialog() = True Then
        Dim transazioni = DatabaseService.GetAllTransazioni()

        Dim syncData = New With {
            .version = "1.0",
            .exported_at = DateTime.Now,
            .account_id = ContoCorrente.IdAttivo,
            .transactions = transazioni.Select(Function(t) New With {
                .data = t.Data.ToString("yyyy-MM-dd"),
                .descrizione = t.Descrizione,
                .causale = If(t.Causale, ""),
                .importo = t.Importo
            }).ToList()
        }

        Dim json = JsonConvert.SerializeObject(syncData, Formatting.Indented)

        ' Compressione opzionale
        If dialog.FileName.EndsWith(".mmsync") Then
            CompressAndSave(json, dialog.FileName)
        Else
            File.WriteAllText(dialog.FileName, json)
        End If

        MessageBox.Show($"Esportate {transazioni.Count} transazioni in {dialog.FileName}", "Successo")
    End If
End Sub

Private Sub CompressAndSave(json As String, filePath As String)
    Using fs = New FileStream(filePath, FileMode.Create)
        Using gz = New GZipStream(fs, CompressionMode.Compress)
            Dim bytes = Encoding.UTF8.GetBytes(json)
            gz.Write(bytes, 0, bytes.Length)
        End Using
    End Using
End Sub
```

### Mobile Import

**ImportPage.xaml** → Button "Importa File Sync"

```csharp
[RelayCommand]
private async Task ImportSyncFileAsync()
{
    try
    {
        var result = await FilePicker.PickAsync(new PickOptions
        {
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/octet-stream", "application/json" } },
                { DevicePlatform.iOS, new[] { "public.data", "public.json" } }
            })
        });

        if (result == null)
            return;

        using var stream = await result.OpenReadAsync();
        string json;

        // Decomprimi se .mmsync
        if (result.FileName.EndsWith(".mmsync"))
        {
            using var gz = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gz);
            json = await reader.ReadToEndAsync();
        }
        else
        {
            using var reader = new StreamReader(stream);
            json = await reader.ReadToEndAsync();
        }

        var syncData = JsonSerializer.Deserialize<SyncData>(json);

        // Importa transazioni
        var imported = 0;
        foreach (var t in syncData.Transactions)
        {
            if (!await _databaseService.ExistsAsync(DateTime.Parse(t.Data), t.Importo, t.Descrizione))
            {
                await _databaseService.InsertAsync(new Transaction
                {
                    Data = DateTime.Parse(t.Data),
                    Descrizione = t.Descrizione,
                    Causale = t.Causale,
                    Importo = t.Importo
                });
                imported++;
            }
        }

        await Shell.Current.DisplayAlert("Importazione Completata",
            $"{imported} nuove transazioni importate su {syncData.Transactions.Count} totali.",
            "OK");
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}
```

---

## 🎯 Raccomandazione Finale

### Usa Entrambi i Metodi

1. **WiFi Sync** (80% dei casi)
   - Uso quotidiano casa/ufficio
   - **Hotspot mobile quando fuori** ✅
   - Sync rapido bidirezionale

2. **File Export/Import** (20% dei casi)
   - Backup completo
   - Emergenza (se WiFi non disponibile)
   - Trasferimento bulk iniziale

---

## 🔄 LE 3 MODALITÀ DI SYNC

| Modalità | Comportamento | Quando Usarla |
|----------|---------------|---------------|
| **SOSTITUISCI** | Cancella tutto sulla destinazione, copia dalla sorgente | Primo sync, reset completo |
| **UNISCI** | Mantiene esistenti, aggiunge solo non-duplicati | Uso quotidiano |
| **SOLO NUOVE** | Copia solo transazioni più recenti dell'ultima | Aggiornamento veloce |

### Criterio Duplicato (per UNISCI)

```
DUPLICATO = Data identica + Descrizione identica (case-insensitive)
```

---

## ⚠️ AVVISO CLASSIFICAZIONI

**Mobile → Desktop + SOSTITUISCI** = Perdita classificazioni!

Il Desktop ha MacroCategoria/Categoria, il Mobile NO. Se l'utente fa SOSTITUISCI dal Mobile al Desktop:
- Tutte le classificazioni verranno PERSE
- Verrà mostrato avviso con numero transazioni classificate
- Backup automatico permette ripristino

---

## 📋 CHECKLIST IMPLEMENTAZIONE

> **DETTAGLI COMPLETI**: Vedere `WIFI_SYNC_IMPLEMENTATION.md` (v3)

### FASE 1: Modelli e Backup (Mobile)
- [ ] `Models/Sync/SyncModels.cs` - Modelli + enum SyncMode/SyncDirection
- [ ] `Services/Backup/BackupService.cs` - Backup pre-sync

### FASE 2: WiFiSyncService (Mobile)
- [ ] Aggiornare endpoints con supporto 3 modalità
- [ ] Logica SOSTITUISCI, UNISCI, SOLO NUOVE
- [ ] Rilevamento duplicati (Data + Descrizione)

### FASE 3: UI Mobile
- [ ] `Views/WiFiSyncPage.xaml` - Server sync
- [ ] `ViewModels/WiFiSyncViewModel.cs`

### FASE 4: Desktop VB.NET
- [ ] `Services/WiFiSyncClient.vb` - Client HTTP
- [ ] `Views/WiFiSyncDialog.xaml` - Dialog con:
  - [ ] Selezione DIREZIONE
  - [ ] Selezione MODALITÀ (3 opzioni)
  - [ ] Selezione CONTI
  - [ ] Avviso classificazioni
  - [ ] Report sintetico

### FASE 5: Testing
- [ ] Test 3 modalità
- [ ] Test avviso classificazioni
- [ ] Test ripristino backup

---

## 🚨 PUNTI CRITICI

1. **3 MODALITÀ**: SOSTITUISCI, UNISCI, SOLO NUOVE
2. **DIREZIONE ESPLICITA**: Desktop→Mobile o Mobile→Desktop
3. **BACKUP OBBLIGATORIO**: Sempre prima di procedere (ripristinabile)
4. **CRITERIO DUPLICATO**: Data + Descrizione identiche
5. **AVVISO CLASSIFICAZIONI**: Mobile→Desktop + SOSTITUISCI richiede avviso
6. **REPORT SINTETICO**: Mostrare transazioni processate/duplicate/aggiunte
7. **IGNORA CLASSIFICAZIONI**: MacroCategoria/Categoria non trasferite

---

**Documentazione Tecnica Completa**: `WIFI_SYNC_IMPLEMENTATION.md` (v3)
