namespace MoneyMindApp.Models;

/// <summary>
/// Model per informazioni aggiornamento disponibile
/// </summary>
public class UpdateInfo
{
    public bool IsUpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ReleasedAt { get; set; }
    public long FileSizeBytes { get; set; }

    // Computed properties
    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    public string FormattedReleaseDate => ReleasedAt.ToString("dd/MM/yyyy");

    public string UpdateSummary
    {
        get
        {
            if (!IsUpdateAvailable) return "✅ App aggiornata";
            return $"🔔 Versione {LatestVersion} disponibile";
        }
    }
}

/// <summary>
/// Model per release GitHub API response
/// </summary>
public class GitHubRelease
{
    public string tag_name { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public DateTime published_at { get; set; }
    public List<GitHubAsset> assets { get; set; } = new();
}

public class GitHubAsset
{
    public string name { get; set; } = string.Empty;
    public string browser_download_url { get; set; } = string.Empty;
    public long size { get; set; }
}
