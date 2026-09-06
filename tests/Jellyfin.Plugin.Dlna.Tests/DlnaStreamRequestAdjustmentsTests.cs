using Jellyfin.Plugin.Dlna.Playback;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
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
}
