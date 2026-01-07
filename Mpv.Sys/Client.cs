using System.Reflection;
using System.Runtime.InteropServices;

namespace Mpv.Sys;

using Internal;

public sealed class MpvClient : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;
    private static bool s_dllImporterResolverSet;
    private static readonly Mutex ResolverInitMutex = new(false, "MpvClientInternal");
    private bool _initialized;

    public event EventHandler<MpvLogMessage>? OnLog;
    public event EventHandler<MpvPropertyChangeEventArgs>? OnPropertyChange;
    public event EventHandler? OnVideoReconfigure;
    public event EventHandler? OnFileLoaded;

    public MpvClient()
    {
        EnsureDllImporterResolver();

        _handle = MpvClientInternal.Create();
        if (_handle == IntPtr.Zero)
            throw new Exception("MpvClient failed to initialize");

        Task.Run(() =>
        {
            while (!_disposed)
            {
                PollEvent();
            }
        });

        // Initialize();
    }

    private void PollEvent()
    {
        var evt = WaitEvent();

        switch (evt.eventId)
        {
            case MpvEventId.LogMessage:
            {
                var mpvEvent = Marshal.PtrToStructure<MpvEventLogMessage>(evt.data);
                OnLog?.Invoke(this, MpvEventLogMessageHelper.ToManaged(mpvEvent));
                break;
            }
            case MpvEventId.PropertyChange:
            {
                var mpvEvent = Marshal.PtrToStructure<MpvEventProperty>(evt.data);
                object? output;
                try
                {
                    output = ReadEventOutput(mpvEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read event data: {ex}");
                    return;
                }

                OnPropertyChange?.Invoke(
                    this,
                    new MpvPropertyChangeEventArgs
                    {
                        Property = (ObservedProperty)evt.replyUserData,
                        EventData = output,
                    }
                );
                break;
            }
            case MpvEventId.VideoReconfig:
                OnVideoReconfigure?.Invoke(this, EventArgs.Empty);
                break;
            case MpvEventId.FileLoaded:
                OnFileLoaded?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private static object? ReadEventOutput(MpvEventProperty mpvEvent)
    {
        switch (mpvEvent.Format)
        {
            case MpvFormat.None:
                return null;
            case MpvFormat.String:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.String is not available."
                );
            case MpvFormat.OsdString:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.OsdString is not available."
                );
            case MpvFormat.Flag:
                return Marshal.ReadIntPtr(mpvEvent.Data) == 1;
            case MpvFormat.Int64:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.Int64 is not available."
                );
            case MpvFormat.Double:
                var bytes = BitConverter.GetBytes(Marshal.ReadIntPtr(mpvEvent.Data));
                return BitConverter.ToDouble(bytes, 0);
            case MpvFormat.Node:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.Node is not available."
                );
            case MpvFormat.NodeArray:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.NodeArray is not available."
                );
            case MpvFormat.NodeMap:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.NodeMap is not available."
                );
            case MpvFormat.ByteArray:
                throw new NotImplementedException(
                    "Implementation for MpvFormat.ByteArray is not available."
                );
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Initialize()
    {
        if (_initialized)
            return;
        var error = MpvClientInternal.RequestLogMessages(_handle, "debug");
        if (error != 0)
            throw new Exception(
                "MpvClient failed to request log messages: " + ErrorToString(error)
            );
        error = MpvClientInternal.Initialize(_handle);
        if (error != 0)
            throw new Exception("MpvClient failed to initialize: " + ErrorToString(error));
        _initialized = true;
    }

    public void SetOption(string key, string value)
    {
        var error = MpvClientInternal.SetOptionString(_handle, key, value);
        if (error != 0)
            throw new Exception($"Failed to set option {key} -> {value}: " + ErrorToString(error));
    }

    public void SetOption(string key, int format, IntPtr data)
    {
        var error = MpvClientInternal.SetOption(_handle, key, format, data);
        if (error != 0)
            throw new Exception("Failed to set option: " + ErrorToString(error));
    }

    public IntPtr GetPropertyPtr(string property, MpvFormat format = MpvFormat.Int64)
    {
        var error = MpvClientInternal.GetProperty(_handle, property, format, out IntPtr output);
        if (error != 0)
            throw new Exception($"Failed to get property: {property} " + ErrorToString(error));

        return output;
    }

    public void Command(string command, params string[] parameters)
    {
        // command in the beginning and null at the end
        string?[] outputParameters = new string?[parameters.Length + 2];
        outputParameters[0] = command;
        parameters.CopyTo(outputParameters, 1);
        outputParameters[^1] = null;

        var error = MpvClientInternal.Command(_handle, outputParameters);
        if (error != 0)
            throw new Exception("Failed to execute command: " + ErrorToString(error));
    }

    /// <summary>
    /// Sets the audio track by track ID.
    /// </summary>
    /// <param name="trackId">The audio track ID to switch to.</param>
    public void SetAudioTrack(int trackId)
    {
        Command("set", "aid", trackId.ToString());
    }

    /// <summary>
    /// Sets the subtitle track by track ID. Use 0 to disable subtitles.
    /// </summary>
    /// <param name="trackId">The subtitle track ID to switch to, or 0 to disable.</param>
    public void SetSubtitleTrack(int trackId)
    {
        Command("set", "sid", trackId.ToString());
    }

    /// <summary>
    /// Gets the current audio track ID.
    /// </summary>
    /// <returns>The current audio track ID, or 0 if no audio track is selected.</returns>
    public int GetCurrentAudioTrack()
    {
        return GetTrackIdProperty("aid");
    }

    /// <summary>
    /// Gets the current subtitle track ID.
    /// </summary>
    /// <returns>The current subtitle track ID, or 0 if subtitles are disabled.</returns>
    public int GetCurrentSubtitleTrack()
    {
        return GetTrackIdProperty("sid");
    }

    /// <summary>
    /// Gets the language of the current audio track.
    /// </summary>
    /// <returns>The language code (e.g., "eng", "jpn") or null if not available.</returns>
    public string? GetCurrentAudioTrackLanguage()
    {
        int trackId = GetCurrentAudioTrack();
        if (trackId == 0)
            return null;

        return GetTrackLanguage(trackId, "audio");
    }

    /// <summary>
    /// Gets the language of the current subtitle track.
    /// </summary>
    /// <returns>The language code (e.g., "eng", "jpn") or null if not available.</returns>
    public string? GetCurrentSubtitleTrackLanguage()
    {
        int trackId = GetCurrentSubtitleTrack();
        if (trackId == 0)
            return null;

        return GetTrackLanguage(trackId, "sub");
    }

    /// <summary>
    /// Gets the language for a specific track by ID and type.
    /// Uses the track-list property to find the track and retrieve its language.
    /// See: https://mpv.io/manual/stable/#command-interface-track-list
    /// </summary>
    /// <param name="trackId">The track ID (as used in aid/sid).</param>
    /// <param name="trackType">The track type ("audio" or "sub").</param>
    /// <returns>The language code or null if not found or not available.</returns>
    private string? GetTrackLanguage(int trackId, string trackType)
    {
        // Get the track list count
        IntPtr countPtr = GetPropertyPtr("track-list/count", MpvFormat.Int64);
        int trackCount = (int)(long)countPtr;

        // Iterate through all tracks to find the one matching the ID and type
        for (int i = 0; i < trackCount; i++)
        {
            // Check if this track matches our type
            string typeProperty = $"track-list/{i}/type";
            IntPtr typePtr = GetPropertyPtr(typeProperty, MpvFormat.String);
            try
            {
                string? type = Marshal.PtrToStringAnsi(typePtr);
                if (!string.Equals(type, trackType, StringComparison.Ordinal))
                {
                    continue;
                }
            }
            finally
            {
                MpvClientInternal.Free(typePtr);
            }

            // Check if this track's ID matches
            string idProperty = $"track-list/{i}/id";
            IntPtr idPtr = GetPropertyPtr(idProperty, MpvFormat.Int64);
            int id = (int)(long)idPtr;

            if (id == trackId)
            {
                // Found the matching track, get its language
                string langProperty = $"track-list/{i}/lang";
                try
                {
                    IntPtr langPtr = GetPropertyPtr(langProperty, MpvFormat.String);
                    try
                    {
                        return Marshal.PtrToStringAnsi(langPtr);
                    }
                    finally
                    {
                        MpvClientInternal.Free(langPtr);
                    }
                }
                catch
                {
                    // Language property might not be available for this track
                    return null;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Helper method to get a track ID property (aid/sid) from MPV.
    /// These properties return strings that can be "auto", "no", or numeric values.
    /// See: https://mpv.io/manual/stable/#options (--aid, --sid)
    /// </summary>
    /// <param name="propertyName">The name of the MPV property to retrieve (aid or sid).</param>
    /// <returns>The property value as an integer, or 0 if the track is disabled or not set.</returns>
    private int GetTrackIdProperty(string propertyName)
    {
        IntPtr ptr = GetPropertyPtr(propertyName, MpvFormat.String);
        try
        {
            string? value = Marshal.PtrToStringAnsi(ptr);
            // MPV returns "no", "auto", or a numeric string
            // Return 0 for "no" or "auto", otherwise parse the numeric value
            if (
                string.IsNullOrEmpty(value)
                || string.Equals(value, "no", StringComparison.Ordinal)
                || string.Equals(value, "auto", StringComparison.Ordinal)
            )
            {
                return 0;
            }

            if (int.TryParse(value, out int trackId))
            {
                return trackId;
            }

            return 0;
        }
        finally
        {
            // For string format, MPV allocates memory that must be freed
            MpvClientInternal.Free(ptr);
        }
    }

    /// <summary>
    /// Helper method to get an Int64 property from MPV.
    /// For MpvFormat.Int64, MPV returns the value directly in the IntPtr (not as allocated memory).
    /// See: https://mpv.io/manual/stable/#libmpv - mpv_get_property returns simple types by value.
    /// </summary>
    /// <param name="propertyName">The name of the MPV property to retrieve.</param>
    /// <returns>The property value as an integer.</returns>
    private int GetInt64Property(string propertyName)
    {
        IntPtr ptr = GetPropertyPtr(propertyName, MpvFormat.Int64);
        // The value is stored in the pointer itself, cast directly without dereferencing
        return (int)(long)ptr;
    }

    /// <summary>
    /// Sets subtitle visibility by disabling subtitles (setting sid to 0) when visible is false.
    /// Note: In MPV, subtitle visibility is controlled by the sid property. To make subtitles
    /// visible, use SetSubtitleTrack() with a valid track ID. This method is provided as a
    /// convenience for disabling subtitles.
    /// </summary>
    /// <param name="visible">False to hide subtitles. True has no effect as subtitle visibility
    /// requires selecting a specific track via SetSubtitleTrack().</param>
    public void SetSubtitleVisibility(bool visible)
    {
        if (!visible)
        {
            // Disable subtitles by setting sid to 0
            SetSubtitleTrack(0);
        }
        // When visible is true, no action is taken because MPV requires
        // a specific track ID to enable subtitles. Use SetSubtitleTrack() instead.
    }

    private MpvEvent WaitEvent()
    {
        var evt = MpvClientInternal.WaitEvent(_handle, 1000.0);
        return Marshal.PtrToStructure<MpvEvent>(evt);
    }

    private static void EnsureDllImporterResolver()
    {
        ResolverInitMutex.WaitOne();
        if (s_dllImporterResolverSet)
        {
            ResolverInitMutex.ReleaseMutex();
            return;
        }

        try
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
            s_dllImporterResolverSet = true;
        }
        finally
        {
            ResolverInitMutex.ReleaseMutex();
        }
    }

    private static IntPtr DllImportResolver(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath
    )
    {
        if (!string.Equals(libraryName, "mpv", StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        if (OperatingSystem.IsWindows())
            return NativeLibrary.Load("mpv-2.dll", assembly, searchPath);

        const string homeBrewPath = "/opt/homebrew/lib/libmpv.dylib";
        if (OperatingSystem.IsMacCatalyst() && File.Exists(homeBrewPath))
        {
            return NativeLibrary.Load("libmpv.2.dylib");
        }
        // Otherwise, fallback to default import resolver.
        return IntPtr.Zero;
    }

    private string ErrorToString(int error)
    {
        return Marshal.PtrToStringAnsi(MpvClientInternal.ErrorToString(error))!;
    }

    ~MpvClient()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _initialized = false;

        if (_handle == IntPtr.Zero)
        {
            return;
        }

        var handleToFree = _handle;
        _handle = IntPtr.Zero;

        if (disposing)
        {
            MpvClientInternal.Destroy(handleToFree);
            return;
        }

        try
        {
            MpvClientInternal.Destroy(handleToFree);
        }
        catch
        {
            // Never throw from a finalizer.
        }
    }

    public static ulong Version()
    {
        EnsureDllImporterResolver();
        return MpvClientInternal.Version();
    }

    public void ObserveProperty(ulong userData, string name, MpvFormat format)
    {
        var error = MpvClientInternal.ObserveProperty(_handle, userData, name, (int)format);
        if (error != 0)
            throw new Exception($"Failed to observe property {name}: " + ErrorToString(error));
    }

    public int UnobserveProperty(ulong userData)
    {
        var error = MpvClientInternal.UnobserveProperty(_handle, userData);
        if (error < 0)
            throw new Exception("Failed to unobserve property: " + ErrorToString(error));

        // Number of properties unobserved
        return error;
    }

    public IntPtr GetHandle()
    {
        return _handle;
    }

    public string ErrorToStringPublic(int error)
    {
        return ErrorToString(error);
    }
}
