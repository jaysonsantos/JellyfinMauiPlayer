using System.Runtime.CompilerServices;
using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Extensions;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Storage;

namespace JellyfinPlayer.Lib.Services;

public sealed class MediaService(
    JellyfinApiClientFactory apiClientFactory,
    IStorageService? cacheService,
    ISecureStorageService secureStorage,
    RetryPolicy retryPolicy,
    ILogger<MediaService> logger
)
{
    private const string ServerUrlKey = "jellyfin_server_url";
    private const string UserIdKey = "jellyfin_user_id";

    public async Task<IReadOnlyList<MediaItem>> GetUserViewsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get user views: API client not available");
            return [];
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get user views: User ID not available");
            return [];
        }

        try
        {
            var result = await apiClient
                .UserViews.GetAsync(
                    config =>
                    {
                        config.QueryParameters.UserId = userId;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result?.Items is null)
            {
                return [];
            }

            var baseImageUrl = await GetBaseImageUrlAsync(cancellationToken).ConfigureAwait(false);
            return result.Items.Select(item => item!.ToMediaItem(baseImageUrl)).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user views");
            return [];
        }
    }

    public async Task<QueryResult<MediaItem>> GetLibraryItemsAsync(
        string libraryId,
        int startIndex = 0,
        int limit = 50,
        CancellationToken cancellationToken = default
    )
    {
        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get library items: API client not available");
            return QueryResult<MediaItem>.Empty;
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get library items: User ID not available");
            return QueryResult<MediaItem>.Empty;
        }

        var cacheKey = $"library:{libraryId}:{startIndex}:{limit}";

        // Try cache first
        if (cacheService is not null)
        {
            var cached = await cacheService
                .GetAsync<QueryResult<MediaItem>>(cacheKey, cancellationToken)
                .ConfigureAwait(false);
            if (cached is not null)
            {
                logger.LogDebug("Cache hit for library {LibraryId}", libraryId);
                return cached;
            }
        }

        try
        {
            // Use Items endpoint with parentId to get children of the library folder
            var result = await retryPolicy
                .ExecuteAsync(
                    async ct =>
                    {
                        var queryResult = await apiClient
                            .Items.GetAsync(
                                config =>
                                {
                                    config.QueryParameters.UserId = userId;
                                    config.QueryParameters.ParentId = libraryId;
                                    config.QueryParameters.StartIndex = startIndex;
                                    config.QueryParameters.Limit = limit;
                                    config.QueryParameters.Recursive = true;
                                },
                                ct
                            )
                            .ConfigureAwait(false);
                        return queryResult ?? new BaseItemDtoQueryResult();
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result?.Items is null)
            {
                return QueryResult<MediaItem>.Empty;
            }

            var baseImageUrl = await GetBaseImageUrlAsync(cancellationToken).ConfigureAwait(false);
            var items = result.Items.Select(item => item!.ToMediaItem(baseImageUrl)).ToArray();

            var queryResult = new QueryResult<MediaItem>(
                Items: items,
                TotalRecordCount: result.TotalRecordCount ?? 0,
                StartIndex: startIndex
            );

            // Cache the result
            if (cacheService is not null)
            {
                await cacheService
                    .SetAsync(cacheKey, queryResult, TimeSpan.FromHours(1), cancellationToken)
                    .ConfigureAwait(false);
            }

            return queryResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get library items for {LibraryId}", libraryId);
            return QueryResult<MediaItem>.Empty;
        }
    }

    public async IAsyncEnumerable<MediaItem> GetLibraryItemsStreamAsync(
        string libraryId,
        int pageSize = 50,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var startIndex = 0;

        while (true)
        {
            var result = await GetLibraryItemsAsync(
                    libraryId,
                    startIndex,
                    pageSize,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result.Items.Count == 0)
                break;

            foreach (var item in result.Items)
            {
                yield return item;
            }

            if (!result.HasMore)
                break;

            startIndex = result.NextStartIndex;
        }
    }

    public async Task<MediaItem?> GetItemAsync(
        string itemId,
        CancellationToken cancellationToken = default
    )
    {
        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get item: API client not available");
            return null;
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get item: User ID not available");
            return null;
        }

        try
        {
            var result = await apiClient
                .Items[itemId]
                .GetAsync(
                    config =>
                    {
                        config.QueryParameters.UserId = userId;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is null)
            {
                return null;
            }

            var baseImageUrl = await GetBaseImageUrlAsync(cancellationToken).ConfigureAwait(false);
            return result.ToMediaItem(baseImageUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get item {ItemId}", itemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<MediaItem>> GetLatestItemsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default
    )
    {
        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get latest items: API client not available");
            return [];
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get latest items: User ID not available");
            return [];
        }

        try
        {
            var result = await apiClient
                .Items.Latest.GetAsync(
                    config =>
                    {
                        config.QueryParameters.UserId = userId;
                        config.QueryParameters.Limit = limit;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is null)
            {
                return [];
            }

            var baseImageUrl = await GetBaseImageUrlAsync(cancellationToken).ConfigureAwait(false);
            return result.Select(item => item!.ToMediaItem(baseImageUrl)).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get latest items");
            return [];
        }
    }

    public async Task<IReadOnlyList<MediaItem>> GetResumeItemsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default
    )
    {
        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get resume items: API client not available");
            return [];
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get resume items: User ID not available");
            return [];
        }

        try
        {
            var result = await apiClient
                .UserItems.Resume.GetAsync(
                    config =>
                    {
                        config.QueryParameters.UserId = userId;
                        config.QueryParameters.Limit = limit;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result?.Items is null)
            {
                return [];
            }

            var baseImageUrl = await GetBaseImageUrlAsync(cancellationToken).ConfigureAwait(false);
            return result.Items.Select(item => item!.ToMediaItem(baseImageUrl)).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get resume items");
            return [];
        }
    }

    private async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        return await secureStorage.GetAsync(UserIdKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> GetBaseImageUrlAsync(CancellationToken cancellationToken = default)
    {
        var serverUrl = await secureStorage
            .GetAsync(ServerUrlKey, cancellationToken)
            .ConfigureAwait(false);
        return serverUrl;
    }
}
