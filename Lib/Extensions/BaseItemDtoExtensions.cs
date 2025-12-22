using System.Globalization;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Models;

namespace JellyfinPlayer.Lib.Extensions;

public static class BaseItemDtoExtensions
{
    public static MediaItem ToMediaItem(this BaseItemDto dto, string? baseImageUrl = null)
    {
        var imageUrl = GetImageUrl(dto, baseImageUrl);
        var runtimeMinutes = dto.RunTimeTicks.HasValue
            ? (int?)(dto.RunTimeTicks.Value / TimeSpan.TicksPerMinute)
            : null;

        var itemId = dto.Id ?? string.Empty;

        return new MediaItem(
            Id: itemId,
            Name: dto.Name ?? "Unknown",
            Overview: dto.Overview,
            ReleaseDate: dto.PremiereDate?.DateTime,
            ImageUrl: imageUrl
        )
        {
            Type = dto.Type?.ToString() ?? "Unknown",
            RuntimeMinutes = runtimeMinutes,
            Genres = dto.Genres?.ToList() ?? [],
            Tags = dto.Tags?.ToList() ?? [],
            CommunityRating = dto.CommunityRating.HasValue
                ? (decimal?)dto.CommunityRating.Value
                : null,
            DateCreated = dto.DateCreated?.DateTime,
            ProductionYear = dto.ProductionYear?.ToString(CultureInfo.CurrentCulture),
            OfficialRating = dto.OfficialRating,
            IsFolder = dto.IsFolder ?? false,
            CollectionType = dto.CollectionType?.ToString(),
            ChildCount = dto.ChildCount,
            RunTimeTicks = dto.RunTimeTicks,
            SeriesName = dto.SeriesName,
            IndexNumber = dto.IndexNumber,
            ParentIndexNumber = dto.ParentIndexNumber,
        };
    }

    private static string? GetImageUrl(BaseItemDto dto, string? baseImageUrl)
    {
        if (string.IsNullOrWhiteSpace(baseImageUrl) || dto.Id is null)
            return null;

        var itemId = dto.Id;

        // ImageTags is stored in AdditionalData as a dictionary-like structure
        // Try to get primary image tag from AdditionalData
        if (dto.ImageTags?.AdditionalData.TryGetValue("Primary", out var primaryTagObj) == true)
        {
            var primaryTag = primaryTagObj?.ToString();
            if (!string.IsNullOrWhiteSpace(primaryTag))
            {
                return $"{baseImageUrl.TrimEnd('/')}/Items/{itemId}/Images/Primary?tag={primaryTag}";
            }
        }

        // Fallback to backdrop or other images
        if (dto.ImageTags?.AdditionalData.TryGetValue("Backdrop", out var backdropTagObj) == true)
        {
            var backdropTag = backdropTagObj?.ToString();
            if (!string.IsNullOrWhiteSpace(backdropTag))
            {
                return $"{baseImageUrl.TrimEnd('/')}/Items/{itemId}/Images/Backdrop?tag={backdropTag}";
            }
        }

        // If no image tags found, return URL without tag (server will return default)
        return $"{baseImageUrl.TrimEnd('/')}/Items/{itemId}/Images/Primary";
    }
}
