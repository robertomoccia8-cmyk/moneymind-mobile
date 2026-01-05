namespace MoneyMindApp.Services.Security;

/// <summary>
/// Interface for biometric authentication (Face ID, Touch ID, Fingerprint)
/// </summary>
public interface IBiometricAuthService
{
    /// <summary>
    /// Check if biometric authentication is available on this device
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get the type of biometric authentication available (Face, Fingerprint, Iris, None)
    /// </summary>
    Task<BiometricType> GetBiometricTypeAsync();

    /// <summary>
    /// Authenticate user with biometric credentials
    /// </summary>
    /// <param name="reason">Reason for authentication (shown to user)</param>
    /// <param name="cancelTitle">Cancel button text</param>
    /// <returns>True if authentication successful, false otherwise</returns>
    Task<bool> AuthenticateAsync(string reason, string cancelTitle = "Annulla");

    /// <summary>
    /// Check if biometric authentication is enrolled (user has registered fingerprint/face)
    /// </summary>
    Task<bool> IsEnrolledAsync();
}

/// <summary>
/// Types of biometric authentication
/// </summary>
public enum BiometricType
{
    None,
    Fingerprint,
    Face,
    Iris,
    Multiple
}
