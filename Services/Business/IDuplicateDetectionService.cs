using MoneyMindApp.Models;

namespace MoneyMindApp.Services.Business;

public interface IDuplicateDetectionService
{
    /// <summary>
    /// Rileva gruppi di transazioni duplicate
    /// </summary>
    Task<DuplicateDetectionResult> DetectDuplicatesAsync(List<Transaction>? transactions = null);

    /// <summary>
    /// Elimina le transazioni duplicate mantenendo quelle selezionate
    /// </summary>
    Task<int> DeleteDuplicatesAsync(List<DuplicateGroup> groups);

    /// <summary>
    /// Calcola similarità tra due stringhe (Levenshtein)
    /// </summary>
    double CalculateSimilarity(string s1, string s2);
}
