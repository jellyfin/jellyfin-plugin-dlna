using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using StreamInfo = MediaBrowser.Model.Dlna.StreamInfo;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaStreamRequestAdjustmentsTests
{
    [Fact]
    public void ApplyBrowseSubtitlePreferences_LeavesStreamInfoWhenEnabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo);

            Assert.Equal(2, streamInfo.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplyBrowseSubtitlePreferences_ClearsStreamInfoWhenDisabled()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var streamInfo = new StreamInfo { DeviceProfile = new DeviceProfile(), SubtitleStreamIndex = 2 };

            DlnaStreamRequestAdjustments.ApplyBrowseSubtitlePreferences(streamInfo);

            Assert.Null(streamInfo.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, streamInfo.SubtitleDeliveryMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_ClearsExistingIndexWhenDisabled()
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

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, new MediaSourceInfo());

            Assert.Null(request.SubtitleStreamIndex);
            Assert.Equal(SubtitleDeliveryMethod.Drop, request.SubtitleMethod);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_DoesNothingWhenDisabledAndNoIndex()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = false;
            var request = new VideoRequestDto();

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, new MediaSourceInfo());

            Assert.Null(request.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }

    [Fact]
    public void ApplySubtitleBurnInPreferences_SetsEncodeWhenEnabledAndDefaultIndexPresent()
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto();
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2 };

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
    public void ApplySubtitleBurnInPreferences_DoesNotOverrideExistingIndexWhenEnabled()
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
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = 2 };

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
    public void ApplySubtitleBurnInPreferences_IgnoresMissingDefaultIndexWhenEnabled(int? defaultIndex)
    {
        var previous = DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn;
        try
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = true;
            var request = new VideoRequestDto();
            var mediaSource = new MediaSourceInfo { DefaultSubtitleStreamIndex = defaultIndex };

            DlnaStreamRequestAdjustments.ApplySubtitleBurnInPreferences(true, request, mediaSource);

            Assert.Null(request.SubtitleStreamIndex);
        }
        finally
        {
            DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn = previous;
        }
    }
}
