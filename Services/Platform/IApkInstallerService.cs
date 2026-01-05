namespace MoneyMindApp.Services.Platform;

/// <summary>
/// Interface per installazione APK (Android-only)
/// </summary>
public interface IApkInstallerService
{
    /// <summary>
    /// Scarica APK da URL e mostra progress
    /// </summary>
    Task<string?> DownloadApkAsync(string url, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installa APK scaricato
    /// </summary>
    Task<bool> InstallApkAsync(string filePath);

    /// <summary>
    /// Verifica se app può installare APK (permessi OK)
    /// </summary>
    Task<bool> CanInstallApksAsync();

    /// <summary>
    /// Richiedi permesso installazione APK (Android 8.0+)
    /// </summary>
    Task RequestInstallPermissionAsync();
}
