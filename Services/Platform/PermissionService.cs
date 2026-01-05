namespace MoneyMindApp.Services.Platform;

/// <summary>
/// Implementation of permission service using MAUI Essentials
/// Handles runtime permissions for Android and iOS
/// </summary>
public class PermissionService : IPermissionService
{
    /// <summary>
    /// Check if a permission is granted
    /// </summary>
    public async Task<bool> CheckPermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<TPermission>();
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionService.CheckPermissionAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Request a permission from the user
    /// </summary>
    public async Task<PermissionStatus> RequestPermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        try
        {
            var status = await Permissions.RequestAsync<TPermission>();
            return status;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionService.RequestPermissionAsync error: {ex.Message}");
            return PermissionStatus.Unknown;
        }
    }

    /// <summary>
    /// Check and request permission if not granted
    /// Returns true only if permission is granted
    /// </summary>
    public async Task<bool> CheckAndRequestAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        try
        {
            // First check if already granted
            var currentStatus = await Permissions.CheckStatusAsync<TPermission>();
            if (currentStatus == PermissionStatus.Granted)
            {
                return true;
            }

            // Check if we should show rationale
            if (currentStatus == PermissionStatus.Denied && await ShouldShowRationaleAsync<TPermission>())
            {
                // Show explanation to user before requesting again
                await ShowPermissionRationaleAsync<TPermission>();
            }

            // Request permission
            var newStatus = await Permissions.RequestAsync<TPermission>();

            if (newStatus == PermissionStatus.Granted)
            {
                return true;
            }

            // If permanently denied, offer to open settings
            if (newStatus == PermissionStatus.Denied)
            {
                var permissionName = GetPermissionName<TPermission>();
                var openSettings = await Application.Current!.MainPage!.DisplayAlert(
                    "Permesso Necessario",
                    $"MoneyMind necessita del permesso '{permissionName}' per questa funzionalità. " +
                    $"Vuoi aprire le impostazioni per concederlo?",
                    "Apri Impostazioni",
                    "Annulla");

                if (openSettings)
                {
                    await OpenAppSettingsAsync();
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionService.CheckAndRequestAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if permission should show rationale (Android only)
    /// </summary>
    public async Task<bool> ShouldShowRationaleAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<TPermission>();
            return status == PermissionStatus.Denied;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionService.ShouldShowRationaleAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Open app settings page
    /// </summary>
    public async Task OpenAppSettingsAsync()
    {
        try
        {
            AppInfo.ShowSettingsUI();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionService.OpenAppSettingsAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Show permission rationale dialog
    /// </summary>
    private async Task ShowPermissionRationaleAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        var permissionName = GetPermissionName<TPermission>();
        var explanation = GetPermissionExplanation<TPermission>();

        await Application.Current!.MainPage!.DisplayAlert(
            $"Permesso {permissionName}",
            explanation,
            "OK");
    }

    /// <summary>
    /// Get user-friendly permission name
    /// </summary>
    private string GetPermissionName<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        var type = typeof(TPermission);

        if (type == typeof(Permissions.StorageRead))
            return "Lettura Storage";
        if (type == typeof(Permissions.StorageWrite))
            return "Scrittura Storage";
        if (type == typeof(Permissions.Camera))
            return "Fotocamera";
        if (type == typeof(Permissions.Photos))
            return "Foto";
        if (type == typeof(Permissions.Media))
            return "Media";
        if (type == typeof(Permissions.NetworkState))
            return "Stato Rete";

        return type.Name;
    }

    /// <summary>
    /// Get permission explanation for user
    /// </summary>
    private string GetPermissionExplanation<TPermission>() where TPermission : Permissions.BasePermission, new()
    {
        var type = typeof(TPermission);

        if (type == typeof(Permissions.StorageRead))
            return "MoneyMind necessita di accedere allo storage per importare file di transazioni (CSV, Excel).";

        if (type == typeof(Permissions.StorageWrite))
            return "MoneyMind necessita di accedere allo storage per esportare le tue transazioni.";

        if (type == typeof(Permissions.Camera))
            return "MoneyMind necessita della fotocamera per scansionare ricevute (funzionalità futura).";

        if (type == typeof(Permissions.Photos))
            return "MoneyMind necessita di accedere alle foto per salvare screenshot o report.";

        if (type == typeof(Permissions.Media))
            return "MoneyMind necessita di accedere ai media per importare/esportare file.";

        if (type == typeof(Permissions.NetworkState))
            return "MoneyMind necessita di conoscere lo stato della rete per la sincronizzazione WiFi.";

        return "MoneyMind necessita di questo permesso per funzionare correttamente.";
    }
}
