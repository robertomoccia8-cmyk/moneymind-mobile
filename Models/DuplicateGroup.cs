namespace MoneyMindApp.Models;

/// <summary>
/// Gruppo di transazioni duplicate
/// </summary>
public class DuplicateGroup
{
    public int GroupId { get; set; }
    public List<Transaction> Transactions { get; set; } = new();
    public double SimilarityScore { get; set; }
    public DateTime Date => Transactions.FirstOrDefault()?.Data ?? DateTime.MinValue;
    public decimal Amount => Transactions.FirstOrDefault()?.Importo ?? 0;

    // UI Properties
    public int TransactionCount => Transactions.Count;
    public string DateFormatted => Date.ToString("dd/MM/yyyy");
    public string AmountFormatted => Amount.ToString("C2");
    public string SimilarityFormatted => $"{SimilarityScore:P0}";
    public string Description => Transactions.FirstOrDefault()?.Descrizione ?? "";

    // Selection for deletion
    public Transaction? SelectedToKeep { get; set; }
    public List<Transaction> ToDelete => Transactions.Where(t => t != SelectedToKeep).ToList();
}

/// <summary>
/// Risultato rilevamento duplicati
/// </summary>
public class DuplicateDetectionResult
{
    public bool Success { get; set; }
    public int TotalTransactions { get; set; }
    public int DuplicateGroupsFound { get; set; }
    public int TotalDuplicates { get; set; }
    public List<DuplicateGroup> Groups { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }

    public string Message => Success
        ? $"✅ Trovati {DuplicateGroupsFound} gruppi ({TotalDuplicates} duplicati) in {ElapsedTime.TotalSeconds:F1}s"
        : "❌ Errore durante rilevamento";
}
