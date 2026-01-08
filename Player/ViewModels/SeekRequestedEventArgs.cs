namespace Player.ViewModels;

public sealed class SeekRequestedEventArgs(TimeSpan position) : EventArgs
{
    public TimeSpan Position { get; } = position;
}
