using JellyfinPlayer.Lib.Api.Models;

namespace JellyfinPlayer.Lib.Api;

/// <summary>
/// Builder for creating Jellyfin device profiles optimized for MAUI MediaElement playback.
/// </summary>
public sealed class DeviceProfileBuilder
{
    /// <summary>
    /// Creates a default device profile for MAUI MediaElement with HLS and MP4 support.
    /// </summary>
    public static DeviceProfile CreateDefaultProfile()
    {
        return new DeviceProfile
        {
            Name = "Jellyfin Player MAUI",
            MaxStreamingBitrate = 120_000_000, // 120 Mbps
            MaxStaticBitrate = 100_000_000, // 100 Mbps
            MusicStreamingTranscodingBitrate = 384_000, // 384 kbps
            DirectPlayProfiles = CreateDirectPlayProfiles(),
            TranscodingProfiles = CreateTranscodingProfiles(),
            CodecProfiles = CreateCodecProfiles(),
            SubtitleProfiles = CreateSubtitleProfiles(),
        };
    }

    private static List<DirectPlayProfile> CreateDirectPlayProfiles()
    {
        // "Play everything" approach - claim support for all formats
        // and let Jellyfin server decide when to transcode
        return
        [
            new DirectPlayProfile { Type = DirectPlayProfile_Type.Video },
            new DirectPlayProfile { Type = DirectPlayProfile_Type.Audio },
            new DirectPlayProfile { Type = DirectPlayProfile_Type.Photo },
        ];
    }

    private static List<TranscodingProfile> CreateTranscodingProfiles()
    {
        return
        [
            // Generic audio transcoding
            new TranscodingProfile { Type = TranscodingProfile_Type.Audio },
            // HLS video transcoding with configurable codecs
            new TranscodingProfile
            {
                Container = "ts",
                Type = TranscodingProfile_Type.Video,
                Protocol = TranscodingProfile_Protocol.Hls,
                AudioCodec = "aac,mp3,ac3,opus,flac,vorbis",
                VideoCodec = "h264,mpeg4,mpeg2video",
                MaxAudioChannels = "6",
            },
            // Photo transcoding
            new TranscodingProfile { Container = "jpeg", Type = TranscodingProfile_Type.Photo },
        ];
    }

    private static List<CodecProfile> CreateCodecProfiles()
    {
        // Empty list - no codec restrictions
        // Let the server decide based on DirectPlayProfiles and TranscodingProfiles
        return [];
    }

    private static List<SubtitleProfile> CreateSubtitleProfiles()
    {
        return
        [
            // External subtitle formats
            new SubtitleProfile { Format = "srt", Method = SubtitleProfile_Method.External },
            new SubtitleProfile { Format = "ass", Method = SubtitleProfile_Method.External },
            new SubtitleProfile { Format = "sub", Method = SubtitleProfile_Method.External },
            new SubtitleProfile { Format = "ssa", Method = SubtitleProfile_Method.External },
            new SubtitleProfile { Format = "smi", Method = SubtitleProfile_Method.External },
            // Embedded subtitle formats
            new SubtitleProfile { Format = "srt", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "ass", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "sub", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "ssa", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "smi", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "pgssub", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "dvdsub", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "dvbsub", Method = SubtitleProfile_Method.Embed },
            new SubtitleProfile { Format = "pgs", Method = SubtitleProfile_Method.Embed },
        ];
    }
}
