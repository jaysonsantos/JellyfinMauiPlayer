using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;

namespace JellyfinPlayer.Lib.Services;

/// <summary>
/// Service for managing Jellyfin libraries (admin operations).
/// </summary>
public sealed class LibraryManagementService(
    JellyfinApiClientFactory apiClientFactory,
    ILogger<LibraryManagementService> logger
) : ILibraryManagementService
{
    public async Task<ServiceResult<IReadOnlyList<VirtualFolderInfo>>> GetLibrariesAsync(
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
                logger.LogWarning("Cannot get libraries: API client not available");
                return new ServiceResult<IReadOnlyList<VirtualFolderInfo>>.Error(
                    "API client not available"
                );
            }

            var result = await apiClient
                .Library.VirtualFolders.GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                return new ServiceResult<IReadOnlyList<VirtualFolderInfo>>.Success([]);
            }

            IReadOnlyList<VirtualFolderInfo> libraries = result.AsReadOnly();
            return new ServiceResult<IReadOnlyList<VirtualFolderInfo>>.Success(libraries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get libraries");
            return ex.ToServiceResult<IReadOnlyList<VirtualFolderInfo>>();
        }
    }

    public async Task<ServiceResult<bool>> CreateLibraryAsync(
        string name,
        string? collectionType = null,
        IReadOnlyList<string>? paths = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot create library: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            var dto = new AddVirtualFolderDto { LibraryOptions = new LibraryOptions() };

            await apiClient
                .Library.VirtualFolders.PostAsync(
                    dto,
                    config =>
                    {
                        config.QueryParameters.Name = name;
                        if (!string.IsNullOrWhiteSpace(collectionType))
                        {
                            config.QueryParameters.CollectionType = ParseCollectionType(
                                collectionType
                            );
                        }
                        if (paths is not null && paths.Count > 0)
                        {
                            config.QueryParameters.Paths = paths.ToArray();
                        }
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation("Successfully created library {LibraryName}", name);
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create library {LibraryName}", name);
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<bool>> DeleteLibraryAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot delete library: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            await apiClient
                .Library.VirtualFolders.DeleteAsync(
                    config =>
                    {
                        config.QueryParameters.Name = name;
                        config.QueryParameters.RefreshLibrary = false;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation("Successfully deleted library {LibraryName}", name);
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete library {LibraryName}", name);
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<bool>> AddPathAsync(
        string libraryName,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot add path to library: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            var dto = new MediaPathDto { Name = libraryName, Path = path };

            await apiClient
                .Library.VirtualFolders.Paths.PostAsync(
                    dto,
                    config =>
                    {
                        config.QueryParameters.RefreshLibrary = false;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation(
                "Successfully added path {Path} to library {LibraryName}",
                path,
                libraryName
            );
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to add path {Path} to library {LibraryName}",
                path,
                libraryName
            );
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<bool>> RemovePathAsync(
        string libraryName,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var apiClient = await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot remove path from library: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            await apiClient
                .Library.VirtualFolders.Paths.DeleteAsync(
                    config =>
                    {
                        config.QueryParameters.Name = libraryName;
                        config.QueryParameters.Path = path;
                        config.QueryParameters.RefreshLibrary = false;
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            logger.LogInformation(
                "Successfully removed path {Path} from library {LibraryName}",
                path,
                libraryName
            );
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to remove path {Path} from library {LibraryName}",
                path,
                libraryName
            );
            return ex.ToServiceResult<bool>();
        }
    }

    public async Task<ServiceResult<bool>> RefreshLibraryAsync(
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
                logger.LogWarning("Cannot refresh library: API client not available");
                return new ServiceResult<bool>.Error("API client not available");
            }

            await apiClient
                .Library.Refresh.PostAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Successfully started library refresh");
            return new ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh library");
            return ex.ToServiceResult<bool>();
        }
    }

    private static JellyfinPlayer.Lib.Api.Library.VirtualFolders.CollectionTypeOptions? ParseCollectionType(
        string collectionType
    )
    {
        return collectionType.ToLowerInvariant() switch
        {
            "movies" => JellyfinPlayer.Lib.Api.Library.VirtualFolders.CollectionTypeOptions.Movies,
            "tvshows" => JellyfinPlayer
                .Lib
                .Api
                .Library
                .VirtualFolders
                .CollectionTypeOptions
                .Tvshows,
            "music" => JellyfinPlayer.Lib.Api.Library.VirtualFolders.CollectionTypeOptions.Music,
            "musicvideos" => JellyfinPlayer
                .Lib
                .Api
                .Library
                .VirtualFolders
                .CollectionTypeOptions
                .Musicvideos,
            "homevideos" => JellyfinPlayer
                .Lib
                .Api
                .Library
                .VirtualFolders
                .CollectionTypeOptions
                .Homevideos,
            "boxsets" => JellyfinPlayer
                .Lib
                .Api
                .Library
                .VirtualFolders
                .CollectionTypeOptions
                .Boxsets,
            "books" => JellyfinPlayer.Lib.Api.Library.VirtualFolders.CollectionTypeOptions.Books,
            "mixed" => JellyfinPlayer.Lib.Api.Library.VirtualFolders.CollectionTypeOptions.Mixed,
            _ => null,
        };
    }
}
