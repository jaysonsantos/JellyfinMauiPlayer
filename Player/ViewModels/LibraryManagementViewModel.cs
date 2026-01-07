using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;

namespace Player.ViewModels;

public sealed partial class LibraryManagementViewModel(
    ILibraryManagementService libraryManagementService,
    JellyfinApiClientFactory apiClientFactory,
    ILogger<LibraryManagementViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial IReadOnlyList<VirtualFolderInfo> Libraries { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    [RelayCommand]
    private async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            // Check if user is admin
            await CheckAdminStatusAsync(cancellationToken);

            if (!IsAdmin)
            {
                ErrorMessage = "You must be an administrator to access library management.";
                return;
            }

            // Load libraries
            await LoadLibrariesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load library management data");
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckAdminStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var apiClient = await apiClientFactory.CreateClientAsync(cancellationToken);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot check admin status: API client not available");
                IsAdmin = false;
                return;
            }

            var currentUser = await apiClient.Users.Me.GetAsync(
                cancellationToken: cancellationToken
            );

            IsAdmin = currentUser?.Policy?.IsAdministrator ?? false;
            logger.LogInformation("User admin status: {IsAdmin}", IsAdmin);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check admin status");
            IsAdmin = false;
        }
    }

    private async Task LoadLibrariesAsync(CancellationToken cancellationToken)
    {
        var result = await libraryManagementService.GetLibrariesAsync(cancellationToken);

        if (result is ServiceResult<IReadOnlyList<VirtualFolderInfo>>.Success success)
        {
            Libraries = success.Value;
            logger.LogInformation("Loaded {Count} libraries", Libraries.Count);
        }
        else if (result is ServiceResult<IReadOnlyList<VirtualFolderInfo>>.Error error)
        {
            ErrorMessage = $"Failed to load libraries: {error.Message}";
            logger.LogWarning("Failed to load libraries: {Error}", error.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteLibraryAsync(VirtualFolderInfo? library)
    {
        if (library is null || string.IsNullOrWhiteSpace(library.Name))
            return;

        // Show confirmation dialog
        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Library",
            $"Are you sure you want to delete the library '{library.Name}'? This will remove the library configuration but not the actual files.",
            "Delete",
            "Cancel"
        );

        if (!confirmed)
            return;

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var cancellationToken = CancellationToken.None;
            var result = await libraryManagementService.DeleteLibraryAsync(
                library.Name,
                cancellationToken
            );

            if (result is ServiceResult<bool>.Success)
            {
                SuccessMessage = $"Library '{library.Name}' deleted successfully.";
                logger.LogInformation("Successfully deleted library {LibraryName}", library.Name);

                // Reload libraries
                await LoadLibrariesAsync(cancellationToken);
            }
            else if (result is ServiceResult<bool>.Error error)
            {
                ErrorMessage = $"Failed to delete library: {error.Message}";
                logger.LogWarning(
                    "Failed to delete library {LibraryName}: {Error}",
                    library.Name,
                    error.Message
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete library {LibraryName}", library.Name);
            ErrorMessage = $"Failed to delete library: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ScanLibraryAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await libraryManagementService.RefreshLibraryAsync(cancellationToken);

            if (result is ServiceResult<bool>.Success)
            {
                SuccessMessage = "Library scan started successfully.";
                logger.LogInformation("Successfully started library scan");
            }
            else if (result is ServiceResult<bool>.Error error)
            {
                ErrorMessage = $"Failed to start library scan: {error.Message}";
                logger.LogWarning("Failed to start library scan: {Error}", error.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start library scan");
            ErrorMessage = $"Failed to start library scan: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
