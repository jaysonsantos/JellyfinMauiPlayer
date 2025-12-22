using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;

namespace Player.ViewModels;

public sealed partial class HomeViewModel(
    MediaService mediaService,
    AuthenticationService authenticationService,
    ILogger<HomeViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial IReadOnlyList<MediaItem> Libraries { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MediaItem> LatestItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<MediaItem> ResumeItems { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var cancellationToken = CancellationToken.None;
        if (IsLoading || cancellationToken.IsCancellationRequested)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            logger.LogInformation("Loading home page data...");

            // Load libraries (user views)
            Libraries = await mediaService.GetUserViewsAsync(cancellationToken);
            logger.LogInformation("Loaded {Count} libraries", Libraries.Count);

            // Load latest items
            LatestItems = await mediaService.GetLatestItemsAsync(limit: 20, cancellationToken);
            logger.LogInformation("Loaded {Count} latest items", LatestItems.Count);

            // Load resume items
            ResumeItems = await mediaService.GetResumeItemsAsync(
                limit: 20,
                cancellationToken: cancellationToken
            );
            logger.LogInformation("Loaded {Count} resume items", ResumeItems.Count);

            if (Libraries.Count == 0 && LatestItems.Count == 0 && ResumeItems.Count == 0)
            {
                ErrorMessage =
                    "No content found. Make sure you're logged in and have libraries configured.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load home data");
            ErrorMessage = $"Failed to load content: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToLibraryAsync(MediaItem? library)
    {
        if (library is null)
            return;

        await Shell.Current.GoToAsync(
            Routes.Library,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "LibraryId", library.Id },
                { "LibraryName", library.Name },
            }
        );
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(MediaItem? item)
    {
        if (item is null)
            return;

        var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "ItemId", item.Id },
            { "ItemName", item.Name },
        };

        await Shell.Current.GoToAsync(Routes.ItemDetail, parameters).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            logger.LogInformation("User requested logout");
            await authenticationService.LogoutAsync();

            // Navigate to login page
            await Shell.Current.GoToAsync($"//{Routes.Login}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to logout");
            ErrorMessage = $"Failed to logout: {ex.Message}";
        }
    }
}
