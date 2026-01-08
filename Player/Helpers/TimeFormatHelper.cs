using System.Globalization;

namespace Player.Helpers;

public static class TimeFormatHelper
{
    /// <summary>
    /// Formats a TimeSpan for display as MM:SS or H:MM:SS depending on duration
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format</param>
    /// <returns>Formatted time string</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return timeSpan.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }
        return timeSpan.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
}
