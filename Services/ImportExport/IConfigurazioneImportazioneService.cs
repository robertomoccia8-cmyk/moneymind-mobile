using MoneyMindApp.Models;

namespace MoneyMindApp.Services.ImportExport;

/// <summary>
/// Service per gestire le configurazioni di importazione salvate
/// </summary>
public interface IConfigurazioneImportazioneService
{
    /// <summary>
    /// Carica tutte le configurazioni salvate
    /// </summary>
    Task<List<ConfigurazioneImportazione>> GetConfigurazioniAsync();

    /// <summary>
    /// Salva una configurazione
    /// </summary>
    Task SalvaConfigurazioneAsync(ConfigurazioneImportazione configurazione);

    /// <summary>
    /// Elimina una configurazione
    /// </summary>
    Task EliminaConfigurazioneAsync(string nome);

    /// <summary>
    /// Carica una configurazione per nome
    /// </summary>
    Task<ConfigurazioneImportazione?> GetConfigurazioneAsync(string nome);

    /// <summary>
    /// Aggiorna l'ultimo utilizzo di una configurazione
    /// </summary>
    Task AggiornaUltimoUtilizzoAsync(string nome);

    /// <summary>
    /// Crea le configurazioni preset per le banche italiane
    /// </summary>
    Task CreaConfigurazioniPresetAsync();

    /// <summary>
    /// Verifica se esistono configurazioni preset
    /// </summary>
    Task<bool> ExistPresetAsync();
}
