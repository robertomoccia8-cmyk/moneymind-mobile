namespace MoneyMindApp.Services.Platform;

/// <summary>
/// Stub implementation per iOS/Windows (non supportano APK)
/// </summary>
public class ApkInstallerServiceStub : IApkInstallerService
{
    public Task<string?> DownloadApkAsync(string url, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // Non supportato su iOS/Windows
        return Task.FromResult<string?>(null);
    }

    public Task<bool> InstallApkAsync(string filePath)
    {
        // Non supportato su iOS/Windows
        return Task.FromResult(false);
    }

    public Task<bool> CanInstallApksAsync()
    {
        // Non supportato su iOS/Windows
        return Task.FromResult(false);
    }

    public Task RequestInstallPermissionAsync()
    {
        // Non supportato su iOS/Windows
        return Task.CompletedTask;
    }
}
