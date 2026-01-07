namespace Player.Models;

/// <summary>
/// Represents information about a media track (audio or subtitle).
/// Wrapper around Mpv.Sys.TrackInfo for use in the UI layer.
/// </summary>
public sealed class TrackInfo
{
    /// <summary>
    /// Track ID as used in aid/sid properties.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Track type: "audio" or "sub".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Language code (e.g., "eng", "jpn") or null if not available.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Track title or null if not available.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Codec name (e.g., "h264", "aac") or null if not available.
    /// </summary>
    public string? Codec { get; init; }

    /// <summary>
    /// True if this track has the default flag set.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// True if this track has the forced flag set.
    /// </summary>
    public bool IsForced { get; init; }

    /// <summary>
    /// Gets a display name for the track combining language, title, and codec.
    /// </summary>
    public string DisplayName
    {
        get
        {
            List<string> parts = [];

            // Add language if available
            if (!string.IsNullOrWhiteSpace(Language))
            {
                parts.Add(Language.ToUpperInvariant());
            }

            // Add title if available and different from language
            if (
                !string.IsNullOrWhiteSpace(Title)
                && !string.Equals(Title, Language, StringComparison.OrdinalIgnoreCase)
            )
            {
                parts.Add(Title);
            }

            // Add codec if available
            if (!string.IsNullOrWhiteSpace(Codec))
            {
                parts.Add($"({Codec})");
            }

            // Add default/forced indicators
            if (IsDefault)
            {
                parts.Add("[Default]");
            }
            if (IsForced)
            {
                parts.Add("[Forced]");
            }

            // If we have any parts, join them; otherwise return a generic name
            return parts.Count > 0 ? string.Join(" ", parts) : $"Track {Id}";
        }
    }

    /// <summary>
    /// Creates a TrackInfo from an Mpv.Sys.TrackInfo.
    /// </summary>
    public static TrackInfo FromMpvTrackInfo(Mpv.Sys.TrackInfo mpvTrack)
    {
        return new TrackInfo
        {
            Id = mpvTrack.Id,
            Type = mpvTrack.Type,
            Language = mpvTrack.Language,
            Title = mpvTrack.Title,
            Codec = mpvTrack.Codec,
            IsDefault = mpvTrack.IsDefault,
            IsForced = mpvTrack.IsForced,
        };
    }
}
