namespace JellyfinPlayer.Lib.Models;

public sealed record MediaItem(
    string Id,
    string Name,
    string? Overview,
    DateTime? ReleaseDate,
    string? ImageUrl
)
{
    public required string Type { get; init; }
    public required int? RuntimeMinutes { get; init; }
    public IReadOnlyList<string> Genres { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public decimal? CommunityRating { get; init; }
    public DateTime? DateCreated { get; init; }
    public string? ProductionYear { get; init; }
    public string? OfficialRating { get; init; }
    public bool IsFolder { get; init; }
    public string? CollectionType { get; init; }
    public int? ChildCount { get; init; }
    public long? RunTimeTicks { get; init; }
    public string? SeriesName { get; init; }
    public int? IndexNumber { get; init; }
    public int? ParentIndexNumber { get; init; }
}

public sealed record QueryResult<T>(IReadOnlyList<T> Items, int TotalRecordCount, int StartIndex)
{
    public static QueryResult<T> Empty { get; } = new([], 0, 0);

    public bool HasMore => StartIndex + Items.Count < TotalRecordCount;
    public int NextStartIndex => StartIndex + Items.Count;
}
