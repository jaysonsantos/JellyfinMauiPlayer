using JellyfinPlayer.Lib.Api.Models;

namespace JellyfinPlayer.Lib.Services;

/// <summary>
/// Service for metadata read/update operations.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Updates an item's metadata.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="updates">The updated item data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> UpdateItemAsync(
        Guid itemId,
        BaseItemDto updates,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Refreshes metadata for an item from providers.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="replaceAllMetadata">Whether to replace all metadata.</param>
    /// <param name="replaceAllImages">Whether to replace all images.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> RefreshMetadataAsync(
        Guid itemId,
        bool replaceAllMetadata = false,
        bool replaceAllImages = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Searches remote metadata providers.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="type">The item type (Movie, Series, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result with list of remote search results.</returns>
    Task<ServiceResult<IReadOnlyList<RemoteSearchResult>>> SearchRemoteAsync(
        string query,
        string type,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Applies a remote search result to an item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="result">The remote search result to apply.</param>
    /// <param name="replaceAllImages">Whether to replace all images.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> ApplyRemoteSearchAsync(
        Guid itemId,
        RemoteSearchResult result,
        bool replaceAllImages = true,
        CancellationToken cancellationToken = default
    );
}
