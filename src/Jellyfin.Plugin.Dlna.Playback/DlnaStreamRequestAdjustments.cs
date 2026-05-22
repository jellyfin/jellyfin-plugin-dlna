using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Adjusts DLNA stream requests once the probed media source is available.
/// </summary>
public static class DlnaStreamRequestAdjustments
{
    /// <summary>
    /// Clears subtitle selection on browse/play-to stream info when burn-in is disabled.
    /// </summary>
    public static void ApplyBrowseSubtitlePreferences(StreamInfo? streamInfo)
    {
        if (streamInfo is null || DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            return;
        }

        streamInfo.SubtitleStreamIndex = null;
        streamInfo.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
    }

    /// <summary>
    /// Applies or clears subtitle burn-in based on plugin configuration and the probed media source.
    /// </summary>
    public static void ApplySubtitleBurnInPreferences(DlnaStreamState state, MediaSourceInfo? mediaSource)
    {
        ApplySubtitleBurnInPreferences(state.IsVideoRequest, state.VideoRequest, mediaSource);

        if (!DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            state.SubtitleStream = null;
            state.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
        }
    }

    /// <summary>
    /// Applies or clears subtitle burn-in based on plugin configuration and the probed media source.
    /// </summary>
    public static void ApplySubtitleBurnInPreferences(
        bool isVideoRequest,
        VideoRequestDto? videoRequest,
        MediaSourceInfo? mediaSource)
    {
        if (!isVideoRequest || videoRequest is null)
        {
            return;
        }

        if (!DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            videoRequest.SubtitleStreamIndex = null;
            videoRequest.SubtitleMethod = SubtitleDeliveryMethod.Drop;
            return;
        }

        if (mediaSource is null || videoRequest.SubtitleStreamIndex.HasValue)
        {
            return;
        }

        var defaultIndex = mediaSource.DefaultSubtitleStreamIndex;
        if (!defaultIndex.HasValue || defaultIndex.Value < 0)
        {
            return;
        }

        videoRequest.SubtitleStreamIndex = defaultIndex;
        videoRequest.SubtitleMethod = SubtitleDeliveryMethod.Encode;
    }
}
