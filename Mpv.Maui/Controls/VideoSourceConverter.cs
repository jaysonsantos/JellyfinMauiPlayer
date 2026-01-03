using System.ComponentModel;
using System.Diagnostics;

namespace Mpv.Maui.Controls
{
    public class VideoSourceConverter : TypeConverter, IExtendedTypeConverter
    {
        object IExtendedTypeConverter.ConvertFromInvariantString(
            string value,
            IServiceProvider serviceProvider
        )
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Uri? uri;
                VideoSource result;

                if (Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme != "file")
                {
                    result = VideoSource.FromUri(value);
                    Debug.WriteLine(
                        $"[VideoSourceConverter] Created UriVideoSource from: '{value}'"
                    );
                }
                else
                {
                    result = VideoSource.FromResource(value);
                    Debug.WriteLine(
                        $"[VideoSourceConverter] Created ResourceVideoSource from: '{value}'"
                    );
                }

                return result;
            }
            throw new InvalidOperationException(
                "Cannot convert null or whitespace to VideoSource."
            );
        }
    }
}
