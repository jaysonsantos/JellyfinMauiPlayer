using JellyfinPlayer.Lib.Services;
using JellyfinPlayer.Lib.Storage;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;

namespace JellyfinPlayer.Lib.Api;

public sealed class JellyfinApiClientFactory(
    IHttpClientFactory httpClientFactory,
    ISecureStorageService secureStorage,
    DeviceInfoService deviceInfoService,
    ILogger<JellyfinApiClientFactory> logger
)
{
    private const string ServerUrlKey = "jellyfin_server_url";
    private const string AccessTokenKey = "jellyfin_access_token";

    private JellyfinApiClient? _cachedClient;
    private string? _cachedServerUrl;
    private string? _cachedAccessToken;

    private sealed record StoredConnection(string ServerUrl, string AccessToken);

    public async Task<JellyfinApiClient?> CreateClientAsync(
        CancellationToken cancellationToken = default
    )
    {
        var connection = await GetStoredConnectionAsync(cancellationToken).ConfigureAwait(false);
        LogCreateClientRequest(connection);

        if (connection is null)
        {
            logger.LogWarning("Cannot create API client: Missing server URL or access token");
            return null;
        }

        if (TryUseCachedClient(connection, out var cachedClient))
        {
            return cachedClient;
        }

        var client = await BuildClientAsync(connection, cancellationToken).ConfigureAwait(false);
        CacheClient(connection, client);
        return client;
    }

    public void InvalidateCache()
    {
        logger.LogInformation("API client cache invalidated");
        _cachedClient = null;
        _cachedServerUrl = null;
        _cachedAccessToken = null;
    }

    private async Task<StoredConnection?> GetStoredConnectionAsync(
        CancellationToken cancellationToken
    )
    {
        var serverUrl = await secureStorage
            .GetAsync(ServerUrlKey, cancellationToken)
            .ConfigureAwait(false);
        var accessToken = await secureStorage
            .GetAsync(AccessTokenKey, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return new StoredConnection(serverUrl, accessToken);
    }

    private void LogCreateClientRequest(StoredConnection? connection)
    {
        var serverUrl = connection?.ServerUrl ?? "null";
        var token = connection?.AccessToken;

        logger.LogDebug(
            "CreateClientAsync called - ServerUrl: {ServerUrl}, HasToken: {HasToken}, TokenLength: {TokenLength}, CachedClient: {CachedClient}, CachedToken: {CachedToken}",
            serverUrl,
            !string.IsNullOrWhiteSpace(token),
            token?.Length ?? 0,
            _cachedClient is not null,
            !string.IsNullOrWhiteSpace(_cachedAccessToken)
        );
    }

    private bool TryUseCachedClient(StoredConnection connection, out JellyfinApiClient? client)
    {
        if (
            _cachedClient is not null
            && string.Equals(_cachedServerUrl, connection.ServerUrl, StringComparison.Ordinal)
            && string.Equals(_cachedAccessToken, connection.AccessToken, StringComparison.Ordinal)
        )
        {
            client = _cachedClient;
            return true;
        }

        client = null;
        return false;
    }

    private async Task<JellyfinApiClient> BuildClientAsync(
        StoredConnection connection,
        CancellationToken cancellationToken
    )
    {
        var httpClient = httpClientFactory.CreateClient("Jellyfin");
        httpClient.BaseAddress = new Uri(connection.ServerUrl.TrimEnd('/'));

        var deviceInfo = await deviceInfoService
            .GetDeviceInfoAsync(cancellationToken)
            .ConfigureAwait(false);
        var authProvider = new JellyfinAuthenticationProvider(deviceInfo, connection.AccessToken);

        logger.LogDebug(
            "Creating new API client with token (first 10 chars): {TokenPreview}",
            GetTokenPreview(connection.AccessToken)
        );

        JsonSerializationWriterFactory serializationWriterFactory = new();
        JsonParseNodeFactory parseNodeFactory = new();

        var requestAdapter = new HttpClientRequestAdapter(
            authProvider,
            parseNodeFactory,
            serializationWriterFactory,
            httpClient
        );

        return new JellyfinApiClient(requestAdapter);
    }

    private void CacheClient(StoredConnection connection, JellyfinApiClient client)
    {
        _cachedClient = client;
        _cachedServerUrl = connection.ServerUrl;
        _cachedAccessToken = connection.AccessToken;
    }

    private static string GetTokenPreview(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "null";
        }

        return token.Length > 10 ? string.Concat(token.AsSpan(0, 10), "...") : token;
    }
}
