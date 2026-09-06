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
    [Fact]
    public void ShouldBurnInSubtitles_RequiresEnabledLiveTvSource()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;

            Assert.True(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(new MediaSourceInfo { IsInfiniteStream = true }));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(new MediaSourceInfo { IsInfiniteStream = false }));
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(null));

            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            Assert.False(DlnaStreamRequestAdjustments.ShouldBurnInSubtitles(new MediaSourceInfo { IsInfiniteStream = true }));
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_LeavesStreamInfoForLiveTvWhenEnabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };
            var mediaSource = new MediaSourceInfo { IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, mediaSource);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_LeavesMoviesUntouched()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };
            var mediaSource = new MediaSourceInfo
            {
                IsInfiniteStream = false,
                MediaStreams =
                [
                    new MediaStream { Type = MediaStreamType.Subtitle, Index = 2, Language = "ron", IsTextSubtitleStream = true }
                ]
            };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, mediaSource);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_ClearsLiveTvWhenBurnInDisabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };
            var mediaSource = new MediaSourceInfo { IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, mediaSource);

            Assert.Null(streamInfo.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, streamInfo.SubtitleDeliveryMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_LeavesMoviesUntouchedWhenBurnInDisabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };
            var mediaSource = new MediaSourceInfo
            {
                IsInfiniteStream = false,
                MediaStreams =
                [
                    new MediaStream { Type = MediaStreamType.Subtitle, Index = 2, Language = "ron", IsTextSubtitleStream = true }
                ]
            };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo, mediaSource);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_LeavesMoviesUntouchedWhenDisabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 2,
                SubtitleMethod = SubtitleDeliveryMethod.Encode
            };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, new MediaSourceInfo { IsInfiniteStream = false });

            Assert.Equal(2, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Encode, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_DoesNothingWhenDisabledAndNoIndexForLiveTv()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, new MediaSourceInfo { IsInfiniteStream = true });

            Assert.Null(request.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_SetsEncodeWhenEnabledForLiveTvAndDefaultIndexPresent()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto();
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2, IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Equal(2, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Encode, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_LeavesMoviesUntouchedWhenEnabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 2,
                SubtitleMethod = SubtitleDeliveryMethod.Embed
            };
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2, IsInfiniteStream = false };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Equal(2, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Embed, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_ClearsLiveTvWhenBurnInDisabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 2,
                SubtitleMethod = SubtitleDeliveryMethod.Encode
            };
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2, IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Null(request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_DoesNotOverrideExistingIndexWhenEnabledForLiveTv()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto
            {
                SubtitleStreamIndex = 5,
                SubtitleMethod = SubtitleDeliveryMethod.Embed
            };
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2, IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Equal(5, request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Embed, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public void ApplySubtitleBurnInPreferences_IgnoresMissingDefaultIndexWhenEnabledForLiveTv(int? defaultIndex)
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto();
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = defaultIndex, IsInfiniteStream = true };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Null(request.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }
}
