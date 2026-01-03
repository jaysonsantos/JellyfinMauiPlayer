namespace Mpv.Maui.Controls
{
    public class VideoPositionEventArgs : EventArgs
    {
        public TimeSpan? Position { get; }

        public VideoPositionEventArgs(TimeSpan? position = null)
        {
            Position = position;
        }
    }
}
