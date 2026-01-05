// FILE: Helpers/SyncHelper.cs

using MoneyMindApp.Models;
using MoneyMindApp.Models.Sync;

namespace MoneyMindApp.Helpers;

/// <summary>
/// Helper per operazioni di sincronizzazione
/// </summary>
public static class SyncHelper
{
    /// <summary>
    /// Verifica se due transazioni sono duplicate
    /// CRITERIO: Data identica + Descrizione identica (case-insensitive, trimmed)
    /// </summary>
    public static bool IsDuplicate(SyncTransaction source, Transaction dest)
    {
        // Parse data sorgente
        if (!DateTime.TryParse(source.Data, out var sourceDate))
            return false;

        // Confronta data (solo giorno)
        if (sourceDate.Date != dest.Data.Date)
            return false;

        // Confronta descrizione (case-insensitive, trimmed)
        var sourceDesc = (source.Descrizione ?? "").Trim();
        var destDesc = (dest.Descrizione ?? "").Trim();

        return sourceDesc.Equals(destDesc, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se due transazioni sono duplicate (entrambe SyncTransaction)
    /// </summary>
    public static bool IsDuplicate(SyncTransaction source, SyncTransaction dest)
    {
        // Confronta data
        if (source.Data != dest.Data)
            return false;

        // Confronta descrizione (case-insensitive, trimmed)
        var sourceDesc = (source.Descrizione ?? "").Trim();
        var destDesc = (dest.Descrizione ?? "").Trim();

        return sourceDesc.Equals(destDesc, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converte Transaction in SyncTransaction
    /// </summary>
    public static SyncTransaction ToSyncTransaction(Transaction t)
    {
        return new SyncTransaction
        {
            Data = t.Data.ToString("yyyy-MM-dd"),
            Importo = t.Importo,
            Descrizione = t.Descrizione,
            Causale = t.Causale ?? string.Empty,
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt
        };
    }

    /// <summary>
    /// Converte SyncTransaction in Transaction
    /// </summary>
    public static Transaction ToTransaction(SyncTransaction st, int accountId)
    {
        return new Transaction
        {
            Data = DateTime.Parse(st.Data),
            Importo = st.Importo,
            Descrizione = st.Descrizione,
            Causale = string.IsNullOrEmpty(st.Causale) ? null : st.Causale,
            AccountId = accountId,
            CreatedAt = st.CreatedAt ?? DateTime.Now,
            ModifiedAt = st.ModifiedAt
        };
    }

    /// <summary>
    /// Trova l'ultima data transazione in una lista
    /// </summary>
    public static DateTime? GetLatestTransactionDate(List<Transaction> transactions)
    {
        if (transactions == null || transactions.Count == 0)
            return null;

        return transactions.Max(t => t.Data);
    }

    /// <summary>
    /// Filtra transazioni più recenti di una data
    /// </summary>
    public static List<SyncTransaction> FilterNewerThan(
        List<SyncTransaction> transactions,
        DateTime? cutoffDate)
    {
        if (cutoffDate == null)
            return transactions;

        return transactions
            .Where(t => DateTime.TryParse(t.Data, out var date) && date > cutoffDate.Value)
            .ToList();
    }

    /// <summary>
    /// Genera messaggio di warning per confronto
    /// </summary>
    public static string? GenerateWarningMessage(
        int sourceCount,
        string? sourceLatestDate,
        int destCount,
        string? destLatestDate)
    {
        var warnings = new List<string>();

        // Confronta conteggio
        if (destCount > sourceCount)
        {
            warnings.Add($"La destinazione ha {destCount - sourceCount} transazioni in più");
        }

        // Confronta date
        if (!string.IsNullOrEmpty(sourceLatestDate) && !string.IsNullOrEmpty(destLatestDate))
        {
            if (DateTime.TryParse(sourceLatestDate, out var srcDate) &&
                DateTime.TryParse(destLatestDate, out var dstDate))
            {
                if (dstDate > srcDate)
                {
                    warnings.Add($"La destinazione ha dati più recenti ({dstDate:dd/MM/yyyy} vs {srcDate:dd/MM/yyyy})");
                }
            }
        }

        return warnings.Count > 0 ? string.Join(". ", warnings) : null;
    }
}
