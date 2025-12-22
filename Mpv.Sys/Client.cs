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

    public event EventHandler<MpvLogMessage>? OnLog;
    public event EventHandler? OnVideoReconfigure;

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

        if (evt.eventId != MpvEventId.LogMessage)
            Console.WriteLine($"EVENT: {evt}");

        switch (evt.eventId)
        {
            case MpvEventId.LogMessage:
            {
                var mpvEvent = Marshal.PtrToStructure<MpvEventLogMessage>(evt.data);
                OnLog?.Invoke(this, MpvEventLogMessageHelper.ToManaged(mpvEvent));
                break;
            }
            case MpvEventId.VideoReconfig:
                OnVideoReconfigure?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    public void Initialize()
    {
        var error = MpvClientInternal.RequestLogMessages(_handle, "debug");
        if (error != 0)
            throw new Exception(
                "MpvClient failed to request log messages: " + ErrorToString(error)
            );
        error = MpvClientInternal.Initialize(_handle);
        if (error != 0)
            throw new Exception("MpvClient failed to initialize: " + ErrorToString(error));
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

    public IntPtr GetPropertyPtr(string property)
    {
        var mpvFormatInt64 = 4;
        var error = MpvClientInternal.GetProperty(
            _handle,
            property,
            mpvFormatInt64,
            out IntPtr output
        );
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

    public MpvRenderContext GetRenderContext(MpvOpenGlInitParmsInner.GetProc getProc)
    {
        return new(this._handle, "opengl", getProc);
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
            // return NativeLibrary.Load(homeBrewPath);
            // return NativeLibrary.Load(homeBrewPath);
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

        if (_handle == IntPtr.Zero)
        {
            return;
        }

        var handleToFree = _handle;
        _handle = IntPtr.Zero;

        if (disposing)
        {
            MpvClientInternal.Free(handleToFree);
            return;
        }

        try
        {
            MpvClientInternal.Free(handleToFree);
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

    public IntPtr GetHandle()
    {
        return _handle;
    }

    public string ErrorToStringPublic(int error)
    {
        return ErrorToString(error);
    }
}
