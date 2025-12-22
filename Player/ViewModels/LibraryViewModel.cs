using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;

namespace Player.ViewModels;

[QueryProperty(nameof(LibraryId), "LibraryId")]
[QueryProperty(nameof(LibraryName), "LibraryName")]
public sealed partial class LibraryViewModel(
    MediaService mediaService,
    ILogger<LibraryViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial string LibraryId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LibraryName { get; set; } = "Library";

    [ObservableProperty]
    public partial IReadOnlyList<MediaItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMoreItems { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial int GridSpan { get; set; } = 2;

    private int _currentPage;
    private const int PageSize = 50;

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(LibraryId))
            return;

        IsLoading = true;
        ErrorMessage = null;
        _currentPage = 0;
        Items = [];

        try
        {
            await LoadPageAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load library items");
            ErrorMessage = "Failed to load items";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        if (IsLoadingMore || !HasMoreItems || string.IsNullOrWhiteSpace(LibraryId))
            return;

        IsLoadingMore = true;

        try
        {
            await LoadPageAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load more items");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(MediaItem? item)
    {
        if (item is null)
            return;

        await Shell.Current.GoToAsync(
            Routes.ItemDetail,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "ItemId", item.Id },
                { "ItemName", item.Name },
            }
        );
    }

    private async Task LoadPageAsync()
    {
        var startIndex = _currentPage * PageSize;
        var result = await mediaService.GetLibraryItemsAsync(LibraryId, startIndex);

        if (result.Items.Count > 0)
        {
            Items = Items.Concat(result.Items).ToList();
            HasMoreItems = result.HasMore;
            _currentPage++;
        }
        else
        {
            HasMoreItems = false;
        }
    }

    partial void OnLibraryIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            LoadItemsCommand.Execute(null);
        }
    }
}
