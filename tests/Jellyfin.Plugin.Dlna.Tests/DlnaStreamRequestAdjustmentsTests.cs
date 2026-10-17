using System;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaStreamRequestAdjustmentsTests
{
    private static MediaSourceInfo LiveTvSource(int? defaultSubtitleIndex = null)
        => new() { IsInfiniteStream = true, DefaultSubtitleStreamIndex = defaultSubtitleIndex };

    private static MediaSourceInfo LiveRadioSource()
        => new()
        {
            IsInfiniteStream = true,
            DefaultSubtitleStreamIndex = 1,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Audio, Index = 0, Codec = "aac" }
            ]
        };

    private static MediaSourceInfo UncodedLiveVideoSource()
        => new()
        {
            IsInfiniteStream = true,
            DefaultSubtitleStreamIndex = 2,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Index = 0, Codec = null },
                new MediaStream { Type = MediaStreamType.Subtitle, Index = 2, Codec = "dvb_subtitle", Language = "ron", Score = 5 }
            ]
        };

    private static MediaSourceInfo MovieSource(int? defaultSubtitleIndex = null)
        => new()
        {
            IsInfiniteStream = false,
            DefaultSubtitleStreamIndex = defaultSubtitleIndex,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Subtitle, Index = 2, Language = "ron", Codec = "srt", IsExternal = true }
            ]
        };

    private static StreamInfo StreamInfoFor(MediaSourceInfo mediaSource, int? subtitleIndex)
        => new()
        {
            DeviceProfile = new DeviceProfile(),
            MediaSource = mediaSource,
            SubtitleStreamIndex = subtitleIndex,
            SubtitleDeliveryMethod = SubtitleDeliveryMethod.External
        };

    private static void WithBurnIn(bool enabled, Action body)
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = enabled;
            body();
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ShouldBurnInSubtitles_RequiresEnabledLiveTvSource()
    {
        WithBurnIn(true, () =>
        {
            Assert.True(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(LiveTvSource()));
            Assert.True(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(UncodedLiveVideoSource()));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(LiveTvSource(), MediaType.Audio));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(LiveRadioSource()));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(MovieSource()));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(null));
        });

        WithBurnIn(false, () => Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(LiveTvSource())));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyBrowseSubtitlePreferences_LeavesMoviesUntouched(bool burnInEnabled)
    {
        WithBurnIn(burnInEnabled, () =>
        {
            var streamInfo = StreamInfoFor(MovieSource(), 2);

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.External, streamInfo.SubtitleDeliveryMethod);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyBrowseSubtitlePreferences_LeavesMoviesUntouchedWithExplicitSource(bool burnInEnabled)
    {
        WithBurnIn(burnInEnabled, () =>
        {
            var movie = MovieSource();
            var streamInfo = StreamInfoFor(movie, 2);

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, movie);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        });
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_LeavesLiveTvWhenEnabled()
    {
        WithBurnIn(true, () =>
        {
            var streamInfo = StreamInfoFor(LiveTvSource(), 2);

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        });
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_ClearsLiveTvWhenDisabled()
    {
        WithBurnIn(false, () =>
        {
            var streamInfo = StreamInfoFor(LiveTvSource(), 2);

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo);

            Assert.Null(streamInfo.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, streamInfo.SubtitleDeliveryMethod);
        });
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_ExplicitSourceWinsOverStreamInfoSource()
    {
        WithBurnIn(false, () =>
        {
            // StreamInfo carries a movie source but the caller knows the item is live TV.
            var streamInfo = StreamInfoFor(MovieSource(), 2);

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, LiveTvSource());

            Assert.Null(streamInfo.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, streamInfo.SubtitleDeliveryMethod);
        });
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_IgnoresNullStreamInfo()
    {
        WithBurnIn(false, () => DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(null, LiveTvSource()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplySubtitleBurnInPreferences_LeavesMoviesUntouched(bool burnInEnabled)
    {
        WithBurnIn(burnInEnabled, () =>
        {
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 2,
                SubtitleMethod = SubtitleDeliveryMethod.Embed
            };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, MovieSource(2));

            Assert.Equal(2, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Embed, request.SubtitleMethod);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplySubtitleBurnInPreferences_DoesNotFillInMovieDefault(bool burnInEnabled)
    {
        WithBurnIn(burnInEnabled, () =>
        {
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, MovieSource(2));

            Assert.Null(request.SubtitleStreamIndex);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_ClearsLiveTvWhenDisabled()
    {
        WithBurnIn(false, () =>
        {
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 2,
                SubtitleMethod = SubtitleDeliveryMethod.Encode
            };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveTvSource(2));

            Assert.Null(request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, request.SubtitleMethod);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_SetsEncodeWhenEnabledForLiveTvAndDefaultIndexPresent()
    {
        WithBurnIn(true, () =>
        {
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveTvSource(2));

            Assert.Equal(2, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Encode, request.SubtitleMethod);
            Assert.False(request.AllowVideoStreamCopy);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_DisablesVideoCopyWhenExplicitEncodeIndexIsKept()
    {
        WithBurnIn(true, () =>
        {
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 5,
                SubtitleMethod = SubtitleDeliveryMethod.Encode,
                AllowVideoStreamCopy = true
            };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveTvSource(2));

            Assert.Equal(5, request.SubtitleStreamIndex);
            Assert.False(request.AllowVideoStreamCopy);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_LeavesLiveRadioUntouched()
    {
        WithBurnIn(true, () =>
        {
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveRadioSource(), MediaType.Audio);

            Assert.Null(request.SubtitleStreamIndex);
            Assert.True(request.AllowVideoStreamCopy);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_KeepsExplicitIndexWhenEnabledForLiveTv()
    {
        WithBurnIn(true, () =>
        {
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 5,
                SubtitleMethod = SubtitleDeliveryMethod.Embed
            };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveTvSource(2));

            Assert.Equal(5, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Embed, request.SubtitleMethod);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public void ApplySubtitleBurnInPreferences_IgnoresMissingDefaultIndexWhenEnabledForLiveTv(int? defaultIndex)
    {
        WithBurnIn(true, () =>
        {
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, LiveTvSource(defaultIndex));

            Assert.Null(request.SubtitleStreamIndex);
        });
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_IgnoresNonVideoAndMissingSource()
    {
        WithBurnIn(false, () =>
        {
            var request = new VideoRequestDto { SubtitleStreamIndex = 2 };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(false, request, LiveTvSource());
            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, null);
            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, null, LiveTvSource());

            Assert.Equal(2, request.SubtitleStreamIndex);
        });
    }

    [Fact]
    public void RequireBurnInTranscode_DisablesDirectPlaybackAndVideoCopy()
    {
        var options = new MediaOptions
        {
            Profile = new DeviceProfile(),
            EnableDirectPlay = true,
            EnableDirectStream = true,
            AllowVideoStreamCopy = true
        };

        DlnaStreamRequestAdjustments.RequireBurnInTranscode(options);

        Assert.False(options.EnableDirectPlay);
        Assert.False(options.EnableDirectStream);
        Assert.False(options.AllowVideoStreamCopy);
    }

    [Theory]
    [InlineData("copy")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("COPY")]
    public void SelectBurnInVideoCodec_ReplacesMissingOrCopiedCodecWithH264(string? outputCodec)
    {
        Assert.Equal("h264", DlnaStreamRequestAdjustments.SelectBurnInVideoCodec(outputCodec, null));
    }

    [Fact]
    public void SelectBurnInVideoCodec_PrefersRequestedEncodableCodecOverCopy()
    {
        Assert.Equal("hevc", DlnaStreamRequestAdjustments.SelectBurnInVideoCodec("copy", ["copy", "hevc", "h264"]));
        Assert.Equal("h264", DlnaStreamRequestAdjustments.SelectBurnInVideoCodec("h264", ["hevc"]));
    }

    [Fact]
    public void CopyForSubtitleSelection_DoesNotShareSubtitleState()
    {
        var originalStream = new MediaStream
        {
            Type = MediaStreamType.Subtitle,
            Index = 2,
            Score = 5,
            Language = "ron",
            Codec = "dvb_subtitle"
        };
        var original = new MediaSourceInfo
        {
            IsInfiniteStream = true,
            DefaultSubtitleStreamIndex = null,
            MediaStreams = [originalStream]
        };

        var copy = DlnaStreamRequestAdjustments.CopyForSubtitleSelection(original);
        copy.DefaultSubtitleStreamIndex = 2;
        copy.MediaStreams[0].Score = 99;

        Assert.Null(original.DefaultSubtitleStreamIndex);
        Assert.Equal(5, originalStream.Score);
        Assert.NotSame(originalStream, copy.MediaStreams[0]);
    }
}
