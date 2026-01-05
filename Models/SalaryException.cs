using SQLite;

namespace MoneyMindApp.Models;

/// <summary>
/// Salary payment exception for specific months
/// Example: December 2025 → payment on day 15 instead of default day 27
/// </summary>
[Table("SalaryExceptions")]
public class SalaryException
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Month (1-12)
    /// </summary>
    public int Mese { get; set; }

    /// <summary>
    /// Year (e.g., 2025)
    /// For permanent exceptions (applies to all years), set to 0
    /// </summary>
    public int Anno { get; set; }

    /// <summary>
    /// If true, this exception applies to all years for the specified month
    /// Example: Tredicesima sempre il 15 dicembre
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// Alternative payment day for this specific month
    /// </summary>
    public int GiornoAlternativo { get; set; }

    /// <summary>
    /// Optional note/reason for the exception
    /// </summary>
    public string? Nota { get; set; }

    /// <summary>
    /// When this exception was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Display string for the month name in Italian
    /// </summary>
    [Ignore]
    public string MeseNome => Mese switch
    {
        1 => "Gennaio",
        2 => "Febbraio",
        3 => "Marzo",
        4 => "Aprile",
        5 => "Maggio",
        6 => "Giugno",
        7 => "Luglio",
        8 => "Agosto",
        9 => "Settembre",
        10 => "Ottobre",
        11 => "Novembre",
        12 => "Dicembre",
        _ => "?"
    };

    /// <summary>
    /// Display string for year: shows "Permanente" if IsPermanent = true
    /// </summary>
    [Ignore]
    public string AnnoDisplay => IsPermanent ? "Permanente" : Anno.ToString();

    /// <summary>
    /// Display string: "Dicembre 2025 → Giorno 15" or "Dicembre Permanente → Giorno 15"
    /// </summary>
    [Ignore]
    public string DisplayText => $"{MeseNome} {AnnoDisplay} → Giorno {GiornoAlternativo}";
}
