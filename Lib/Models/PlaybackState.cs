namespace JellyfinPlayer.Lib.Models;

public sealed record PlaybackState(string ItemId, long PositionTicks, bool IsPaused)
{
    public required string SessionId { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public long? TotalTicks { get; init; }
    public double? PlaybackRate { get; init; }
    public int? VolumeLevel { get; init; }
    public bool IsMuted { get; init; }
}

public sealed record PlaybackInfo(string StreamUrl, string ItemId)
{
    public required string MediaSourceId { get; init; }
    public required string SessionId { get; init; }
    public IReadOnlyList<SubtitleTrack> SubtitleTracks { get; init; } = [];
    public IReadOnlyList<AudioTrack> AudioTracks { get; init; } = [];
    public bool CanSeek { get; init; }
    public long? TotalTicks { get; init; }
}

public sealed record SubtitleTrack(int Index, string? Language, string? DisplayTitle)
{
    public required string Id { get; init; }
    public bool IsDefault { get; init; }
    public bool IsForced { get; init; }
}

public sealed record AudioTrack(int Index, string? Language, string? DisplayTitle)
{
    public required string Id { get; init; }
    public int? Channels { get; init; }
    public int? SampleRate { get; init; }
    public int? BitRate { get; init; }
}
