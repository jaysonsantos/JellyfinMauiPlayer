namespace Mpv.Maui.Controls;

public sealed class TrackChangeEventArgs(int trackId) : EventArgs
{
    public int TrackId { get; } = trackId;
}
