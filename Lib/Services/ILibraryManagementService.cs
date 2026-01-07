using JellyfinPlayer.Lib.Api.Models;

namespace JellyfinPlayer.Lib.Services;

/// <summary>
/// Service for managing Jellyfin libraries (admin operations).
/// </summary>
public interface ILibraryManagementService
{
    /// <summary>
    /// Gets all virtual folders (libraries) with their paths.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of virtual folders.</returns>
    Task<ServiceResult<IReadOnlyList<VirtualFolderInfo>>> GetLibrariesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a new virtual folder (library).
    /// </summary>
    /// <param name="name">The name of the library.</param>
    /// <param name="collectionType">The type of the collection (e.g., movies, tvshows, music).</param>
    /// <param name="paths">Optional paths to add to the library.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> CreateLibraryAsync(
        string name,
        string? collectionType = null,
        IReadOnlyList<string>? paths = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes a virtual folder (library).
    /// </summary>
    /// <param name="name">The name of the library to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> DeleteLibraryAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Adds a path to an existing library.
    /// </summary>
    /// <param name="libraryName">The name of the library.</param>
    /// <param name="path">The path to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> AddPathAsync(
        string libraryName,
        string path,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes a path from an existing library.
    /// </summary>
    /// <param name="libraryName">The name of the library.</param>
    /// <param name="path">The path to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> RemovePathAsync(
        string libraryName,
        string path,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Starts a library refresh (scan).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating success or failure.</returns>
    Task<ServiceResult<bool>> RefreshLibraryAsync(CancellationToken cancellationToken = default);
}
