namespace MoneyMindApp.Models;

/// <summary>
/// Model per dati licenza beta (Google Sheets backend)
/// Porta da Desktop: LocalBetaLicense (BetaLicenseManager.vb)
/// </summary>
public class LicenseData
{
    public string Email { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty; // "Admin", "Plus", "Base"
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastChecked { get; set; } // LastVerified nel desktop
    public int MaxDevices { get; set; } = 1;
    public int ActiveDevices { get; set; } = 1;

    // Computed properties
    public bool IsExpired => DateTime.Now > ExpiresAt;
    public int DaysRemaining => Math.Max(0, (ExpiresAt - DateTime.Now).Days);
    public bool IsRevoked { get; set; } = false;
    public bool IsActive => !IsRevoked && !IsExpired;
    public bool IsValid => IsActive;

    // Grace period: 7 giorni offline
    public bool IsInGracePeriod => (DateTime.Now - LastChecked).Days <= 7;

    // Admin/Plus check
    public bool IsAdmin => Subscription?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsPlusOrHigher => IsAdmin || Subscription?.Equals("Plus", StringComparison.OrdinalIgnoreCase) == true;

    public string StatusText
    {
        get
        {
            if (IsRevoked) return "❌ Licenza Revocata";
            if (IsExpired) return "⏰ Licenza Scaduta";
            if (!IsActive) return "⚠️ Licenza Inattiva";
            if (DaysRemaining <= 7) return $"⚠️ Scade tra {DaysRemaining} giorni";
            return "✅ Licenza Attiva";
        }
    }

    public string FormattedExpiration => ExpiresAt.ToString("dd/MM/yyyy");
}
