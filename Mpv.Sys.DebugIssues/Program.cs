using System.Runtime.InteropServices;
using Mpv.Sys;

var c = Task.Run(() =>
{
    var client = new MpvClient();
    Console.WriteLine($"Client hash {client.GetHashCode()}");
    client.Dispose();
    client.Dispose();
});
var cd = Task.Run(() =>
{
    var client = new MpvClient();
    Console.WriteLine($"Client hash {client.GetHashCode()}");
    client.Dispose();
    client.Dispose();
});

var r = Task.Run(() =>
{
    Console.WriteLine($"MpvVersion {MpvClient.Version()}");
});

Task.WaitAll(c, cd, r);

internal static partial class Gl
{
    [LibraryImport(
        "/System/Library/Frameworks/OpenGL.framework/OpenGL",
        EntryPoint = "glGetString",
        StringMarshalling = StringMarshalling.Utf8
    )]
    public static partial IntPtr GetString(uint name);
}
