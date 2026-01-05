using System.Collections.Generic;

namespace MoneyMindApp.Helpers;

/// <summary>
/// Helper per gestire le icone nell'app
/// Fornisce emoji e simboli Unicode per icone base
/// </summary>
public static class IconHelper
{
    // Financial Icons
    public const string Wallet = "💰";
    public const string Bank = "🏦";
    public const string Money = "💵";
    public const string CreditCard = "💳";
    public const string Chart = "📊";
    public const string TrendUp = "📈";
    public const string TrendDown = "📉";
    public const string Savings = "🐷";
    public const string Receipt = "🧾";

    // Actions
    public const string Add = "+";
    public const string Edit = "✏️";
    public const string Delete = "🗑️";
    public const string Check = "✓";
    public const string Close = "✕";
    public const string Search = "🔍";
    public const string Filter = "⚙️";
    public const string Settings = "⚙️";
    public const string Download = "⬇️";
    public const string Upload = "⬆️";
    public const string Sync = "🔄";

    // Status
    public const string Success = "✓";
    public const string Warning = "⚠️";
    public const string Error = "✕";
    public const string Info = "ℹ️";

    // Navigation
    public const string Home = "🏠";
    public const string List = "📋";
    public const string Analytics = "📊";
    public const string Account = "👤";
    public const string Back = "←";
    public const string Forward = "→";
    public const string Menu = "☰";

    // Misc
    public const string Calendar = "📅";
    public const string Eye = "👁️";
    public const string EyeOff = "🙈";
    public const string Lock = "🔒";
    public const string Unlock = "🔓";
    public const string Copy = "📋";
    public const string Share = "🔗";

    /// <summary>
    /// Get icon for transaction type
    /// </summary>
    public static string GetTransactionIcon(decimal amount)
    {
        if (amount > 0)
            return TrendUp;
        else if (amount < 0)
            return TrendDown;
        else
            return Money;
    }

    /// <summary>
    /// Get icon for account type by color
    /// </summary>
    public static string GetAccountIcon(string colorHex)
    {
        // Default icons based on color
        return colorHex?.ToLower() switch
        {
            "#2196f3" or "#1976d2" => Bank,      // Blue
            "#4caf50" or "#388e3c" => Savings,   // Green
            "#ff9800" or "#f57c00" => CreditCard,// Orange
            "#f44336" or "#d32f2f" => Wallet,    // Red
            _ => Money
        };
    }

    /// <summary>
    /// Dictionary of predefined account icons
    /// </summary>
    public static Dictionary<string, string> AccountIcons = new()
    {
        { "wallet", Wallet },
        { "bank", Bank },
        { "money", Money },
        { "card", CreditCard },
        { "savings", Savings },
    };
}
