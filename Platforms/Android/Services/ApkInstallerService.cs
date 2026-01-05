using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using AndroidX.Core.Content;
using MoneyMindApp.Services.Platform;
using System.IO;

namespace MoneyMindApp.Platforms.Android.Services;

/// <summary>
/// Implementazione Android per download e installazione APK
/// </summary>
public class ApkInstallerService : IApkInstallerService
{
    private const string APK_MIME_TYPE = "application/vnd.android.package-archive";

    public async Task<string?> DownloadApkAsync(string url, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

            // Get total size
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes > 0 && progress != null;

            // Download to cache directory
            var cacheDir = Platform.CurrentActivity?.CacheDir?.AbsolutePath ?? FileSystem.CacheDirectory;
            var apkFileName = $"MoneyMind_Update_{DateTime.Now:yyyyMMdd_HHmmss}.apk";
            var apkFilePath = Path.Combine(cacheDir, apkFileName);

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(apkFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;

                if (canReportProgress)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes;
                    progress?.Report(progressPercentage);
                }
            }

            return apkFilePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading APK: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> InstallApkAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"APK file not found: {filePath}");
                return false;
            }

            var context = Platform.CurrentActivity ?? throw new InvalidOperationException("Current activity is null");

            // Android 8.0+ (API 26+) - Richiede permesso REQUEST_INSTALL_PACKAGES
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var canInstall = await CanInstallApksAsync();
                if (!canInstall)
                {
                    await RequestInstallPermissionAsync();
                    return false; // User deve concedere permesso manualmente
                }
            }

            // Crea URI con FileProvider
            var authority = $"{context.PackageName}.fileprovider";
            var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, new Java.IO.File(filePath));

            // Intent per installazione
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(apkUri, APK_MIME_TYPE);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NewTask);

            context.StartActivity(intent);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error installing APK: {ex.Message}");
            return false;
        }
    }

    public Task<bool> CanInstallApksAsync()
    {
        try
        {
            var context = Platform.CurrentActivity ?? throw new InvalidOperationException("Current activity is null");

            // Android 8.0+ (API 26+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var packageManager = context.PackageManager;
                return Task.FromResult(packageManager?.CanRequestPackageInstalls() ?? false);
            }

            // Android 7.x e precedenti - sempre true
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking install permission: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task RequestInstallPermissionAsync()
    {
        try
        {
            var context = Platform.CurrentActivity ?? throw new InvalidOperationException("Current activity is null");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // Apri impostazioni per concedere permesso installazione
                var intent = new Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources);
                intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
                intent.AddFlags(ActivityFlags.NewTask);

                context.StartActivity(intent);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting install permission: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
