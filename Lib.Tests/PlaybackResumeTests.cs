using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using JellyfinPlayer.Lib.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lib.Tests;

public class PlaybackResumeTests
{
    private const string TestItemId = "test-item-123";
    private const long ThirtySecondsInTicks = 300_000_000; // 30 seconds
    private const long OneMinuteInTicks = 600_000_000; // 60 seconds
    private const long FiveSecondsInTicks = 50_000_000; // 5 seconds

    [Fact]
    public async Task GetPlaybackState_WithSavedPosition_ReturnsState()
    {
        // Arrange
        var storageService = new InMemoryStorageService();
        var secureStorage = new InMemorySecureStorage();
        var logger = NullLogger<PlaybackService>.Instance;

        var playbackService = new PlaybackService(
            null!, // JellyfinApiClientFactory - not needed for this test
            secureStorage,
            storageService,
            logger
        );

        var savedState = new PlaybackState(
            ItemId: TestItemId,
            PositionTicks: OneMinuteInTicks,
            IsPaused: false
        )
        {
            SessionId = "test-session",
        };

        await playbackService.SavePlaybackStateAsync(savedState, CancellationToken.None);

        // Act
        var retrievedState = await playbackService.GetPlaybackStateAsync(
            TestItemId,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(retrievedState);
        Assert.Equal(TestItemId, retrievedState.ItemId);
        Assert.Equal(OneMinuteInTicks, retrievedState.PositionTicks);
        Assert.Equal("test-session", retrievedState.SessionId);
    }

    [Fact]
    public async Task GetPlaybackState_WithNoSavedState_ReturnsNull()
    {
        // Arrange
        var storageService = new InMemoryStorageService();
        var secureStorage = new InMemorySecureStorage();
        var logger = NullLogger<PlaybackService>.Instance;

        var playbackService = new PlaybackService(
            null!, // JellyfinApiClientFactory - not needed for this test
            secureStorage,
            storageService,
            logger
        );

        // Act
        var retrievedState = await playbackService.GetPlaybackStateAsync(
            "non-existent-item",
            CancellationToken.None
        );

        // Assert
        Assert.Null(retrievedState);
    }

    [Theory]
    [InlineData(ThirtySecondsInTicks + 1, true)] // Just over 30 seconds - should prompt
    [InlineData(OneMinuteInTicks, true)] // 60 seconds - should prompt
    [InlineData(ThirtySecondsInTicks, false)] // Exactly 30 seconds - should not prompt
    [InlineData(FiveSecondsInTicks, false)] // 5 seconds - should not prompt
    [InlineData(0, false)] // 0 seconds - should not prompt
    public void ResumeThreshold_ShouldMatchExpectedBehavior(long positionTicks, bool shouldPrompt)
    {
        // This test verifies the logic for determining whether to show resume prompt
        const long minResumeThresholdTicks = 300_000_000; // 30 seconds

        bool actualShouldPrompt = positionTicks > minResumeThresholdTicks;

        Assert.Equal(shouldPrompt, actualShouldPrompt);
    }

    [Fact]
    public void ConvertTicksToTimeSpan_ShouldWorkCorrectly()
    {
        // Arrange
        var thirtySeconds = TimeSpan.FromTicks(ThirtySecondsInTicks);
        var oneMinute = TimeSpan.FromTicks(OneMinuteInTicks);
        var oneHour = TimeSpan.FromTicks(36_000_000_000); // 1 hour in ticks

        // Assert
        Assert.Equal(30, thirtySeconds.TotalSeconds);
        Assert.Equal(60, oneMinute.TotalSeconds);
        Assert.Equal(3600, oneHour.TotalSeconds);
    }
}
