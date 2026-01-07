namespace Player.ViewModels;

public sealed class TrackSelectedEventArgs(int trackId) : EventArgs
{
    public int TrackId { get; } = trackId;
}
