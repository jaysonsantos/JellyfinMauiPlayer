namespace Mpv.Sys.Tests;

public class Player
{
    [Fact]
    public void Test1()
    {
        var client = new MpvClient();
        Assert.NotNull(client);

        // Configure for headless environment (no video output)
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.SetOption("video", "no");

        client.Initialize();
        client.Dispose();
    }

    [Fact]
    public void Version()
    {
        Assert.Equal(131077uL, MpvClient.Version());
    }

    [Fact]
    public void InitializeWithHeadlessOptions()
    {
        var client = new MpvClient();

        // Set options before initialization for headless systems
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.SetOption("video", "no");
        client.SetOption("terminal", "yes");
        client.SetOption("msg-level", "all=v");

        client.Initialize();

        Assert.NotNull(client);

        client.Dispose();
    }

    [Fact]
    public void LoadVideoAndVerifyTracks()
    {
        var client = new MpvClient();

        // Configure for headless environment (no video output)
        client.SetOption("vo", "null");
        client.SetOption("ao", "null");
        client.SetOption("video", "no");

        client.Initialize();

        // Load the test video file
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

        // Use loadfile command to load the video
        client.Command("loadfile", absolutePath, "replace");

        // Wait a bit for the file to load and tracks to be detected
        Thread.Sleep(1000);

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

        client.Dispose();
    }
}
