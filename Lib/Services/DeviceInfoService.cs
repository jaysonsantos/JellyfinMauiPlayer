using System.Reflection;
using JellyfinPlayer.Lib.Storage;

namespace JellyfinPlayer.Lib.Services;

/// <summary>
/// Service for managing device information for Jellyfin authentication.
/// </summary>
public sealed class DeviceInfoService(
    ISecureStorageService secureStorage,
    ILogger<DeviceInfoService> logger
)
{
    private const string DeviceIdKey = "jellyfin_device_id";
    private const string ClientName = "Jellyfin Player";
    private const string DeviceName = "Jellyfin Player";

    /// <summary>
    /// Gets or generates a persistent device ID.
    /// </summary>
    private async Task<string> GetOrCreateDeviceIdAsync(
        CancellationToken cancellationToken = default
    )
    {
        var existingDeviceId = await secureStorage
            .GetAsync(DeviceIdKey, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existingDeviceId))
        {
            return existingDeviceId;
        }

        // Generate a new DeviceId based on device-specific information
        // Using a combination of device info and a GUID for uniqueness
        var deviceInfo =
            $"{Environment.MachineName}_{Environment.UserName}_{Assembly.GetExecutingAssembly().GetName().Name}";
        var deviceIdBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(deviceInfo)
        );
        var deviceId = Convert
            .ToBase64String(deviceIdBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, Math.Min(32, deviceIdBytes.Length * 2));

        await secureStorage
            .SetAsync(DeviceIdKey, deviceId, cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation("Generated new device ID: {DeviceId}", deviceId);
        return deviceId;
    }

    /// <summary>
    /// Gets the application version from the assembly.
    /// </summary>
    private string GetApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0"; // Format as Major.Minor.Build
    }

    /// <summary>
    /// Gets the client name.
    /// </summary>
    private string GetClientName() => ClientName;

    /// <summary>
    /// Gets the device name.
    /// </summary>
    private string GetDeviceName() => DeviceName;

    /// <summary>
    /// Gets all device info as a dictionary.
    /// </summary>
    public async Task<IDictionary<string, string>> GetDeviceInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        var deviceId = await GetOrCreateDeviceIdAsync(cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Client"] = GetClientName(),
            ["Device"] = GetDeviceName(),
            ["DeviceId"] = deviceId,
            ["Version"] = GetApplicationVersion(),
        };
    }
}
