using System.Runtime.InteropServices;

namespace Mpv.Sys.Internal;

public struct Constants
{
#if WINDOWS
    public const string LibraryName = "mpv2";
#elif IOS
    public const string LibraryName = "__Internal";
#elif MACCATALYST || MACOS
    public const string LibraryName = "mpv";
#elif LINUX || ANDROID
    public const string LibraryName = "libmpv.so";
#else
#error Unsupported platform
#endif
}

[StructLayout(LayoutKind.Sequential)]
public struct MpvParameter
{
    public int type;
    public IntPtr data;
}

public class ArrayPtr<T>
{
    private readonly IntPtr _paramsPtr;

    public ArrayPtr(T[] renderParams)
    {
        // Allocate unmanaged memory for the array
        var paramSize = Marshal.SizeOf<T>();
        _paramsPtr = Marshal.AllocHGlobal(paramSize * renderParams.Length + 1);

        // Copy structs to unmanaged memory
        for (var i = 0; i < renderParams.Length; i++)
        {
            var offset = IntPtr.Add(_paramsPtr, i * paramSize);
            Marshal.StructureToPtr(
                renderParams[i] ?? throw new InvalidOperationException(),
                offset,
                false
            );
        }
    }

    ~ArrayPtr()
    {
        if (_paramsPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(_paramsPtr);
    }

    public IntPtr Get()
    {
        return _paramsPtr;
    }
}

internal static partial class MpvClientInternal
{
    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_client_api_version")]
    internal static partial ulong Version();

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_create")]
    internal static partial IntPtr Create();

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_free")]
    internal static partial void Free(IntPtr ptr);

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_initialize")]
    internal static partial int Initialize(IntPtr ptr);

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_error_string")]
    // [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    internal static partial IntPtr ErrorToString(int code);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_set_option_string",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int SetOptionString(IntPtr ptr, string key, string value);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_set_option",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int SetOption(IntPtr ptr, string key, int format, IntPtr data);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_set_option",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int SetOptionInt64(IntPtr ptr, string key, int format, Int64 data);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_command",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int Command(IntPtr ptr, [In] string?[] args);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_get_property",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int GetProperty(
        IntPtr ptr,
        string property,
        int format,
        out IntPtr value
    );

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_wait_event")]
    internal static partial IntPtr WaitEvent(IntPtr ptr, double timeout);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_event_name",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial string EventName(int eventId);

    [LibraryImport(
        Constants.LibraryName,
        EntryPoint = "mpv_request_log_messages",
        StringMarshalling = StringMarshalling.Utf8
    )]
    internal static partial int RequestLogMessages(IntPtr ptr, string minLevel);
}

internal static partial class MpvRenderInternal
{
    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_render_context_create")]
    internal static partial int Create(out IntPtr contextOutput, IntPtr client, IntPtr parameters);

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_render_context_free")]
    internal static partial void Free(IntPtr context);

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_render_context_render")]
    internal static partial int Render(IntPtr context, IntPtr parameters);

    [LibraryImport(Constants.LibraryName, EntryPoint = "mpv_render_context_set_update_callback")]
    internal static partial void SetUpdateCallback(
        IntPtr context,
        IntPtr callback,
        IntPtr callbackCtx
    );
}

public class MpvRenderContext
{
    private readonly IntPtr _context;
    private readonly IntPtr _initParams;

    public MpvRenderContext(IntPtr client, string apiType, MpvOpenGlInitParmsInner.GetProc getProc)
    {
        MpvOpenGlInitParmsInner mpvOpenGlInitParmsInner = new()
        {
            get_proc_address = getProc,
            get_proc_address_ctx = IntPtr.Zero,
        };
        _initParams = Marshal.AllocHGlobal(Marshal.SizeOf<MpvOpenGlInitParmsInner>());
        Marshal.StructureToPtr(mpvOpenGlInitParmsInner, _initParams, false);

        MpvParameter[] renderParams =
        [
            new() { type = 1, data = Marshal.StringToHGlobalAnsi(apiType) },
            new() { type = 2, data = _initParams },
            new() { type = 0, data = IntPtr.Zero },
        ];

        var parameters = new ArrayPtr<MpvParameter>(renderParams);

        var error = MpvRenderInternal.Create(out _context, client, parameters.Get());
        if (error != 0)
            throw new Exception(
                Marshal.PtrToStringAnsi(MpvClientInternal.ErrorToString(error)) + " " + error
            );
    }

    //
    // public void SetUpdateCallback(IntPtr callback, IntPtr callbackCtx)
    // {
    //     MpvRenderInternal.SetUpdateCallback(_context, callback, callbackCtx);
    // }
    //
    // public void Render()
    // {
    //     var parameters = new ArrayPtr<MpvParameter>([
    //         new MpvParameter
    //         {
    //             type = 0, data = IntPtr.Zero, // Null terminator
    //         },
    //     ]);
    //
    //     var error = MpvRenderInternal.Render(_context, parameters.Get());
    //     if (error != 0)
    //         throw new Exception(
    //             Marshal.PtrToStringAnsi(MpvClientInternal.ErrorToString(error)) + " " + error
    //         );
    // }
    //
    // public void Render(int width, int height, int fbo = 0, bool flipY = true)
    // {
    //     // Create FBO struct as int array
    //     int[] fboStruct = [fbo, width, height, 0]; // fbo, width, height, internal_format
    //     var fboArray = new ArrayPtr<int>(fboStruct);
    //
    //     // Create flipY parameter as int array
    //     int[] flipYArray = [flipY ? 1 : 0];
    //     var flipYPtr = new ArrayPtr<int>(flipYArray);
    //
    //     MpvParameter[] renderParams =
    //     [
    //         new() { type = 3, data = fboArray.Get() },
    //         new() { type = 4, data = flipYPtr.Get() },
    //         new() { type = 0, data = IntPtr.Zero },
    //     ];
    //
    //     var parameters = new ArrayPtr<MpvParameter>(renderParams);
    //
    //     var error = MpvRenderInternal.Render(_context, parameters.Get());
    //     if (error != 0)
    //         throw new Exception(
    //             Marshal.PtrToStringAnsi(MpvClientInternal.ErrorToString(error)) + " " + error
    //         );
    // }

    ~MpvRenderContext()
    {
        if (_context != IntPtr.Zero)
            MpvRenderInternal.Free(_context);

        if (_initParams != IntPtr.Zero)
            Marshal.FreeHGlobal(_initParams);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct MpvOpenGlInitParmsInner
{
    public delegate IntPtr GetProc(IntPtr ctx, string name);

    public GetProc get_proc_address;
    public IntPtr get_proc_address_ctx;
}

[StructLayout(LayoutKind.Sequential)]
public struct MpvOpenGlInitParms
{
    public int type;
    public MpvOpenGlInitParmsInner? data;
}

public static partial class FfmpegLibs
{
    [LibraryImport("libavcodec.so", EntryPoint = "av_jni_set_java_vm")]
    public static partial int SetJavaVm(IntPtr vm, IntPtr reserved);

    [LibraryImport("libavutil.so", EntryPoint = "av_strerror")]
    public static partial int MakeErrorString(int errorNumber, IntPtr errbuf, int size);
}
