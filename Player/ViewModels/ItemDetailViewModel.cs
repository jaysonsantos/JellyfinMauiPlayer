using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;

namespace Player.ViewModels;

[QueryProperty(nameof(ItemId), "ItemId")]
[QueryProperty(nameof(ItemName), "ItemName")]
public sealed partial class ItemDetailViewModel(
    MediaService mediaService,
    IPlaybackService playbackService,
    ILogger<ItemDetailViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial string ItemId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemName { get; set; } = "Item";

    [ObservableProperty]
    public partial MediaItem? Item { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? GenresText { get; set; }

    [ObservableProperty]
    public partial string? RuntimeText { get; set; }

    [ObservableProperty]
    public partial string? RatingText { get; set; }

    [ObservableProperty]
    public partial string? YearText { get; set; }

    [ObservableProperty]
    public partial string? OfficialRatingText { get; set; }

    [ObservableProperty]
    public partial string? DateCreatedText { get; set; }

    [ObservableProperty]
    public partial string? ReleaseDateText { get; set; }

    [ObservableProperty]
    public partial string? SeriesInfoText { get; set; }

    [ObservableProperty]
    public partial bool HasSeriesInfo { get; set; }

    [ObservableProperty]
    public partial bool CanPlay { get; set; }

    [ObservableProperty]
    public partial bool CanResume { get; set; }

    [ObservableProperty]
    public partial string? ResumePositionText { get; set; }

    private TimeSpan? _resumePosition;
    private const long MinResumeThresholdTicks = 300_000_000; // 30 seconds in ticks

    [RelayCommand]
    private async Task LoadItemAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ItemId))
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var mediaItem = await mediaService.GetItemAsync(ItemId).ConfigureAwait(false);
            if (mediaItem is null)
            {
                ErrorMessage = "Failed to load item details.";
                return;
            }

            UpdateItemProperties(mediaItem);
            await CheckResumePositionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load item {ItemId}", ItemId);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateItemProperties(MediaItem mediaItem)
    {
        Item = mediaItem;
        ItemName = mediaItem.Name;
        GenresText = mediaItem.Genres.Count > 0 ? string.Join(", ", mediaItem.Genres) : null;
        RuntimeText = mediaItem.RuntimeMinutes is { } runtime ? $"{runtime} min" : null;
        RatingText = mediaItem.CommunityRating is { } rating
            ? rating.ToString("0.0", CultureInfo.CurrentCulture)
            : null;
        YearText = mediaItem.ProductionYear;
        OfficialRatingText = mediaItem.OfficialRating;
        DateCreatedText = mediaItem.DateCreated?.ToString(
            "MMMM dd, yyyy",
            CultureInfo.CurrentCulture
        );
        ReleaseDateText = mediaItem.ReleaseDate?.ToString(
            "MMMM dd, yyyy",
            CultureInfo.CurrentCulture
        );

        // Series/Episode information
        if (!string.IsNullOrWhiteSpace(mediaItem.SeriesName))
        {
            var episodeInfo = $"{mediaItem.SeriesName}";
            if (mediaItem is { ParentIndexNumber: not null, IndexNumber: not null })
            {
                episodeInfo +=
                    $" - Season {mediaItem.ParentIndexNumber}, Episode {mediaItem.IndexNumber}";
            }
            SeriesInfoText = episodeInfo;
            HasSeriesInfo = true;
        }
        else
        {
            HasSeriesInfo = false;
        }

        // Determine if item can be played
        CanPlay =
            !mediaItem.IsFolder
            && (
                mediaItem.Type.Equals("Movie", StringComparison.OrdinalIgnoreCase)
                || mediaItem.Type.Equals("Episode", StringComparison.OrdinalIgnoreCase)
                || mediaItem.Type.Equals("Video", StringComparison.OrdinalIgnoreCase)
                || mediaItem.Type.Equals("Audio", StringComparison.OrdinalIgnoreCase)
            );
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (Item is null)
            return;

        try
        {
            // Navigate to video player page - start from beginning
            var navigationParams = new Dictionary<string, object>(StringComparer.CurrentCulture)
            {
                { "ItemId", ItemId },
                { "ItemName", Item.Name },
                { "StartPosition", TimeSpan.Zero },
            };

            await Shell
                .Current.GoToAsync(Routes.VideoPlayer, navigationParams)
                .ConfigureAwait(false);

            logger.LogInformation("Navigating to video player for item {ItemId}", ItemId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start playback for item {ItemId}", ItemId);
            await Shell
                .Current.DisplayAlertAsync("Playback Error", ex.Message, "OK")
                .ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (Item is null || _resumePosition is null)
            return;

        try
        {
            // Navigate to video player page - resume from saved position
            var navigationParams = new Dictionary<string, object>(StringComparer.CurrentCulture)
            {
                { "ItemId", ItemId },
                { "ItemName", Item.Name },
                { "StartPosition", _resumePosition.Value },
            };

            await Shell
                .Current.GoToAsync(Routes.VideoPlayer, navigationParams)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Navigating to video player for item {ItemId} to resume at {Position}",
                ItemId,
                _resumePosition.Value
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resume playback for item {ItemId}", ItemId);
            await Shell
                .Current.DisplayAlertAsync("Playback Error", ex.Message, "OK")
                .ConfigureAwait(false);
        }
    }

    private async Task CheckResumePositionAsync()
    {
        try
        {
            var playbackState = await playbackService
                .GetPlaybackStateAsync(ItemId, CancellationToken.None)
                .ConfigureAwait(false);

            if (playbackState is not null && playbackState.PositionTicks > MinResumeThresholdTicks)
            {
                _resumePosition = TimeSpan.FromTicks(playbackState.PositionTicks);
                ResumePositionText = FormatTimeSpan(_resumePosition.Value);
                CanResume = true;

                logger.LogInformation(
                    "Found resume position for item {ItemId}: {Position}",
                    ItemId,
                    _resumePosition.Value
                );
            }
            else
            {
                _resumePosition = null;
                ResumePositionText = null;
                CanResume = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check resume position for item {ItemId}", ItemId);
            _resumePosition = null;
            ResumePositionText = null;
            CanResume = false;
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return timeSpan.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }
        return timeSpan.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Item is null)
            return;

        // Favorites functionality will be implemented in Phase 5
        await Shell
            .Current.DisplayAlertAsync(
                "Favorites",
                "Favorites functionality will be implemented in Phase 5.",
                "OK"
            )
            .ConfigureAwait(false);
        logger.LogInformation("Toggle favorite requested for item {ItemId}", ItemId);
    }

    [RelayCommand]
    private async Task ShowMoreInfoAsync()
    {
        if (Item is null)
            return;

        // Display additional information in an alert for now
        var info = $"ID: {Item.Id}\n";
        info += $"Type: {Item.Type}\n";
        if (Item.ChildCount.HasValue)
        {
            info += $"Child Items: {Item.ChildCount}\n";
        }
        if (Item.RunTimeTicks.HasValue)
        {
            info += $"Runtime Ticks: {Item.RunTimeTicks}\n";
        }
        info += $"Is Folder: {Item.IsFolder}\n";
        if (!string.IsNullOrWhiteSpace(Item.CollectionType))
        {
            info += $"Collection Type: {Item.CollectionType}\n";
        }

        await Shell
            .Current.DisplayAlertAsync("Additional Information", info, "OK")
            .ConfigureAwait(false);
    }

    partial void OnItemIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            LoadItemCommand.Execute(null);
        }
    }
}
