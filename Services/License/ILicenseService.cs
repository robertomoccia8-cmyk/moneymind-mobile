using MoneyMindApp.Models;

namespace MoneyMindApp.Services.License;

/// <summary>
/// Interface per gestione Beta License (Google Sheets backend)
/// Porta da Desktop: Services/BetaLicenseManager.vb
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Verifica e attiva licenza tramite Google Sheets API
    /// </summary>
    Task<(bool Success, string Message, LicenseData? License)> ActivateLicenseAsync(string licenseKey);

    /// <summary>
    /// Check status licenza esistente (revoke check + expiration)
    /// </summary>
    Task<(bool IsValid, string Message, LicenseData? License)> CheckLicenseStatusAsync();

    /// <summary>
    /// Ottieni dati licenza cached (senza chiamata API)
    /// </summary>
    LicenseData? GetCachedLicense();

    /// <summary>
    /// Salva licenza in cache locale (Preferences)
    /// </summary>
    void CacheLicense(LicenseData license);

    /// <summary>
    /// Revoca licenza locale e rimuovi cache
    /// </summary>
    void RevokeLicense();

    /// <summary>
    /// Genera fingerprint device univoco
    /// </summary>
    string GetDeviceFingerprint();

    /// <summary>
    /// Check se app è in grace period (7 giorni offline)
    /// </summary>
    bool IsInGracePeriod();
}
