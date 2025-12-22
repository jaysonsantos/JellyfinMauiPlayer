namespace JellyfinPlayer.Lib.Models;

public sealed record UserAuthenticationResult(
    string AccessToken,
    string UserId,
    string ServerId,
    string ServerName
)
{
    public required string Username { get; init; }
    public DateTime AuthenticatedAt { get; init; } = DateTime.UtcNow;
}
