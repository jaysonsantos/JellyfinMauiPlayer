using System.ComponentModel.DataAnnotations;

namespace JellyfinPlayer.Lib.Storage;

public sealed class CacheEntry
{
    [Key]
    [MaxLength(500)]
    public required string Key { get; init; }

    [Required]
    public required string Value { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}
