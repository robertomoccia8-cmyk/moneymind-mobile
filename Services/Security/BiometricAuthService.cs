using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace MoneyMindApp.Services.Security;

/// <summary>
/// Service for biometric authentication using Plugin.Fingerprint
/// Supports Face ID (iOS), Touch ID (iOS), Fingerprint (Android), Windows Hello
/// </summary>
public class BiometricAuthService : IBiometricAuthService
{
    private readonly IFingerprint _fingerprint;

    public BiometricAuthService()
    {
        _fingerprint = CrossFingerprint.Current;
        System.Diagnostics.Debug.WriteLine($"BiometricAuthService initialized: {_fingerprint != null}");
    }

    /// <summary>
    /// Check if biometric authentication is available on this device
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var availability = await _fingerprint.GetAvailabilityAsync();
            var isAvailable = await _fingerprint.IsAvailableAsync();
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.IsAvailableAsync - Availability: {availability}, Result: {isAvailable}");
            return isAvailable;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.IsAvailableAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.IsAvailableAsync stack: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Get the type of biometric authentication available
    /// </summary>
    public async Task<BiometricType> GetBiometricTypeAsync()
    {
        try
        {
            var availability = await _fingerprint.GetAvailabilityAsync();

            return availability switch
            {
                FingerprintAvailability.Available => BiometricType.Fingerprint,
                FingerprintAvailability.NoFingerprint => BiometricType.None,
                FingerprintAvailability.NoPermission => BiometricType.None,
                FingerprintAvailability.NoImplementation => BiometricType.None,
                FingerprintAvailability.Unknown => BiometricType.None,
                _ => BiometricType.None
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.GetBiometricTypeAsync error: {ex.Message}");
            return BiometricType.None;
        }
    }

    /// <summary>
    /// Authenticate user with biometric credentials
    /// </summary>
    public async Task<bool> AuthenticateAsync(string reason, string cancelTitle = "Annulla")
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.AuthenticateAsync called with reason: {reason}");

            // Check if biometric is available
            var isAvailable = await IsAvailableAsync();
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.AuthenticateAsync - IsAvailable: {isAvailable}");

            if (!isAvailable)
            {
                System.Diagnostics.Debug.WriteLine("Biometric authentication not available");
                return false;
            }

            // Configure authentication request
            var request = new AuthenticationRequestConfiguration(
                title: "MoneyMind",
                reason: reason)
            {
                CancelTitle = cancelTitle,
                FallbackTitle = "Usa Password",
                AllowAlternativeAuthentication = true,
                ConfirmationRequired = false
            };

            System.Diagnostics.Debug.WriteLine("BiometricAuthService.AuthenticateAsync - Calling AuthenticateAsync...");

            // Perform authentication
            var result = await _fingerprint.AuthenticateAsync(request);

            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.AuthenticateAsync - Result: Authenticated={result.Authenticated}, Status={result.Status}, Error={result.ErrorMessage}");

            if (result.Authenticated)
            {
                System.Diagnostics.Debug.WriteLine("Biometric authentication successful");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Biometric authentication failed: Status={result.Status}, Error={result.ErrorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.AuthenticateAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.AuthenticateAsync stack: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Check if biometric authentication is enrolled (user has registered fingerprint/face)
    /// </summary>
    public async Task<bool> IsEnrolledAsync()
    {
        try
        {
            var availability = await _fingerprint.GetAvailabilityAsync();
            return availability == FingerprintAvailability.Available;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BiometricAuthService.IsEnrolledAsync error: {ex.Message}");
            return false;
        }
    }
}
