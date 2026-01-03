using Microsoft.Kiota.Abstractions.Authentication;

namespace JellyfinPlayer.Lib.Api;

/// <summary>
/// Authentication provider for Jellyfin API that includes device information in the Authorization header.
/// </summary>
public sealed class JellyfinAuthenticationProvider(
    IDictionary<string, string> deviceInfo,
    string? accessToken = null
) : IAuthenticationProvider
{
    private readonly IDictionary<string, string> _deviceInfo =
        deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
    private string? _accessToken = accessToken;

    public string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default
    )
    {
        // Always build the Authorization header with device info
        var authParts = new List<string>
        {
            $"Client=\"{_deviceInfo["Client"]}\"",
            $"Device=\"{_deviceInfo["Device"]}\"",
            $"DeviceId=\"{_deviceInfo["DeviceId"]}\"",
            $"Version=\"{_deviceInfo["Version"]}\"",
        };

        // Append Token if available
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            authParts.Add($"Token=\"{_accessToken}\"");
        }

        var authHeader = $"MediaBrowser {string.Join(", ", authParts)}";

        // Log the auth header for debugging (mask the token)
        var maskedHeader =
            _accessToken != null && _accessToken.Length > 10
                ? authHeader.Replace(_accessToken, _accessToken[..10] + "...")
                : authHeader;
        Console.WriteLine($"[AUTH] Setting Authorization header: {maskedHeader}");

        // Set or update the Authorization header
        if (request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Remove("Authorization");
        }
        request.Headers.Add("Authorization", authHeader);

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
