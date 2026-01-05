using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyMindApp.Models;
using MoneyMindApp.Services.Business;
using MoneyMindApp.Services.Logging;
using System.Collections.ObjectModel;

namespace MoneyMindApp.ViewModels;

public partial class DuplicatesViewModel : ObservableObject
{
    private readonly IDuplicateDetectionService _duplicateService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasScanned;

    [ObservableProperty]
    private bool hasDuplicates;

    [ObservableProperty]
    private string statusMessage = "Premi 'Rileva Duplicati' per iniziare la scansione";

    [ObservableProperty]
    private int totalTransactions;

    [ObservableProperty]
    private int duplicateGroupsCount;

    [ObservableProperty]
    private int totalDuplicatesCount;

    [ObservableProperty]
    private ObservableCollection<DuplicateGroup> duplicateGroups = new();

    public DuplicatesViewModel(IDuplicateDetectionService duplicateService, ILoggingService loggingService)
    {
        _duplicateService = duplicateService;
        _loggingService = loggingService;
    }

    [RelayCommand]
    private async Task DetectDuplicatesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scansione in corso...";
            DuplicateGroups.Clear();

            var result = await _duplicateService.DetectDuplicatesAsync();

            TotalTransactions = result.TotalTransactions;
            DuplicateGroupsCount = result.DuplicateGroupsFound;
            TotalDuplicatesCount = result.TotalDuplicates;

            foreach (var group in result.Groups)
            {
                DuplicateGroups.Add(group);
            }

            HasScanned = true;
            HasDuplicates = result.DuplicateGroupsFound > 0;
            StatusMessage = result.Message;

            _loggingService.LogInfo($"Duplicate scan: {result.DuplicateGroupsFound} groups found");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error in duplicate detection", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAllDuplicatesAsync()
    {
        if (!HasDuplicates || DuplicateGroups.Count == 0)
            return;

        var confirm = await Shell.Current.DisplayAlert(
            "⚠️ Conferma Eliminazione",
            $"Vuoi eliminare {TotalDuplicatesCount} transazioni duplicate?\n\n" +
            "Verranno mantenute le prime transazioni di ogni gruppo.\n" +
            "Questa azione NON può essere annullata!",
            "Elimina",
            "Annulla");

        if (!confirm)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Eliminazione in corso...";

            var deletedCount = await _duplicateService.DeleteDuplicatesAsync(DuplicateGroups.ToList());

            StatusMessage = $"✅ Eliminate {deletedCount} transazioni duplicate";
            DuplicateGroups.Clear();
            HasDuplicates = false;
            DuplicateGroupsCount = 0;
            TotalDuplicatesCount = 0;

            await Shell.Current.DisplayAlert(
                "✅ Completato",
                $"Eliminate {deletedCount} transazioni duplicate.",
                "OK");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting duplicates", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(DuplicateGroup group)
    {
        if (group == null)
            return;

        var toDeleteCount = group.ToDelete.Count;
        var confirm = await Shell.Current.DisplayAlert(
            "Elimina Duplicati",
            $"Eliminare {toDeleteCount} duplicati di:\n\n" +
            $"📅 {group.DateFormatted}\n" +
            $"💰 {group.AmountFormatted}\n" +
            $"📝 {group.Description}",
            "Elimina",
            "Annulla");

        if (!confirm)
            return;

        try
        {
            var deletedCount = await _duplicateService.DeleteDuplicatesAsync(new List<DuplicateGroup> { group });

            DuplicateGroups.Remove(group);
            TotalDuplicatesCount -= deletedCount;
            DuplicateGroupsCount = DuplicateGroups.Count;
            HasDuplicates = DuplicateGroups.Count > 0;

            StatusMessage = $"✅ Eliminate {deletedCount} transazioni";
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting group", ex);
            StatusMessage = $"❌ Errore: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectToKeep(Transaction transaction)
    {
        var group = DuplicateGroups.FirstOrDefault(g => g.Transactions.Contains(transaction));
        if (group != null)
        {
            group.SelectedToKeep = transaction;
            // Force UI refresh
            var index = DuplicateGroups.IndexOf(group);
            DuplicateGroups[index] = group;
        }
    }
}
