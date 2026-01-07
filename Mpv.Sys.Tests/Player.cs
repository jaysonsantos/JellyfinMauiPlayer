using Mpv.Sys.Internal;

namespace Mpv.Sys.Tests;

public class Player
{
    [Fact]
    public void Version()
    {
        Assert.Equal(131077uL, MpvClient.Version());
    }

    [Fact]
    public void CreateAndDisposeClient()
    {
        using var client = new MpvClient();
        Assert.NotNull(client);
        Assert.NotEqual(IntPtr.Zero, client.GetHandle());
    }

    [Fact]
    public void DisposeClientMultipleTimes()
    {
        var client = new MpvClient();
        client.Dispose();
        // Should not throw when disposing multiple times
        client.Dispose();
    }

    [Fact]
    public void InitializeClient()
    {
        using var client = CreateInitializedClient();

        // Calling Initialize again should be safe (idempotent)
        client.Initialize();
    }

    [Fact]
    public void SetOptionBeforeInitialize()
    {
        using var client = new MpvClient();

        // Setting options before Initialize should work
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.SetOption("video", "no");
        client.SetOption("audio", "no");

        client.Initialize();
    }

    [Fact]
    public void SetInvalidOptionThrows()
    {
        using var client = new MpvClient();

        // Setting an invalid option value should throw
        Assert.Throws<Exception>(() =>
            client.SetOption("invalid-option-name-xyz", "invalid-value")
        );
    }

    [Fact]
    public void GetHandle()
    {
        using var client = new MpvClient();
        IntPtr handle = client.GetHandle();

        Assert.NotEqual(IntPtr.Zero, handle);
    }

    [Fact]
    public void ErrorToStringPublic()
    {
        using var client = new MpvClient();

        // Error code -1 is MPV_ERROR_EVENT_QUEUE_FULL or similar
        string errorString = client.ErrorToStringPublic(-1);
        Assert.NotNull(errorString);
        Assert.NotEmpty(errorString);
    }

    [Fact]
    public void CommandWithoutFile()
    {
        using var client = CreateInitializedClient();

        // Commands that don't require a loaded file should work
        // "quit" would close mpv, so use a safe command like "print-text"
        client.Command("print-text", "test message");
    }

    [Fact]
    public void LoadVideoAndVerifyTracks()
    {
        using var client = CreateInitializedClientWithVideo();

        // Get audio tracks
        IReadOnlyList<TrackInfo> audioTracks = client.GetAudioTracks();
        Assert.NotEmpty(audioTracks);

        // Get subtitle tracks
        IReadOnlyList<TrackInfo> subtitleTracks = client.GetSubtitleTracks();
        Assert.NotEmpty(subtitleTracks);

        // Verify we have English (eng) audio track
        TrackInfo? engAudioTrack = audioTracks.FirstOrDefault(t =>
            string.Equals(t.Language, "eng", StringComparison.OrdinalIgnoreCase)
        );
        Assert.NotNull(engAudioTrack);

        // Verify we have Portuguese (pob or por) audio track
        TrackInfo? pobAudioTrack = audioTracks.FirstOrDefault(t =>
            string.Equals(t.Language, "pob", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.Language, "por", StringComparison.OrdinalIgnoreCase)
        );
        Assert.NotNull(pobAudioTrack);

        // Verify we have English (eng) subtitle track
        TrackInfo? engSubtitleTrack = subtitleTracks.FirstOrDefault(t =>
            string.Equals(t.Language, "eng", StringComparison.OrdinalIgnoreCase)
        );
        Assert.NotNull(engSubtitleTrack);

        // Verify we have Portuguese (pob or por) subtitle track
        TrackInfo? pobSubtitleTrack = subtitleTracks.FirstOrDefault(t =>
            string.Equals(t.Language, "pob", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.Language, "por", StringComparison.OrdinalIgnoreCase)
        );
        Assert.NotNull(pobSubtitleTrack);
    }

    [Fact]
    public void SetAudioTrack()
    {
        using var client = CreateInitializedClientWithVideo();

        IReadOnlyList<TrackInfo> audioTracks = client.GetAudioTracks();
        Assert.NotEmpty(audioTracks);

        // Switch to each audio track
        foreach (var track in audioTracks)
        {
            client.SetAudioTrack(track.Id);

            // Verify the track was selected
            TrackInfo? currentTrack = client.GetCurrentAudioTrack();
            Assert.NotNull(currentTrack);
            Assert.Equal(track.Id, currentTrack.Value.Id);
        }
    }

    [Fact]
    public void SetSubtitleTrack()
    {
        using var client = CreateInitializedClientWithVideo();

        IReadOnlyList<TrackInfo> subtitleTracks = client.GetSubtitleTracks();
        Assert.NotEmpty(subtitleTracks);

        // Switch to each subtitle track
        foreach (var track in subtitleTracks)
        {
            client.SetSubtitleTrack(track.Id);

            // Verify the track was selected
            TrackInfo? currentTrack = client.GetCurrentSubtitleTrack();
            Assert.NotNull(currentTrack);
            Assert.Equal(track.Id, currentTrack.Value.Id);
        }
    }

    [Fact]
    public void DisableSubtitleTrack()
    {
        using var client = CreateInitializedClientWithVideo();

        IReadOnlyList<TrackInfo> subtitleTracks = client.GetSubtitleTracks();
        Assert.NotEmpty(subtitleTracks);

        // First, enable a subtitle track
        client.SetSubtitleTrack(subtitleTracks[0].Id);
        TrackInfo? currentTrack = client.GetCurrentSubtitleTrack();
        Assert.NotNull(currentTrack);

        // Now disable subtitles using SetSubtitleTrack(0)
        client.SetSubtitleTrack(0);
        currentTrack = client.GetCurrentSubtitleTrack();
        Assert.Null(currentTrack);
    }

    [Fact]
    public void SetSubtitleVisibilityFalse()
    {
        using var client = CreateInitializedClientWithVideo();

        IReadOnlyList<TrackInfo> subtitleTracks = client.GetSubtitleTracks();
        Assert.NotEmpty(subtitleTracks);

        // First, enable a subtitle track
        client.SetSubtitleTrack(subtitleTracks[0].Id);
        TrackInfo? currentTrack = client.GetCurrentSubtitleTrack();
        Assert.NotNull(currentTrack);

        // Hide subtitles using SetSubtitleVisibility
        client.SetSubtitleVisibility(false);
        currentTrack = client.GetCurrentSubtitleTrack();
        Assert.Null(currentTrack);
    }

    [Fact]
    public void SetSubtitleVisibilityTrueDoesNothing()
    {
        using var client = CreateInitializedClientWithVideo();

        // Disable subtitles first
        client.SetSubtitleTrack(0);
        TrackInfo? currentTrack = client.GetCurrentSubtitleTrack();
        Assert.Null(currentTrack);

        // SetSubtitleVisibility(true) should not enable any track
        client.SetSubtitleVisibility(true);
        currentTrack = client.GetCurrentSubtitleTrack();
        Assert.Null(currentTrack);
    }

    [Fact]
    public void TrackInfoContainsExpectedProperties()
    {
        using var client = CreateInitializedClientWithVideo();

        IReadOnlyList<TrackInfo> audioTracks = client.GetAudioTracks();
        Assert.NotEmpty(audioTracks);

        // Check that track info has expected structure
        var firstTrack = audioTracks[0];
        Assert.True(firstTrack.Id > 0);
        Assert.Equal("audio", firstTrack.Type);
    }

    [Fact]
    public void GetEmptyTracksBeforeFileLoaded()
    {
        using var client = CreateInitializedClient();

        // Before loading any file, track lists should be empty
        IReadOnlyList<TrackInfo> audioTracks = client.GetAudioTracks();
        IReadOnlyList<TrackInfo> subtitleTracks = client.GetSubtitleTracks();

        Assert.Empty(audioTracks);
        Assert.Empty(subtitleTracks);
    }

    [Fact]
    public void GetCurrentTracksBeforeFileLoaded()
    {
        using var client = CreateInitializedClient();

        // Before loading any file, current tracks should be null
        TrackInfo? currentAudio = client.GetCurrentAudioTrack();
        TrackInfo? currentSubtitle = client.GetCurrentSubtitleTrack();

        Assert.Null(currentAudio);
        Assert.Null(currentSubtitle);
    }

    [Fact]
    public void ObserveAndUnobserveProperty()
    {
        using var client = CreateInitializedClient();

        // Observe a property
        client.ObserveProperty((ulong)ObservedProperty.Pause, "pause", MpvFormat.Flag);

        // Unobserve should return the count of unobserved properties
        int unobservedCount = client.UnobserveProperty((ulong)ObservedProperty.Pause);
        Assert.Equal(1, unobservedCount);

        // Unobserving again should return 0 (nothing to unobserve)
        unobservedCount = client.UnobserveProperty((ulong)ObservedProperty.Pause);
        Assert.Equal(0, unobservedCount);
    }

    [Fact]
    public void ObserveMultipleProperties()
    {
        using var client = CreateInitializedClient();

        // Observe multiple properties
        client.ObserveProperty((ulong)ObservedProperty.Pause, "pause", MpvFormat.Flag);
        client.ObserveProperty((ulong)ObservedProperty.Duration, "duration", MpvFormat.Double);
        client.ObserveProperty((ulong)ObservedProperty.TimePos, "time-pos", MpvFormat.Double);

        // Unobserve each one
        Assert.Equal(1, client.UnobserveProperty((ulong)ObservedProperty.Pause));
        Assert.Equal(1, client.UnobserveProperty((ulong)ObservedProperty.Duration));
        Assert.Equal(1, client.UnobserveProperty((ulong)ObservedProperty.TimePos));
    }

    [Fact]
    public void GetPropertyPtrInt64()
    {
        using var client = CreateInitializedClientWithVideo();

        // Get track-list/count which should be > 0 after loading video
        IntPtr countPtr = client.GetPropertyPtr("track-list/count", MpvFormat.Int64);
        int trackCount = (int)(long)countPtr;

        Assert.True(trackCount > 0);
    }

    [Fact]
    public void GetPropertyPtrString()
    {
        using var client = CreateInitializedClientWithVideo();

        // Get the media-title property
        IntPtr titlePtr = client.GetPropertyPtr("media-title", MpvFormat.String);
        Assert.NotEqual(IntPtr.Zero, titlePtr);

        string? title = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(titlePtr);
        Assert.NotNull(title);
    }

    [Fact]
    public void GetInvalidPropertyThrows()
    {
        using var client = CreateInitializedClient();

        Assert.Throws<Exception>(() =>
            client.GetPropertyPtr("invalid-property-xyz", MpvFormat.Int64)
        );
    }

    [Fact]
    public void CommandLoadfileWithInvalidPath()
    {
        using var client = CreateInitializedClient();

        // Loading a non-existent file should not throw immediately
        // (MPV handles this asynchronously)
        client.Command("loadfile", "/nonexistent/path/to/file.mkv", "replace");

        // Wait briefly and check tracks - should be empty
        Thread.Sleep(500);

        IReadOnlyList<TrackInfo> audioTracks = client.GetAudioTracks();
        Assert.Empty(audioTracks);
    }

    [Fact]
    public void UsingStatementDisposesClient()
    {
        IntPtr handle;
        {
            using var client = CreateInitializedClient();
            handle = client.GetHandle();
            Assert.NotEqual(IntPtr.Zero, handle);
        }
        // Client should be disposed after using block
    }

    [Fact]
    public void MultipleClientsCanBeCreated()
    {
        using var client1 = new MpvClient();
        using var client2 = new MpvClient();

        Assert.NotEqual(IntPtr.Zero, client1.GetHandle());
        Assert.NotEqual(IntPtr.Zero, client2.GetHandle());
        Assert.NotEqual(client1.GetHandle(), client2.GetHandle());
    }

    [Fact]
    public void VerifyTrackInfoRecordEquality()
    {
        var track1 = new TrackInfo(1, "audio", "eng", "English", "aac", true, false);
        var track2 = new TrackInfo(1, "audio", "eng", "English", "aac", true, false);
        var track3 = new TrackInfo(2, "audio", "jpn", "Japanese", "aac", false, false);

        Assert.Equal(track1, track2);
        Assert.NotEqual(track1, track3);
    }

    /// <summary>
    /// Helper method to create an initialized MpvClient without loading a video.
    /// </summary>
    private static MpvClient CreateInitializedClient()
    {
        var client = new MpvClient();
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.Initialize();

        return client;
    }

    /// <summary>
    /// Helper method to create an initialized MpvClient with the test video loaded.
    /// </summary>
    private static MpvClient CreateInitializedClientWithVideo()
    {
        var client = new MpvClient();
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.SetOption("video", "no");
        client.Initialize();

        string videoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "test-assets",
            "test_video.mkv"
        );
        string absolutePath = Path.GetFullPath(videoPath);

        client.Command("loadfile", absolutePath, "replace");
        Thread.Sleep(1000);

        return client;
    }
}
