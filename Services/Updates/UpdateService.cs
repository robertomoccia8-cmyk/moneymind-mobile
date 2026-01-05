using MoneyMindApp.Models;
using MoneyMindApp.Services.Platform;
using Newtonsoft.Json;

namespace MoneyMindApp.Services.Updates;

/// <summary>
/// Implementazione servizio aggiornamenti (GitHub Releases API)
/// Porta da Desktop: Services/UpdateService.vb
/// </summary>
public class UpdateService : IUpdateService
{
    private const string GITHUB_API_URL = "https://api.github.com/repos/robertomoccia8-cmyk/moneymind-mobile/releases/latest";
    private const string CACHE_KEY_LAST_VERSION_SEEN = "last_version_seen";

    private readonly HttpClient _httpClient;
    private readonly IApkInstallerService _apkInstaller;

    public UpdateService(IApkInstallerService apkInstaller)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MoneyMind-Mobile");
        _apkInstaller = apkInstaller;
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
            var release = JsonConvert.DeserializeObject<GitHubRelease>(response);

            if (release == null)
                return CreateNoUpdateInfo();

            var latestVersion = ParseVersion(release.tag_name);
            var currentVersion = ParseVersion(GetCurrentVersion());

            var isUpdateAvailable = latestVersion > currentVersion;

            // Trova asset Android (APK o AAB)
            var androidAsset = release.assets.FirstOrDefault(a =>
                a.name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
                a.name.EndsWith(".aab", StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo
            {
                IsUpdateAvailable = isUpdateAvailable,
                CurrentVersion = GetCurrentVersion(),
                LatestVersion = release.tag_name.TrimStart('v'),
                ReleaseNotes = release.body,
                DownloadUrl = androidAsset?.browser_download_url ?? string.Empty,
                ReleasedAt = release.published_at,
                FileSizeBytes = androidAsset?.size ?? 0
            };
        }
        catch (HttpRequestException)
        {
            // Errore connessione, ritorna "nessun update"
            return CreateNoUpdateInfo("❌ Impossibile verificare aggiornamenti (offline)");
        }
        catch (Exception ex)
        {
            return CreateNoUpdateInfo($"❌ Errore: {ex.Message}");
        }
    }

    public string GetCurrentVersion()
    {
        return AppInfo.VersionString; // Es: "1.0.0"
    }

    public async Task OpenUpdateUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            // Android: Apri browser per download APK
            // iOS: Redirect a App Store
            await Browser.OpenAsync(url, BrowserLaunchMode.External);
        }
        catch
        {
            // Fallback: copy to clipboard
            await Clipboard.SetTextAsync(url);
        }
    }

    public bool IsFirstRunAfterUpdate()
    {
        var lastVersionSeen = Preferences.Get(CACHE_KEY_LAST_VERSION_SEEN, string.Empty);
        var currentVersion = GetCurrentVersion();

        return lastVersionSeen != currentVersion;
    }

    public void MarkCurrentVersionSeen()
    {
        Preferences.Set(CACHE_KEY_LAST_VERSION_SEEN, GetCurrentVersion());
    }

    private Version ParseVersion(string versionString)
    {
        // Rimuovi prefisso "v" se presente
        versionString = versionString.TrimStart('v');

        // Parse version (es: "1.2.3" → Version(1,2,3))
        if (Version.TryParse(versionString, out var version))
            return version;

        return new Version(0, 0, 0);
    }

    private UpdateInfo CreateNoUpdateInfo(string? message = null)
    {
        return new UpdateInfo
        {
            IsUpdateAvailable = false,
            CurrentVersion = GetCurrentVersion(),
            LatestVersion = GetCurrentVersion(),
            ReleaseNotes = message ?? "✅ App aggiornata all'ultima versione",
            DownloadUrl = string.Empty,
            ReleasedAt = DateTime.Now,
            FileSizeBytes = 0
        };
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<double>? progress = null)
    {
        try
        {
            // Verifica permessi installazione
            var canInstall = await _apkInstaller.CanInstallApksAsync();
            if (!canInstall)
            {
                await _apkInstaller.RequestInstallPermissionAsync();
                return false;
            }

            // Download APK
            var apkPath = await _apkInstaller.DownloadApkAsync(downloadUrl, progress);
            if (string.IsNullOrEmpty(apkPath))
                return false;

            // Installa APK
            return await _apkInstaller.InstallApkAsync(apkPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading/installing update: {ex.Message}");
            return false;
        }
    }
}
