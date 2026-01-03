using System.Globalization;

namespace Player.Converters;

public sealed class PlayPauseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPlaying)
            return isPlaying ? Pause : Play;
        return Play;
    }

    private static string Pause => "⏸️";
    private static string Play => "▶️";

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException();
    }
}
