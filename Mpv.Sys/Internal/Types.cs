using System.Runtime.InteropServices;

namespace Mpv.Sys.Internal
{
    /// <summary>
    /// The IDs of events dispatched by the mpv library.
    /// </summary>
    public enum MpvEventId
    {
        /// <summary>
        /// Nothing happened. Happens on timeouts or sporadic wakeups.
        /// </summary>
        None = 0,

        /// <summary>
        /// Happens when the player quits. The player enters a state where it tries
        /// to disconnect all clients. Most requests to the player will fail, and
        /// the client should react to this and quit with mpv_destroy() as soon as
        /// possible.
        /// </summary>
        Shutdown = 1,

        /// <summary>
        /// See mpv_request_log_messages().
        /// </summary>
        LogMessage = 2,

        /// <summary>
        /// Reply to a mpv_get_property_async() request.
        /// See also mpv_event and mpv_event_property.
        /// </summary>
        GetPropertyReply = 3,

        /// <summary>
        /// Reply to a mpv_set_property_async() request.
        /// (Unlike GetPropertyReply, mpv_event_property is not used.)
        /// </summary>
        SetPropertyReply = 4,

        /// <summary>
        /// Reply to a mpv_command_async() or mpv_command_node_async() request.
        /// See also mpv_event and mpv_event_command.
        /// </summary>
        CommandReply = 5,

        /// <summary>
        /// Notification before playback start of a file (before the file is loaded).
        /// See also mpv_event and mpv_event_start_file.
        /// </summary>
        StartFile = 6,

        /// <summary>
        /// Notification after playback end (after the file was unloaded).
        /// See also mpv_event and mpv_event_end_file.
        /// </summary>
        EndFile = 7,

        /// <summary>
        /// Notification when the file has been loaded (headers were read etc.), and
        /// decoding starts.
        /// </summary>
        FileLoaded = 8,

        /// <summary>
        /// Idle mode was entered. In this mode, no file is played, and the playback
        /// core waits for new commands.
        /// </summary>
        [System.Obsolete(
            "This is equivalent to using mpv_observe_property() on the \"idle-active\" property. The event is redundant, and might be removed in the far future."
        )]
        Idle = 11,

        /// <summary>
        /// Sent every time after a video frame is displayed.
        /// </summary>
        [System.Obsolete(
            "Use mpv_observe_property() with relevant properties instead (such as \"playback-time\")."
        )]
        Tick = 14,

        /// <summary>
        /// Triggered by the script-message input command. The command uses the
        /// first argument of the command as client name (see mpv_client_name()) to
        /// dispatch the message, and passes along all arguments starting from the
        /// second argument as strings.
        /// See also mpv_event and mpv_event_client_message.
        /// </summary>
        ClientMessage = 16,

        /// <summary>
        /// Happens after video changed in some way. This can happen on resolution
        /// changes, pixel format changes, or video filter changes. Applications
        /// embedding a mpv window should listen to this event in order to resize
        /// the window if needed.
        /// </summary>
        VideoReconfig = 17,

        /// <summary>
        /// Similar to VideoReconfig. This is relatively uninteresting,
        /// because there is no such thing as audio output embedding.
        /// </summary>
        AudioReconfig = 18,

        /// <summary>
        /// Happens when a seek was initiated. Playback stops. Usually it will
        /// resume with PlaybackRestart as soon as the seek is finished.
        /// </summary>
        Seek = 20,

        /// <summary>
        /// There was a discontinuity of some sort (like a seek), and playback
        /// was reinitialized. Usually happens on start of playback and after
        /// seeking. The main purpose is allowing the client to detect when a seek
        /// request is finished.
        /// </summary>
        PlaybackRestart = 21,

        /// <summary>
        /// Event sent due to mpv_observe_property().
        /// See also mpv_event and mpv_event_property.
        /// </summary>
        PropertyChange = 22,

        /// <summary>
        /// Happens if the internal per-mpv_handle ringbuffer overflows, and at
        /// least 1 event had to be dropped.
        /// Event delivery will continue normally once this event was returned.
        /// </summary>
        QueueOverflow = 24,

        /// <summary>
        /// Triggered if a hook handler was registered with mpv_hook_add(), and the
        /// hook is invoked. If you receive this, you must handle it, and continue
        /// the hook with mpv_hook_continue().
        /// See also mpv_event and mpv_event_hook.
        /// </summary>
        Hook = 25,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEvent
    {
        public MpvEventId eventId;
        public int error;
        public UInt64 replyUserData;
        public IntPtr data;

        public override string ToString()
        {
            return $"<MPVEvent: {eventId.ToString()} Error: {error}>";
        }
    }

    /// <summary>
    /// Log levels used by mpv. Values are intentionally spaced
    /// to allow for the addition of intermediate levels in the future.
    /// </summary>
    public enum MpvLogLevel
    {
        /// <summary>
        /// "no" - disable absolutely all messages.
        /// </summary>
        None = 0,

        /// <summary>
        /// "fatal" - critical/aborting errors.
        /// </summary>
        Fatal = 10,

        /// <summary>
        /// "error" - simple errors.
        /// </summary>
        Error = 20,

        /// <summary>
        /// "warn" - possible problems.
        /// </summary>
        Warn = 30,

        /// <summary>
        /// "info" - informational message.
        /// </summary>
        Info = 40,

        /// <summary>
        /// "v" - noisy informational message.
        /// </summary>
        V = 50,

        /// <summary>
        /// "debug" - very noisy technical information.
        /// </summary>
        Debug = 60,

        /// <summary>
        /// "trace" - extremely noisy.
        /// </summary>
        Trace = 70,
    }

    /// <summary>
    /// Data structure for the MPV_EVENT_LOG_MESSAGE event.
    /// This structure is designed for P/Invoke interop with the native C library.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventLogMessage
    {
        /// <summary>
        /// The module prefix, identifies the sender of the message. As a special
        /// case, if the message buffer overflows, this will be set to the string
        /// "overflow". (Corresponds to `const char *prefix`).
        /// </summary>
        public IntPtr prefix;

        /// <summary>
        /// The log level as a string. See mpv_request_log_messages() for possible
        /// values. The level "no" is never used here. (Corresponds to `const char *level`).
        /// </summary>
        public IntPtr level;

        /// <summary>
        /// The log message. It consists of 1 line of text, and is terminated with
        /// a newline character. (Corresponds to `const char *text`).
        /// </summary>
        public IntPtr text;

        /// <summary>
        /// The same contents as the level field, but as a numeric ID.
        /// </summary>
        public MpvLogLevel log_level;
    }

    /// <summary>
    /// Helper methods to convert the unmanaged P/Invoke struct into a managed class.
    /// </summary>
    public static class MpvEventLogMessageHelper
    {
        /// <summary>
        /// Converts the unmanaged interop struct into a managed class for easy use.
        /// It marshals the IntPtr fields to managed C# strings (assuming UTF-8 encoding).
        /// </summary>
        public static MpvLogMessage ToManaged(MpvEventLogMessage unmanaged)
        {
            return new MpvLogMessage
            {
                Prefix = Marshal.PtrToStringUTF8(unmanaged.prefix)!,
                Level = Marshal.PtrToStringUTF8(unmanaged.level)!,
                Text = Marshal.PtrToStringUTF8(unmanaged.text)!,
                LogLevel = unmanaged.log_level,
            };
        }
    }

    /// <summary>
    /// Fully managed log message version for use within C# code.
    /// </summary>
    public class MpvLogMessage
    {
        public required string Prefix { get; set; }
        public required string Level { get; set; }
        public required string Text { get; set; }
        public MpvLogLevel LogLevel { get; set; }
    }

    /// <summary>
    /// Data format for options and properties.
    /// </summary>
    public enum MpvFormat
    {
        None = 0,
        String = 1,
        OsdString = 2,
        Flag = 3,
        Int64 = 4,
        Double = 5,
        Node = 6,
        NodeArray = 7,
        NodeMap = 8,
        ByteArray = 9,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MpvNode
    {
        [FieldOffset(0)]
        public IntPtr Str;

        [FieldOffset(0)]
        public int Flag;

        [FieldOffset(0)]
        public long Int64;

        [FieldOffset(0)]
        public double Double;

        [FieldOffset(0)]
        public IntPtr List;

        [FieldOffset(0)]
        public IntPtr ByteArray;

        [FieldOffset(8)]
        public MpvFormat Format;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvNodeList
    {
        public int Num;
        public IntPtr Values;
        public IntPtr Keys;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvByteArray
    {
        public IntPtr Data;
        public UIntPtr Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventProperty
    {
        public IntPtr Name;
        public MpvFormat Format;
        public IntPtr Data;
    }

    /// <summary>
    /// Constants for mpv property names.
    /// Provides a single source of truth for all property name strings used throughout the application.
    /// </summary>
    public static class MpvPropertyNames
    {
        /// <summary>
        /// Whether the player is currently idle (no file loaded).
        /// Type: Flag (bool)
        /// </summary>
        public const string IdleActive = "idle-active";

        /// <summary>
        /// Whether playback is paused.
        /// Type: Flag (bool)
        /// </summary>
        public const string Pause = "pause";

        /// <summary>
        /// Whether playback is paused due to cache buffering.
        /// Type: Flag (bool)
        /// </summary>
        public const string PausedForCache = "paused-for-cache";

        /// <summary>
        /// Whether the playback core is idle (not actively decoding).
        /// Type: Flag (bool)
        /// </summary>
        public const string CoreIdle = "core-idle";

        /// <summary>
        /// Whether the end of file has been reached.
        /// Type: Flag (bool)
        /// </summary>
        public const string EofReached = "eof-reached";

        /// <summary>
        /// Duration of the current file in seconds.
        /// Type: Double
        /// </summary>
        public const string Duration = "duration";

        /// <summary>
        /// Current playback position in seconds.
        /// Type: Double
        /// </summary>
        public const string TimePos = "time-pos";

        /// <summary>
        /// Whether to loop the current file.
        /// Type: String ("inf" or "no")
        /// </summary>
        public const string LoopFile = "loop-file";

        /// <summary>
        /// Whether to enable input media keys.
        /// Type: String ("yes" or "no")
        /// </summary>
        public const string InputMediaKeys = "input-media-keys";
    }

    /// <summary>
    /// Enum for identifying observed mpv properties.
    /// Used as user data when observing properties.
    /// </summary>
    public enum ObservedProperty : ulong
    {
        None = 0,
        IdleActive = 1,
        Pause = 2,
        PausedForCache = 3,
        CoreIdle = 4,
        EofReached = 5,
        Duration = 6,
        TimePos = 7,
    }

    /// <summary>
    /// Event args for property change events with property identification.
    /// </summary>
    public class MpvPropertyChangeEventArgs : EventArgs
    {
        /// <summary>
        /// The property that changed, identified by the ObservedProperty enum.
        /// </summary>
        public required ObservedProperty Property { get; init; }

        /// <summary>
        /// The raw event data from mpv.
        /// </summary>
        public required object EventData { get; init; }
    }

    public enum MpvEndFileReason
    {
        Eof = 0,
        Stop = 2,
        Quit = 3,
        Error = 4,
        Redirect = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventStartFile
    {
        public long PlaylistEntryId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventEndFile
    {
        public MpvEndFileReason Reason;
        public int Error;
        public long PlaylistEntryId;
        public long PlaylistInsertId;
        public int PlaylistInsertNumEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventClientMessage
    {
        public int NumArgs;
        public IntPtr Args;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventHook
    {
        public IntPtr Name;
        public ulong Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventCommand
    {
        public MpvNode Result;
    }
}
