namespace MoneyMindApp.Services.Platform;

/// <summary>
/// Service for handling runtime permissions (Android, iOS)
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a permission is granted
    /// </summary>
    Task<bool> CheckPermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new();

    /// <summary>
    /// Request a permission from the user
    /// </summary>
    Task<PermissionStatus> RequestPermissionAsync<TPermission>() where TPermission : Permissions.BasePermission, new();

    /// <summary>
    /// Check and request permission if not granted
    /// </summary>
    Task<bool> CheckAndRequestAsync<TPermission>() where TPermission : Permissions.BasePermission, new();

    /// <summary>
    /// Check if permission should show rationale (user denied before)
    /// </summary>
    Task<bool> ShouldShowRationaleAsync<TPermission>() where TPermission : Permissions.BasePermission, new();

    /// <summary>
    /// Open app settings page (for when permission is permanently denied)
    /// </summary>
    Task OpenAppSettingsAsync();
}
