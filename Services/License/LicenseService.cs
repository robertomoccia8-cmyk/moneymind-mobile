using MoneyMindApp.Models;
using Newtonsoft.Json;
using System.Text;

namespace MoneyMindApp.Services.License;

/// <summary>
/// Implementazione servizio Beta License con Google Sheets backend
/// Porta da Desktop: Services/BetaLicenseManager.vb
/// </summary>
public class LicenseService : ILicenseService
{
    private const string GOOGLE_SHEETS_API_URL = "https://script.google.com/macros/s/AKfycbxQ9IBgSBki5MtxrhNnR8sGHfLawjscWItXMm99OtY02Ehrs7hfqQY7fZNA2PepxfcB/exec";
    private const string CACHE_KEY_LICENSE = "license_data";
    private const string CACHE_KEY_LAST_CHECK = "license_last_check";

    private readonly HttpClient _httpClient;

    public LicenseService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MoneyMind-Mobile");
    }

    public async Task<(bool Success, string Message, LicenseData? License)> ActivateLicenseAsync(string licenseKey)
    {
        try
        {
            var fingerprint = GetDeviceFingerprint();
            var deviceName = $"{DeviceInfo.Model} ({DeviceInfo.Platform})";

            // Stessa API del desktop: GET con query string
            var url = $"{GOOGLE_SHEETS_API_URL}?action=verify&key={Uri.EscapeDataString(licenseKey)}&fingerprint={Uri.EscapeDataString(fingerprint)}&deviceName={Uri.EscapeDataString(deviceName)}";

            var response = await _httpClient.GetStringAsync(url);
            var json = Newtonsoft.Json.Linq.JObject.Parse(response);

            if (json["success"] != null && (bool)json["success"] == true)
            {
                // Parse date con fallback
                var activatedAt = DateTime.Now;
                var expiresAt = DateTime.Now.AddMonths(6);

                if (json["activatedAt"] != null && !string.IsNullOrEmpty(json["activatedAt"]?.ToString()))
                    DateTime.TryParse(json["activatedAt"]?.ToString(), out activatedAt);

                if (json["expiresAt"] != null && !string.IsNullOrEmpty(json["expiresAt"]?.ToString()))
                    DateTime.TryParse(json["expiresAt"]?.ToString(), out expiresAt);

                // Parse campi aggiuntivi
                var subscriptionType = json["subscriptionType"]?.ToString() ?? "Base";
                var maxDevices = json["maxDevices"] != null ? (int?)json["maxDevices"] ?? 1 : 1;
                var activeDevices = json["activeDevices"] != null ? (int?)json["activeDevices"] ?? 1 : 1;

                var license = new LicenseData
                {
                    LicenseKey = licenseKey,
                    Email = json["email"]?.ToString() ?? "",
                    DeviceName = json["deviceName"]?.ToString() ?? deviceName,
                    DeviceFingerprint = fingerprint,
                    ActivatedAt = activatedAt,
                    ExpiresAt = expiresAt,
                    LastChecked = DateTime.Now,
                    Subscription = subscriptionType,
                    MaxDevices = maxDevices,
                    ActiveDevices = activeDevices
                };

                CacheLicense(license);
                return (true, "✅ Licenza attivata con successo!", license);
            }

            var errorMsg = json["error"]?.ToString() ?? "Errore sconosciuto";
            return (false, $"❌ {errorMsg}", null);
        }
        catch (HttpRequestException)
        {
            return (false, "❌ Impossibile connettersi al server. Verifica la connessione internet.", null);
        }
        catch (Exception ex)
        {
            return (false, $"❌ Errore: {ex.Message}", null);
        }
    }

    public async Task<(bool IsValid, string Message, LicenseData? License)> CheckLicenseStatusAsync()
    {
        var cached = GetCachedLicense();

        // Nessuna licenza cached
        if (cached == null)
            return (false, "⚠️ Nessuna licenza attiva", null);

        // SEMPRE tenta check remoto per aggiornare i dati
        try
        {
            return await PerformRemoteCheckAsync(cached);
        }
        catch (Exception)
        {
            // Errore di connessione - usa cache se in grace period
            if (IsInGracePeriod())
            {
                // Entro grace period, check offline
                if (cached.IsExpired)
                    return (false, "⏰ Licenza scaduta", cached);

                if (cached.IsRevoked)
                    return (false, "❌ Licenza revocata", cached);

                if (!cached.IsActive)
                    return (false, "⚠️ Licenza inattiva", cached);

                return (true, "✅ Licenza valida (offline)", cached);
            }

            // Oltre grace period senza connessione
            return (false, "❌ Impossibile verificare licenza. Connettiti a internet.", null);
        }
    }

    private async Task<(bool IsValid, string Message, LicenseData? License)> PerformRemoteCheckAsync(LicenseData cached)
    {
        try
        {
            var fingerprint = GetDeviceFingerprint();
            var deviceName = $"{DeviceInfo.Model} ({DeviceInfo.Platform})";

            // Stessa API del desktop: GET con query string
            var url = $"{GOOGLE_SHEETS_API_URL}?action=checkStatus&key={Uri.EscapeDataString(cached.LicenseKey)}&fingerprint={Uri.EscapeDataString(fingerprint)}&deviceName={Uri.EscapeDataString(deviceName)}";

            using var cts = new CancellationTokenSource(5000); // 5 secondi timeout
            var response = await _httpClient.GetStringAsync(url, cts.Token);
            var json = Newtonsoft.Json.Linq.JObject.Parse(response);

            if (json["success"] != null && (bool)json["success"] == true)
            {
                var status = json["status"]?.ToString();
                var revokeReason = json["revokeReason"]?.ToString();

                // Check se revocata/scaduta/sospesa
                if (status == "revoked" || status == "expired" || status == "suspended")
                {
                    cached.IsRevoked = true;
                    CacheLicense(cached); // Salva stato revocato
                    return (false, $"❌ Licenza {status}: {revokeReason ?? "Accesso terminato"}", cached);
                }

                // Status ACTIVE - aggiorna campi
                cached.LastChecked = DateTime.Now;
                cached.Subscription = json["subscriptionType"]?.ToString() ?? "Base";
                cached.MaxDevices = json["maxDevices"] != null ? (int?)json["maxDevices"] ?? 1 : 1;
                cached.ActiveDevices = json["activeDevices"] != null ? (int?)json["activeDevices"] ?? 1 : 1;

                if (json["expiresAt"] != null && !string.IsNullOrEmpty(json["expiresAt"]?.ToString()))
                {
                    if (DateTime.TryParse(json["expiresAt"]?.ToString(), out var expiresAt))
                        cached.ExpiresAt = expiresAt;
                }

                CacheLicense(cached);

                // Check scadenza
                if (cached.IsExpired)
                    return (false, "⏰ Periodo beta terminato", cached);

                return (true, "✅ Licenza valida", cached);
            }

            return (false, json["error"]?.ToString() ?? "❌ Verifica fallita", cached);
        }
        catch (TaskCanceledException)
        {
            // Timeout - usa cache se in grace period
            if ((DateTime.Now - cached.LastChecked).Days <= 7)
                return (cached.IsValid, "⚠️ Offline - Licenza cached valida", cached);

            return (false, "❌ Timeout verifica. Connettiti a internet.", cached);
        }
        catch (HttpRequestException)
        {
            // Errore connessione, usa cached se in grace period
            if ((DateTime.Now - cached.LastChecked).Days <= 7)
                return (cached.IsValid, "⚠️ Offline - Licenza cached valida", cached);

            return (false, "❌ Impossibile verificare licenza. Connettiti a internet.", cached);
        }
    }

    public LicenseData? GetCachedLicense()
    {
        var json = Preferences.Get(CACHE_KEY_LICENSE, string.Empty);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<LicenseData>(json);
        }
        catch
        {
            return null;
        }
    }

    public void CacheLicense(LicenseData license)
    {
        var json = JsonConvert.SerializeObject(license);
        Preferences.Set(CACHE_KEY_LICENSE, json);
        Preferences.Set(CACHE_KEY_LAST_CHECK, DateTime.Now.Ticks);
    }

    public void RevokeLicense()
    {
        Preferences.Remove(CACHE_KEY_LICENSE);
        Preferences.Remove(CACHE_KEY_LAST_CHECK);
    }

    public string GetDeviceFingerprint()
    {
        // Combinazione univoca: Model + Manufacturer + Name
        var fingerprint = $"{DeviceInfo.Model}_{DeviceInfo.Manufacturer}_{DeviceInfo.Name}";

        // Hash per privacy (opzionale)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToBase64String(hash)[..16]; // Prime 16 chars
    }

    public bool IsInGracePeriod()
    {
        var lastCheckTicks = Preferences.Get(CACHE_KEY_LAST_CHECK, 0L);
        if (lastCheckTicks == 0)
            return false;

        var lastCheck = new DateTime(lastCheckTicks);
        return (DateTime.Now - lastCheck).Days <= 7;
    }

}
