using MoneyMindApp.Models;
using MoneyMindApp.Services.Logging;
using System.Text.Json;

namespace MoneyMindApp.Services.ImportExport;

/// <summary>
/// Gestisce il salvataggio e caricamento delle configurazioni di importazione
/// Le configurazioni vengono salvate come file JSON in AppDataDirectory/ConfigurazioniImportazione/
/// </summary>
public class ConfigurazioneImportazioneService : IConfigurazioneImportazioneService
{
    private readonly ILoggingService _loggingService;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _configPath;
    private bool _isDirectoryInitialized = false;
    private readonly object _initLock = new object();

    public ConfigurazioneImportazioneService(ILoggingService loggingService)
    {
        _loggingService = loggingService;

        // Opzioni JSON per serializzazione
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Ensures the configuration directory exists. Uses lazy initialization to avoid
    /// accessing FileSystem.AppDataDirectory during DI resolution.
    /// </summary>
    private void EnsureConfigDirectoryExists()
    {
        if (_isDirectoryInitialized)
            return;

        lock (_initLock)
        {
            if (_isDirectoryInitialized)
                return;

            try
            {
                // Initialize path - this is safe to call after MAUI is fully initialized
                _configPath = Path.Combine(FileSystem.AppDataDirectory, "ConfigurazioniImportazione");

                // Create directory if it doesn't exist
                if (!Directory.Exists(_configPath))
                {
                    Directory.CreateDirectory(_configPath);
                    _loggingService.LogInfo($"Created configurations directory: {_configPath}");
                }

                _isDirectoryInitialized = true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error initializing configuration directory", ex);
                throw;
            }
        }
    }

    public async Task<List<ConfigurazioneImportazione>> GetConfigurazioniAsync()
    {
        EnsureConfigDirectoryExists();
        var configurazioni = new List<ConfigurazioneImportazione>();

        try
        {
            var files = Directory.GetFiles(_configPath, "*.json");
            _loggingService.LogInfo($"Found {files.Length} configuration files");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var config = JsonSerializer.Deserialize<ConfigurazioneImportazione>(json, _jsonOptions);

                    if (config != null)
                    {
                        configurazioni.Add(config);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error loading configuration {Path.GetFileName(file)}", ex);
                }
            }

            // Ordina per ultimo utilizzo (più recenti prima)
            configurazioni = configurazioni
                .OrderByDescending(c => c.UltimoUtilizzo)
                .ToList();

            _loggingService.LogInfo($"Loaded {configurazioni.Count} configurations");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error getting configurations", ex);
        }

        return configurazioni;
    }

    public async Task SalvaConfigurazioneAsync(ConfigurazioneImportazione configurazione)
    {
        EnsureConfigDirectoryExists();
        try
        {
            // Sanitize nome per filesystem
            var safeNome = SanitizeFileName(configurazione.Nome);
            var filePath = Path.Combine(_configPath, $"{safeNome}.json");

            // Aggiorna timestamp
            if (configurazione.DataCreazione == default)
                configurazione.DataCreazione = DateTime.Now;

            configurazione.UltimoUtilizzo = DateTime.Now;

            // Serializza e salva
            var json = JsonSerializer.Serialize(configurazione, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            _loggingService.LogInfo($"Configuration '{configurazione.Nome}' saved to {filePath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error saving configuration '{configurazione.Nome}'", ex);
            throw;
        }
    }

    public async Task EliminaConfigurazioneAsync(string nome)
    {
        EnsureConfigDirectoryExists();
        try
        {
            var safeNome = SanitizeFileName(nome);
            var filePath = Path.Combine(_configPath, $"{safeNome}.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _loggingService.LogInfo($"Configuration '{nome}' deleted");
            }
            else
            {
                _loggingService.LogWarning($"Configuration '{nome}' not found for deletion");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error deleting configuration '{nome}'", ex);
            throw;
        }
    }

    public async Task<ConfigurazioneImportazione?> GetConfigurazioneAsync(string nome)
    {
        EnsureConfigDirectoryExists();
        try
        {
            var safeNome = SanitizeFileName(nome);
            var filePath = Path.Combine(_configPath, $"{safeNome}.json");

            if (!File.Exists(filePath))
            {
                _loggingService.LogWarning($"Configuration '{nome}' not found");
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<ConfigurazioneImportazione>(json, _jsonOptions);

            _loggingService.LogInfo($"Configuration '{nome}' loaded");
            return config;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error loading configuration '{nome}'", ex);
            return null;
        }
    }

    public async Task AggiornaUltimoUtilizzoAsync(string nome)
    {
        try
        {
            var config = await GetConfigurazioneAsync(nome);
            if (config != null)
            {
                config.UltimoUtilizzo = DateTime.Now;
                await SalvaConfigurazioneAsync(config);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error updating last usage for '{nome}'", ex);
        }
    }

    public async Task<bool> ExistPresetAsync()
    {
        var configs = await GetConfigurazioniAsync();
        return configs.Any(c => c.IsPreset);
    }

    /// <summary>
    /// Crea configurazioni preset per le principali banche italiane
    /// </summary>
    public async Task CreaConfigurazioniPresetAsync()
    {
        try
        {
            _loggingService.LogInfo("Creating preset configurations for Italian banks");

            var presets = new List<ConfigurazioneImportazione>();

            // 1. BCC (Banca di Credito Cooperativo) - Formato standard
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "BCC - Banca di Credito Cooperativo",
                RigaIntestazione = 1,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Importo", 1 },
                    { "Descrizione", 2 },
                    { "Causale", 3 }
                },
                Note = "Formato standard BCC con header a riga 1",
                IsPreset = true
            });

            // 2. Intesa San Paolo - Header a riga 12 (tipico)
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "Intesa San Paolo",
                RigaIntestazione = 12,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Descrizione", 1 },
                    { "Importo", 2 },
                    { "Causale", 3 }
                },
                Note = "Intesa ha tipicamente info banca nelle prime 11 righe, header a riga 12",
                IsPreset = true
            });

            // 3. UniCredit - Header a riga 8
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "UniCredit",
                RigaIntestazione = 8,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Descrizione", 2 },
                    { "Importo", 3 },
                    { "Causale", 4 }
                },
                Note = "UniCredit con header a riga 8",
                IsPreset = true
            });

            // 4. Banco BPM
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "Banco BPM",
                RigaIntestazione = 1,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Importo", 2 },
                    { "Descrizione", 3 },
                    { "Causale", 4 }
                },
                Note = "Banco BPM formato standard",
                IsPreset = true
            });

            // 5. Poste Italiane - BancoPosta
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "Poste Italiane - BancoPosta",
                RigaIntestazione = 15,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Descrizione", 1 },
                    { "Importo", 3 },
                    { "Causale", 2 }
                },
                Note = "Poste Italiane ha molte righe di intestazione (fino a 14), header effettivo a riga 15",
                IsPreset = true
            });

            // 6. Monte dei Paschi di Siena
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "Monte dei Paschi di Siena (MPS)",
                RigaIntestazione = 10,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Importo", 1 },
                    { "Descrizione", 2 },
                    { "Causale", 3 }
                },
                Note = "MPS con header a riga 10",
                IsPreset = true
            });

            // 7. BPER Banca
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "BPER Banca",
                RigaIntestazione = 1,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Descrizione", 1 },
                    { "Importo", 2 },
                    { "Causale", 3 }
                },
                Note = "BPER formato standard",
                IsPreset = true
            });

            // 8. CSV Generico Italiano
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "CSV Generico (Italiano)",
                RigaIntestazione = 1,
                HasHeaders = true,
                Separatore = ";",
                FormatoData = "dd/MM/yyyy",
                SeparatoreDecimali = ",",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Importo", 1 },
                    { "Descrizione", 2 }
                },
                Note = "Configurazione generica per CSV italiano con separatore ;",
                IsPreset = true
            });

            // 9. CSV Generico Internazionale
            presets.Add(new ConfigurazioneImportazione
            {
                Nome = "CSV Generico (Internazionale)",
                RigaIntestazione = 1,
                HasHeaders = true,
                Separatore = ",",
                FormatoData = "MM/dd/yyyy",
                SeparatoreDecimali = ".",
                MappingColonne = new Dictionary<string, int>
                {
                    { "Data", 0 },
                    { "Importo", 1 },
                    { "Descrizione", 2 }
                },
                Note = "Configurazione generica per CSV internazionale con separatore ,",
                IsPreset = true
            });

            // Salva tutti i preset
            foreach (var preset in presets)
            {
                // Controlla se esiste già
                var existing = await GetConfigurazioneAsync(preset.Nome);
                if (existing == null)
                {
                    await SalvaConfigurazioneAsync(preset);
                    _loggingService.LogInfo($"Created preset: {preset.Nome}");
                }
                else
                {
                    _loggingService.LogInfo($"Preset already exists: {preset.Nome}");
                }
            }

            _loggingService.LogInfo($"Preset configurations created: {presets.Count}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating preset configurations", ex);
        }
    }

    /// <summary>
    /// Sanitizza il nome file rimuovendo caratteri non validi
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
