using System.Text.Json.Serialization;

namespace MoneyMindApp.Models;

/// <summary>
/// Configurazione salvata per importazione CSV/Excel
/// Permette di salvare preset per diverse banche e riutilizzarli
/// </summary>
public class ConfigurazioneImportazione
{
    /// <summary>
    /// Nome identificativo della configurazione (es. "BCC Bank", "Intesa San Paolo", "UniCredit")
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Riga che contiene le intestazioni delle colonne (1-based)
    /// Es. Se il CSV ha loghi/info nelle prime 9 righe, e header a riga 10, RigaIntestazione = 10
    /// </summary>
    public int RigaIntestazione { get; set; } = 1;

    /// <summary>
    /// Indica se il file ha righe di intestazione
    /// </summary>
    public bool HasHeaders { get; set; } = true;

    /// <summary>
    /// Separatore usato nel CSV ("," o ";" o "\t")
    /// </summary>
    public string Separatore { get; set; } = ";";

    /// <summary>
    /// Mapping colonne: chiave = campo (Data, Importo, Descrizione, Causale), valore = indice colonna (0-based)
    /// Es. { "Data": 0, "Importo": 2, "Descrizione": 4, "Causale": 5 }
    /// </summary>
    public Dictionary<string, int> MappingColonne { get; set; } = new();

    /// <summary>
    /// Formato data usato nel file (es. "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd")
    /// </summary>
    public string FormatoData { get; set; } = "dd/MM/yyyy";

    /// <summary>
    /// Separatore decimali usato nel file ("," italiano o "." US)
    /// </summary>
    public string SeparatoreDecimali { get; set; } = ",";

    /// <summary>
    /// Data creazione configurazione
    /// </summary>
    public DateTime DataCreazione { get; set; } = DateTime.Now;

    /// <summary>
    /// Data ultimo utilizzo configurazione
    /// </summary>
    public DateTime UltimoUtilizzo { get; set; } = DateTime.Now;

    /// <summary>
    /// Note aggiuntive sulla configurazione
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Indica se è una configurazione preset (non eliminabile)
    /// </summary>
    [JsonIgnore]
    public bool IsPreset { get; set; }

    /// <summary>
    /// Crea una ColumnMapping da questa configurazione
    /// </summary>
    public ColumnMapping ToColumnMapping()
    {
        // Debug: log dictionary content
        var mappingInfo = string.Join(", ", MappingColonne.Select(kv => $"{kv.Key}={kv.Value}"));
        System.Diagnostics.Debug.WriteLine($"[ToColumnMapping] Nome: {Nome}, MappingColonne count: {MappingColonne.Count}, Content: [{mappingInfo}]");

        return new ColumnMapping
        {
            DataColumn = MappingColonne.ContainsKey("Data") ? MappingColonne["Data"] : -1,
            ImportoColumn = MappingColonne.ContainsKey("Importo") ? MappingColonne["Importo"] : -1,
            DescrizioneColumn = MappingColonne.ContainsKey("Descrizione") ? MappingColonne["Descrizione"] : -1,
            CausaleColumn = MappingColonne.ContainsKey("Causale") ? MappingColonne["Causale"] : -1,
            DateFormat = FormatoData,
            DecimalSeparator = SeparatoreDecimali,
            HasHeader = HasHeaders,
            HeaderRowNumber = RigaIntestazione
        };
    }

    /// <summary>
    /// Crea una ConfigurazioneImportazione da un ColumnMapping
    /// </summary>
    public static ConfigurazioneImportazione FromColumnMapping(string nome, ColumnMapping mapping)
    {
        var config = new ConfigurazioneImportazione
        {
            Nome = nome,
            RigaIntestazione = mapping.HeaderRowNumber,
            HasHeaders = mapping.HasHeader,
            FormatoData = mapping.DateFormat,
            SeparatoreDecimali = mapping.DecimalSeparator,
            DataCreazione = DateTime.Now,
            UltimoUtilizzo = DateTime.Now
        };

        if (mapping.DataColumn >= 0)
            config.MappingColonne["Data"] = mapping.DataColumn;
        if (mapping.ImportoColumn >= 0)
            config.MappingColonne["Importo"] = mapping.ImportoColumn;
        if (mapping.DescrizioneColumn >= 0)
            config.MappingColonne["Descrizione"] = mapping.DescrizioneColumn;
        if (mapping.CausaleColumn >= 0)
            config.MappingColonne["Causale"] = mapping.CausaleColumn;

        return config;
    }

    /// <summary>
    /// Clona la configurazione
    /// </summary>
    public ConfigurazioneImportazione Clone()
    {
        return new ConfigurazioneImportazione
        {
            Nome = Nome,
            RigaIntestazione = RigaIntestazione,
            HasHeaders = HasHeaders,
            Separatore = Separatore,
            MappingColonne = new Dictionary<string, int>(MappingColonne),
            FormatoData = FormatoData,
            SeparatoreDecimali = SeparatoreDecimali,
            DataCreazione = DataCreazione,
            UltimoUtilizzo = DateTime.Now,
            Note = Note,
            IsPreset = IsPreset
        };
    }

    public override string ToString() => Nome;
}
