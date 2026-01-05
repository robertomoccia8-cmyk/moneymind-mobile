using SQLite;

namespace MoneyMindApp.Models;

/// <summary>
/// App settings stored in global database
/// Key-Value pairs for app configuration
/// </summary>
[Table("AppSettings")]
public class AppSetting
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ModifiedAt { get; set; }
}
