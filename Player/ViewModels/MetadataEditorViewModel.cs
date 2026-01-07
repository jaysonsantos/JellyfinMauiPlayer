using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Services;

namespace Player.ViewModels;

[QueryProperty(nameof(ItemId), "ItemId")]
public sealed partial class MetadataEditorViewModel(
    JellyfinApiClientFactory apiClientFactory,
    IMetadataService metadataService,
    ILogger<MetadataEditorViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial string ItemId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Year { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Overview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Genres { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    private BaseItemDto? originalItem;

    [RelayCommand]
    private async Task LoadItemAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ItemId))
            return;

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var apiClient = await apiClientFactory.CreateClientAsync().ConfigureAwait(false);
            if (apiClient is null)
            {
                ErrorMessage = "API client not available";
                return;
            }

            var result = await apiClient.Items[ItemId].GetAsync().ConfigureAwait(false);
            if (result is null)
            {
                ErrorMessage = "Failed to load item details.";
                return;
            }

            originalItem = result;
            UpdateFormFields(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load item {ItemId}", ItemId);
            ErrorMessage = $"Failed to load item: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateFormFields(BaseItemDto item)
    {
        Title = item.Name ?? string.Empty;
        Year = item.ProductionYear?.ToString() ?? string.Empty;
        Overview = item.Overview ?? string.Empty;
        Genres = item.Genres is not null ? string.Join(", ", item.Genres) : string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving || originalItem is null)
            return;

        // Validate inputs
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            // Create updated item with modified fields
            var updatedItem = new BaseItemDto
            {
                Name = Title.Trim(),
                ProductionYear = int.TryParse(Year, out int yearValue) ? yearValue : null,
                Overview = string.IsNullOrWhiteSpace(Overview) ? null : Overview.Trim(),
                Genres = string.IsNullOrWhiteSpace(Genres)
                    ? null
                    : Genres
                        .Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                        .ToList(),
            };

            var result = await metadataService
                .UpdateItemAsync(Guid.Parse(ItemId), updatedItem)
                .ConfigureAwait(false);

            if (result is ServiceResult<bool>.Success)
            {
                SuccessMessage = "Metadata saved successfully!";
                logger.LogInformation("Successfully saved metadata for item {ItemId}", ItemId);

                // Reload the item to get the latest data
                await LoadItemAsync().ConfigureAwait(false);
            }
            else if (result is ServiceResult<bool>.Error errorResult)
            {
                ErrorMessage = errorResult.Message;
                logger.LogWarning("Failed to save metadata: {Message}", errorResult.Message);
            }
            else if (result is ServiceResult<bool>.ValidationError validationResult)
            {
                ErrorMessage = validationResult.Message;
                logger.LogWarning("Validation error: {Message}", validationResult.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save metadata for item {ItemId}", ItemId);
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task RefreshMetadataAsync()
    {
        if (IsSaving || string.IsNullOrWhiteSpace(ItemId))
            return;

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await metadataService
                .RefreshMetadataAsync(
                    Guid.Parse(ItemId),
                    replaceAllMetadata: false,
                    replaceAllImages: false
                )
                .ConfigureAwait(false);

            if (result is ServiceResult<bool>.Success)
            {
                SuccessMessage = "Metadata refresh initiated. This may take a few moments.";
                logger.LogInformation(
                    "Successfully initiated metadata refresh for item {ItemId}",
                    ItemId
                );

                // Wait a moment then reload
                await Task.Delay(2000).ConfigureAwait(false);
                await LoadItemAsync().ConfigureAwait(false);
            }
            else if (result is ServiceResult<bool>.Error errorResult)
            {
                ErrorMessage = errorResult.Message;
                logger.LogWarning("Failed to refresh metadata: {Message}", errorResult.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh metadata for item {ItemId}", ItemId);
            ErrorMessage = $"Failed to refresh: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnItemIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            LoadItemCommand.Execute(null);
        }
    }
}
