namespace Mpv.Maui.Controls
{
    public class VideoInfo
    {
        public required string DisplayName { get; set; }
        public required VideoSource VideoSource { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
