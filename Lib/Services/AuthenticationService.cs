using System.Text;
using System.Text.Json;
using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Storage;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;

namespace JellyfinPlayer.Lib.Services;

public sealed class AuthenticationService(
    IHttpClientFactory httpClientFactory,
    ISecureStorageService secureStorage,
    DeviceInfoService deviceInfoService,
    JellyfinApiClientFactory apiClientFactory,
    ILogger<AuthenticationService> logger
)
{
    private const string AccessTokenKey = "jellyfin_access_token";
    private const string ServerUrlKey = "jellyfin_server_url";
    private const string UserIdKey = "jellyfin_user_id";

    public async Task<UserAuthenticationResult?> AuthenticateAsync(
        string serverUrl,
        string username,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var deviceInfo = await deviceInfoService
            .GetDeviceInfoAsync(cancellationToken)
            .ConfigureAwait(false);
        var requestAdapter = CreateRequestAdapter(serverUrl, deviceInfo);
        var response = await SendAuthenticationRequestAsync(
                requestAdapter,
                deviceInfo,
                username,
                password,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (response?.AccessToken is null || response.User?.Id is null)
        {
            logger.LogWarning("Authentication failed: Invalid response from server");
            return null;
        }

        var authResult = CreateAuthResult(response, username);

        LogCredentialStorage(authResult, serverUrl);
        await StoreCredentialsAsync(authResult, serverUrl, cancellationToken).ConfigureAwait(false);

        apiClientFactory.InvalidateCache();

        logger.LogInformation(
            "Successfully authenticated user {Username} to server {ServerUrl}",
            username,
            serverUrl
        );
        return authResult;
    }

    private HttpClientRequestAdapter CreateRequestAdapter(
        string serverUrl,
        IDictionary<string, string> deviceInfo
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        ArgumentNullException.ThrowIfNull(deviceInfo);

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(serverUrl.TrimEnd('/'));

        JsonSerializationWriterFactory serializationWriterFactory = new();
        JsonParseNodeFactory parseNodeFactory = new();
        var authProvider = new JellyfinAuthenticationProvider(deviceInfo);

        return new HttpClientRequestAdapter(
            authProvider,
            parseNodeFactory,
            serializationWriterFactory,
            httpClient
        );
    }

    private async Task<AuthenticationResult?> SendAuthenticationRequestAsync(
        HttpClientRequestAdapter requestAdapter,
        IDictionary<string, string> deviceInfo,
        string username,
        string password,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(requestAdapter);
        ArgumentNullException.ThrowIfNull(deviceInfo);

        var apiClient = new JellyfinApiClient(requestAdapter);
        var authRequestJson = JsonSerializer.Serialize(
            new
            {
                Username = username,
                Pw = password,
                App = deviceInfo["Client"],
            }
        );

        var requestInfo = apiClient.Users.AuthenticateByName.ToPostRequestInformation(
            new AuthenticateUserByName { Username = username, Pw = password }
        );

        requestInfo.SetStreamContent(
            new MemoryStream(Encoding.UTF8.GetBytes(authRequestJson)),
            "application/json"
        );

        return await requestAdapter
            .SendAsync(
                requestInfo,
                AuthenticationResult.CreateFromDiscriminatorValue,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private static UserAuthenticationResult CreateAuthResult(
        AuthenticationResult response,
        string username
    )
    {
        var accessToken = response.AccessToken ?? string.Empty;
        var userIdString = response.User?.Id ?? string.Empty;
        var serverId = response.ServerId ?? string.Empty;

        return new UserAuthenticationResult(
            AccessToken: accessToken,
            UserId: userIdString,
            ServerId: serverId,
            ServerName: "Jellyfin Server"
        )
        {
            Username = username,
        };
    }

    private void LogCredentialStorage(UserAuthenticationResult authResult, string serverUrl)
    {
        var tokenPreview =
            authResult.AccessToken.Length > 10
                ? string.Concat(authResult.AccessToken.AsSpan(0, 10), "...")
                : authResult.AccessToken;

        logger.LogInformation(
            "Storing authentication credentials - Token (first 10): {TokenPreview}, Server: {Server}",
            string.IsNullOrEmpty(tokenPreview) ? "null" : tokenPreview,
            serverUrl
        );
    }

    private async Task StoreCredentialsAsync(
        UserAuthenticationResult authResult,
        string serverUrl,
        CancellationToken cancellationToken
    )
    {
        await secureStorage
            .SetAsync(AccessTokenKey, authResult.AccessToken, cancellationToken)
            .ConfigureAwait(false);
        await secureStorage
            .SetAsync(ServerUrlKey, serverUrl, cancellationToken)
            .ConfigureAwait(false);
        await secureStorage
            .SetAsync(UserIdKey, authResult.UserId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<UserAuthenticationResult?> GetStoredAuthenticationAsync(
        CancellationToken cancellationToken = default
    )
    {
        var accessToken = await secureStorage
            .GetAsync(AccessTokenKey, cancellationToken)
            .ConfigureAwait(false);
        var serverUrl = await secureStorage
            .GetAsync(ServerUrlKey, cancellationToken)
            .ConfigureAwait(false);
        var userId = await secureStorage
            .GetAsync(UserIdKey, cancellationToken)
            .ConfigureAwait(false);

        if (
            string.IsNullOrWhiteSpace(accessToken)
            || string.IsNullOrWhiteSpace(serverUrl)
            || string.IsNullOrWhiteSpace(userId)
        )
        {
            return null;
        }

        return new UserAuthenticationResult(
            AccessToken: accessToken,
            UserId: userId,
            ServerId: string.Empty,
            ServerName: "Jellyfin Server"
        )
        {
            Username = "Stored User",
        };
    }

    public async Task<bool> ValidateStoredCredentialsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var stored = await GetStoredAuthenticationAsync(cancellationToken)
                .ConfigureAwait(false);
            if (stored is null)
            {
                logger.LogDebug("No stored credentials to validate");
                return false;
            }

            var apiClient = await TryCreateApiClientAsync(cancellationToken).ConfigureAwait(false);
            if (apiClient is null)
            {
                logger.LogWarning("Cannot validate credentials: API client creation failed");
                return false;
            }

            if (!TryGetStoredUserId(stored, out var userId))
            {
                logger.LogWarning("Cannot validate credentials: User ID not found");
                return false;
            }

            return await ValidateTokenWithServerAsync(apiClient, userId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
            when (string.Equals(ex.GetType().Name, "ApiException", StringComparison.Ordinal)
                && ex.Message.Contains("401", StringComparison.Ordinal)
            )
        {
            logger.LogWarning("Stored credentials are invalid (401 Unauthorized), clearing...");
            await LogoutAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate stored credentials");
            return false;
        }
    }

    private async Task<JellyfinApiClient?> TryCreateApiClientAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await apiClientFactory
                .CreateClientAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create API client for credential validation");
            return null;
        }
    }

    private bool TryGetStoredUserId(UserAuthenticationResult stored, out string userId)
    {
        ArgumentNullException.ThrowIfNull(stored);

        userId = stored.UserId;
        return !string.IsNullOrWhiteSpace(userId);
    }

    private async Task<bool> ValidateTokenWithServerAsync(
        JellyfinApiClient apiClient,
        string userId,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        logger.LogInformation("Validating stored credentials with server...");
        var result = await apiClient
            .UserViews.GetAsync(
                config =>
                {
                    config.QueryParameters.UserId = userId;
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (result is not null)
        {
            logger.LogInformation("Stored credentials are valid");
            return true;
        }

        logger.LogWarning("Stored credentials validation returned null");
        return false;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await secureStorage.RemoveAsync(AccessTokenKey, cancellationToken).ConfigureAwait(false);
        await secureStorage.RemoveAsync(ServerUrlKey, cancellationToken).ConfigureAwait(false);
        await secureStorage.RemoveAsync(UserIdKey, cancellationToken).ConfigureAwait(false);

        // Invalidate cached API client since credentials are cleared
        apiClientFactory.InvalidateCache();

        logger.LogInformation("User logged out");
    }
}
