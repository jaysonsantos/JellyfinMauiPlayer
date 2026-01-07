using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Items.Item.Refresh;
using JellyfinPlayer.Lib.Api.Models;

namespace JellyfinPlayer.Lib.Services;

public sealed class MetadataService(
    JellyfinApiClientFactory apiClientFactory,
    ILogger<MetadataService> logger
) : IMetadataService
{
    public async Task<ServiceResult<bool>> UpdateItemAsync(
        Guid itemId,
        BaseItemDto updates,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(updates);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot update item: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            var itemIdString = itemId.ToString();
            await apiClient
                .Items[itemIdString]
                .PostAsync(updates, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Successfully updated item {ItemId}", itemId);
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update item {ItemId}", itemId);
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<bool>> RefreshMetadataAsync(
        Guid itemId,
        bool replaceAllMetadata = false,
        bool replaceAllImages = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot refresh metadata: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            var itemIdString = itemId.ToString();
            await apiClient
                .Items[itemIdString]
                .Refresh.PostAsync(
                    config =>
                    {
                        config.QueryParameters.ReplaceAllMetadata = replaceAllMetadata;
                        config.QueryParameters.ReplaceAllImages = replaceAllImages;
                        config.QueryParameters.MetadataRefreshMode =
                            MetadataRefreshMode.FullRefresh;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation("Successfully refreshed metadata for item {ItemId}", itemId);
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh metadata for item {ItemId}", itemId);
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<IReadOnlyList<RemoteSearchResult>>> SearchRemoteAsync(
        string query,
        string type,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot search remote: API client not available");
                return new ServiceResult<IReadOnlyList<RemoteSearchResult>>.Error(
                    "API client not available"
                );
            }

            var results = await ExecuteSearchByTypeAsync(apiClient, query, type, cancellationToken)
                .ConfigureAwait(false);

            if (results is null)
            {
                logger.LogWarning("Unsupported search type: {Type}", type);
                return new ServiceResult<IReadOnlyList<RemoteSearchResult>>.ValidationError(
                    $"Unsupported search type: {type}"
                );
            }

            logger.LogInformation(
                "Remote search for {Query} ({Type}) returned {Count} results",
                query,
                type,
                results.Count
            );
            return new ServiceResult<IReadOnlyList<RemoteSearchResult>>.Success(
                results.AsReadOnly()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search remote for {Query} ({Type})", query, type);
            return ex.ToServiceResult<IReadOnlyList<RemoteSearchResult>>();
        }
    }

    private async Task<List<RemoteSearchResult>?> ExecuteSearchByTypeAsync(
        JellyfinApiClient apiClient,
        string query,
        string type,
        CancellationToken cancellationToken
    )
    {
        return type.ToLowerInvariant() switch
        {
            "movie" => await SearchMovieAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "series" => await SearchSeriesAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "musicalbum" or "album" => await SearchMusicAlbumAsync(
                    apiClient,
                    query,
                    cancellationToken
                )
                .ConfigureAwait(false),
            "musicartist" or "artist" => await SearchMusicArtistAsync(
                    apiClient,
                    query,
                    cancellationToken
                )
                .ConfigureAwait(false),
            "book" => await SearchBookAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "boxset" => await SearchBoxSetAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "musicvideo" => await SearchMusicVideoAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "person" => await SearchPersonAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            "trailer" => await SearchTrailerAsync(apiClient, query, cancellationToken)
                .ConfigureAwait(false),
            _ => null,
        };
    }

    public async Task<ServiceResult<bool>> ApplyRemoteSearchAsync(
        Guid itemId,
        RemoteSearchResult result,
        bool replaceAllImages = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(result);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot apply remote search: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            var itemIdString = itemId.ToString();
            await apiClient
                .Items.RemoteSearch.Apply[itemIdString]
                .PostAsync(
                    result,
                    config =>
                    {
                        config.QueryParameters.ReplaceAllImages = replaceAllImages;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation(
                "Successfully applied remote search result to item {ItemId}",
                itemId
            );
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply remote search to item {ItemId}", itemId);
            return ex.ToServiceResult<bool>();
        }
    }

    private async Task<List<RemoteSearchResult>?> SearchMovieAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new MovieInfoRemoteSearchQuery
        {
            SearchInfo = new MovieInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.Movie.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchSeriesAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new SeriesInfoRemoteSearchQuery
        {
            SearchInfo = new SeriesInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.Series.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchMusicAlbumAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new AlbumInfoRemoteSearchQuery
        {
            SearchInfo = new AlbumInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.MusicAlbum.PostAsync(
                searchQuery,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchMusicArtistAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new ArtistInfoRemoteSearchQuery
        {
            SearchInfo = new ArtistInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.MusicArtist.PostAsync(
                searchQuery,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchBookAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new BookInfoRemoteSearchQuery
        {
            SearchInfo = new BookInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.Book.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchBoxSetAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new BoxSetInfoRemoteSearchQuery
        {
            SearchInfo = new BoxSetInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.BoxSet.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchMusicVideoAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new MusicVideoInfoRemoteSearchQuery
        {
            SearchInfo = new MusicVideoInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.MusicVideo.PostAsync(
                searchQuery,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchPersonAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new PersonLookupInfoRemoteSearchQuery
        {
            SearchInfo = new PersonLookupInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.Person.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<RemoteSearchResult>?> SearchTrailerAsync(
        JellyfinApiClient apiClient,
        string query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);

        var searchQuery = new TrailerInfoRemoteSearchQuery
        {
            SearchInfo = new TrailerInfo { Name = query },
        };

        return await apiClient
            .Items.RemoteSearch.Trailer.PostAsync(searchQuery, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
