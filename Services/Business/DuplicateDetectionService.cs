using MoneyMindApp.Models;
using MoneyMindApp.Services.Database;
using MoneyMindApp.Services.Logging;
using System.Diagnostics;

namespace MoneyMindApp.Services.Business;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly DatabaseService _databaseService;
    private readonly ILoggingService _loggingService;

    // Match esatto 100% per robustezza
    // Non usiamo tolleranze o similarità - solo duplicati IDENTICI

    public DuplicateDetectionService(DatabaseService databaseService, ILoggingService loggingService)
    {
        _databaseService = databaseService;
        _loggingService = loggingService;
    }

    public async Task<DuplicateDetectionResult> DetectDuplicatesAsync(List<Transaction>? transactions = null)
    {
        var result = new DuplicateDetectionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _loggingService.LogInfo("Starting duplicate detection");

            // Carica transazioni se non fornite
            transactions ??= await _databaseService.GetAllTransactionsAsync();
            result.TotalTransactions = transactions.Count;

            if (transactions.Count < 2)
            {
                result.Success = true;
                result.ElapsedTime = stopwatch.Elapsed;
                return result;
            }

            // Raggruppa per data (ottimizzazione)
            var byDate = transactions.GroupBy(t => t.Data.Date).Where(g => g.Count() > 1);

            var groups = new List<DuplicateGroup>();
            var processed = new HashSet<int>();
            int groupId = 1;

            foreach (var dateGroup in byDate)
            {
                var dayTransactions = dateGroup.ToList();
                _loggingService.LogInfo($"Checking {dayTransactions.Count} transactions on {dateGroup.Key.Date:dd/MM/yyyy}");

                for (int i = 0; i < dayTransactions.Count; i++)
                {
                    if (processed.Contains(dayTransactions[i].Id))
                        continue;

                    var duplicates = new List<Transaction> { dayTransactions[i] };
                    double maxSimilarity = 0;

                    for (int j = i + 1; j < dayTransactions.Count; j++)
                    {
                        if (processed.Contains(dayTransactions[j].Id))
                            continue;

                        if (IsDuplicate(dayTransactions[i], dayTransactions[j], out double similarity))
                        {
                            duplicates.Add(dayTransactions[j]);
                            maxSimilarity = Math.Max(maxSimilarity, similarity);
                            processed.Add(dayTransactions[j].Id);

                            _loggingService.LogInfo($"✅ DUPLICATE FOUND:");
                            _loggingService.LogInfo($"  Transaction 1 [ID:{dayTransactions[i].Id}]: {dayTransactions[i].Data:dd/MM/yyyy} | {dayTransactions[i].Importo:F2}€ | '{dayTransactions[i].Descrizione}' | Causale:'{dayTransactions[i].Causale ?? "N/A"}'");
                            _loggingService.LogInfo($"  Transaction 2 [ID:{dayTransactions[j].Id}]: {dayTransactions[j].Data:dd/MM/yyyy} | {dayTransactions[j].Importo:F2}€ | '{dayTransactions[j].Descrizione}' | Causale:'{dayTransactions[j].Causale ?? "N/A"}'");
                            _loggingService.LogInfo($"  Similarity: {similarity:P0}");
                        }
                    }

                    if (duplicates.Count > 1)
                    {
                        processed.Add(dayTransactions[i].Id);

                        _loggingService.LogInfo($"📋 Duplicate group #{groupId} created with {duplicates.Count} transactions");

                        groups.Add(new DuplicateGroup
                        {
                            GroupId = groupId++,
                            Transactions = duplicates,
                            SimilarityScore = maxSimilarity,
                            SelectedToKeep = duplicates.First() // Default: mantieni il primo
                        });
                    }
                }
            }

            result.Groups = groups;
            result.DuplicateGroupsFound = groups.Count;
            result.TotalDuplicates = groups.Sum(g => g.TransactionCount - 1);
            result.Success = true;

            _loggingService.LogInfo($"Duplicate detection completed: {result.DuplicateGroupsFound} groups, {result.TotalDuplicates} duplicates");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error detecting duplicates", ex);
            result.Success = false;
        }

        stopwatch.Stop();
        result.ElapsedTime = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Verifica se due transazioni sono duplicati ESATTI (100% match)
    /// Criteri: stessa data + importo identico + descrizione identica + causale identica
    /// </summary>
    private bool IsDuplicate(Transaction t1, Transaction t2, out double similarity)
    {
        similarity = 0;

        _loggingService.LogInfo($"Comparing T1[{t1.Id}] vs T2[{t2.Id}]:");

        // 1. Stessa data (già verificato nel gruppo, ma ricontrolliamo per sicurezza)
        if (t1.Data.Date != t2.Data.Date)
        {
            _loggingService.LogInfo($"  ❌ Date mismatch: {t1.Data.Date:dd/MM/yyyy} != {t2.Data.Date:dd/MM/yyyy}");
            return false;
        }
        _loggingService.LogInfo($"  ✅ Date match: {t1.Data.Date:dd/MM/yyyy}");

        // 2. Importo IDENTICO (no tolleranza - deve essere esattamente uguale)
        if (t1.Importo != t2.Importo)
        {
            _loggingService.LogInfo($"  ❌ Amount mismatch: {t1.Importo:F2}€ != {t2.Importo:F2}€");
            return false;
        }
        _loggingService.LogInfo($"  ✅ Amount match: {t1.Importo:F2}€");

        // 3. Descrizione IDENTICA (case-insensitive, trimmed)
        var desc1 = (t1.Descrizione ?? "").Trim();
        var desc2 = (t2.Descrizione ?? "").Trim();

        if (!desc1.Equals(desc2, StringComparison.OrdinalIgnoreCase))
        {
            _loggingService.LogInfo($"  ❌ Description mismatch:");
            _loggingService.LogInfo($"     T1: '{desc1}'");
            _loggingService.LogInfo($"     T2: '{desc2}'");
            return false;
        }
        _loggingService.LogInfo($"  ✅ Description match: '{desc1}'");

        // 4. Causale IDENTICA (se presente in entrambe)
        var caus1 = (t1.Causale ?? "").Trim();
        var caus2 = (t2.Causale ?? "").Trim();

        // Se entrambe hanno causale, devono essere identiche
        if (!string.IsNullOrEmpty(caus1) && !string.IsNullOrEmpty(caus2))
        {
            if (!caus1.Equals(caus2, StringComparison.OrdinalIgnoreCase))
            {
                _loggingService.LogInfo($"  ❌ Causale mismatch:");
                _loggingService.LogInfo($"     T1: '{caus1}'");
                _loggingService.LogInfo($"     T2: '{caus2}'");
                return false;
            }
            _loggingService.LogInfo($"  ✅ Causale match: '{caus1}'");
        }
        else
        {
            _loggingService.LogInfo($"  ℹ️ Causale: T1='{caus1}', T2='{caus2}' (at least one empty - OK)");
        }

        // Match esatto al 100%
        similarity = 1.0;
        _loggingService.LogInfo($"  ✅✅✅ EXACT DUPLICATE - 100% match!");
        return true;
    }

    public async Task<int> DeleteDuplicatesAsync(List<DuplicateGroup> groups)
    {
        int deletedCount = 0;

        try
        {
            _loggingService.LogInfo($"Deleting duplicates from {groups.Count} groups");

            foreach (var group in groups)
            {
                if (group.SelectedToKeep == null)
                {
                    group.SelectedToKeep = group.Transactions.First();
                }

                foreach (var transaction in group.ToDelete)
                {
                    await _databaseService.DeleteTransactionAsync(transaction.Id);
                    deletedCount++;
                }
            }

            _loggingService.LogInfo($"Deleted {deletedCount} duplicate transactions");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting duplicates", ex);
        }

        return deletedCount;
    }

    public double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLowerInvariant().Trim();
        s2 = s2.ToLowerInvariant().Trim();

        if (s1 == s2)
            return 1.0;

        var distance = LevenshteinDistance(s1, s2);
        var maxLen = Math.Max(s1.Length, s2.Length);

        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }
}
