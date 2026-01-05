using MoneyMindApp.Models;

namespace MoneyMindApp.Services.Updates;

/// <summary>
/// Interface per gestione aggiornamenti app (GitHub Releases)
/// Porta da Desktop: Services/UpdateService.vb
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Check se è disponibile aggiornamento su GitHub
    /// </summary>
    Task<UpdateInfo> CheckForUpdatesAsync();

    /// <summary>
    /// Ottieni versione corrente app
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Apri URL download aggiornamento (browser o store)
    /// </summary>
    Task OpenUpdateUrlAsync(string url);

    /// <summary>
    /// Controlla se è il primo avvio dopo aggiornamento
    /// </summary>
    bool IsFirstRunAfterUpdate();

    /// <summary>
    /// Marca versione corrente come "vista"
    /// </summary>
    void MarkCurrentVersionSeen();

    /// <summary>
    /// Scarica e installa aggiornamento (Android only)
    /// </summary>
    Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<double>? progress = null);
}
