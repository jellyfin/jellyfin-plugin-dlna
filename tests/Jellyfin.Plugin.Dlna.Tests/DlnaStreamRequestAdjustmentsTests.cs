using Jellyfin.Plugin.Dlna.Playback;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaStreamRequestAdjustmentsTests
{
    [Fact]
    public void CapLiveStreamTranscodeResolution_LimitsUnboundedLiveStreamTo1080p()
    {
        var request = new VideoRequestDto();
        var mediaSource = new MediaSourceInfo { IsInfiniteStream = true };

        DlnaStreamRequestAdjustments.CapLiveStreamTranscodeResolution(request, mediaSource);

        Assert.Equal(1920, request.MaxWidth);
        Assert.Equal(1080, request.MaxHeight);
    }

    [Fact]
    public void CapLiveStreamTranscodeResolution_ReducesOversizedLiveStreamCaps()
    {
        var request = new VideoRequestDto { MaxWidth = 3840, MaxHeight = 2160 };
        var mediaSource = new MediaSourceInfo { IsInfiniteStream = true };

        DlnaStreamRequestAdjustments.CapLiveStreamTranscodeResolution(request, mediaSource);

        Assert.Equal(1920, request.MaxWidth);
        Assert.Equal(1080, request.MaxHeight);
    }

    [Fact]
    public void CapLiveStreamTranscodeResolution_DoesNotUpscaleSmallerCaps()
    {
        var request = new VideoRequestDto { MaxWidth = 1280, MaxHeight = 720 };
        var mediaSource = new MediaSourceInfo { IsInfiniteStream = true };

        DlnaStreamRequestAdjustments.CapLiveStreamTranscodeResolution(request, mediaSource);

        Assert.Equal(1280, request.MaxWidth);
        Assert.Equal(720, request.MaxHeight);
    }

    [Fact]
    public void CapLiveStreamTranscodeResolution_SkipsNonLiveSources()
    {
        var request = new VideoRequestDto { MaxWidth = 3840, MaxHeight = 2160 };
        var mediaSource = new MediaSourceInfo { IsInfiniteStream = false };

        DlnaStreamRequestAdjustments.CapLiveStreamTranscodeResolution(request, mediaSource);

        Assert.Equal(3840, request.MaxWidth);
        Assert.Equal(2160, request.MaxHeight);
    }

    [Fact]
    public void NeedsFreshLiveProbe_TrueWhenLiveVideoSizeIsMissing()
    {
        var mediaSource = new MediaSourceInfo
        {
            IsInfiniteStream = true,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0 }
            ]
        };

        Assert.True(DlnaStreamRequestAdjustments.NeedsFreshLiveProbe(mediaSource));
    }

    [Fact]
    public void NeedsFreshLiveProbe_FalseWhenLiveVideoSizeIsKnown()
    {
        var mediaSource = new MediaSourceInfo
        {
            IsInfiniteStream = true,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "h264", Width = 1280, Height = 720 }
            ]
        };

        Assert.False(DlnaStreamRequestAdjustments.NeedsFreshLiveProbe(mediaSource));
    }

    [Fact]
    public void NeedsFreshLiveProbe_FalseForNonLiveSources()
    {
        var mediaSource = new MediaSourceInfo
        {
            IsInfiniteStream = false,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0 }
            ]
        };

        Assert.False(DlnaStreamRequestAdjustments.NeedsFreshLiveProbe(mediaSource));
    }

    [Fact]
    public void TryApplyProbedLiveMediaInfo_ReplacesStreamsWhenProbeHasRealSize()
    {
        var target = new MediaSourceInfo
        {
            IsInfiniteStream = true,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0, Index = 1 }
            ]
        };
        var probed = new MediaSourceInfo
        {
            Container = "ts",
            Bitrate = 25000000,
            DefaultAudioStreamIndex = 2,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 3840, Height = 2160, Index = 1 },
                new MediaStream { Type = MediaStreamType.Audio, Codec = "aac", Channels = 2, Index = 2 }
            ]
        };

        var applied = DlnaStreamRequestAdjustments.TryApplyProbedLiveMediaInfo(target, probed);

        Assert.True(applied);
        Assert.Equal(3840, target.MediaStreams[0].Width);
        Assert.Equal(2160, target.MediaStreams[0].Height);
        Assert.Equal(2, target.MediaStreams[1].Channels);
        Assert.Equal("ts", target.Container);
        Assert.Equal(25000000, target.Bitrate);
        Assert.Equal(2, target.DefaultAudioStreamIndex);
    }

    [Fact]
    public void TryApplyProbedLiveMediaInfo_LeavesTargetWhenProbeIsStillEmpty()
    {
        var target = new MediaSourceInfo
        {
            IsInfiniteStream = true,
            Container = "ts",
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0 }
            ]
        };
        var probed = new MediaSourceInfo
        {
            Container = "mpegts",
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "hevc", Width = 0, Height = 0 }
            ]
        };

        var applied = DlnaStreamRequestAdjustments.TryApplyProbedLiveMediaInfo(target, probed);

        Assert.False(applied);
        Assert.Equal("ts", target.Container);
        Assert.Equal(0, target.MediaStreams[0].Width);
    }
}
