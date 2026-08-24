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
    /// Returns whether the media source represents a live TV stream.
    /// </summary>
    public static bool IsLiveTv(MediaSourceInfo? mediaSource)
        => mediaSource?.IsInfiniteStream == true;

    /// <summary>
    /// Returns whether DLNA subtitle burn-in should be applied for the given media source.
    /// </summary>
    public static bool ShouldBurnInSubtitles(MediaSourceInfo? mediaSource)
        => DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn
           && IsLiveTv(mediaSource);

    /// <summary>
    /// Clears live TV subtitle selection on browse/play-to stream info when burn-in is disabled.
    /// On-demand video is left on default direct-play behavior.
    /// </summary>
    public static void ApplyBrowseSubtitlePreferences(StreamInfo? streamInfo, MediaSourceInfo? mediaSource = null)
    {
        if (streamInfo is null)
        {
            return;
        }

        if (mediaSource is not null)
        {
            if (!IsLiveTv(mediaSource))
            {
                return;
            }

            if (ShouldBurnInSubtitles(mediaSource))
            {
                return;
            }

            streamInfo.SubtitleStreamIndex = null;
            streamInfo.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
            return;
        }

        if (!DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            streamInfo.SubtitleStreamIndex = null;
            streamInfo.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
        }
    }

    /// <summary>
    /// Applies or clears live TV subtitle burn-in based on plugin configuration and the probed media source.
    /// </summary>
    public static void ApplySubtitleBurnInPreferences(DlnaStreamState state, MediaSourceInfo? mediaSource)
    {
        if (!IsLiveTv(mediaSource))
        {
            return;
        }

        ApplySubtitleBurnInPreferences(state.IsVideoRequest, state.VideoRequest, mediaSource);

        if (!ShouldBurnInSubtitles(mediaSource))
        {
            state.SubtitleStream = null;
            state.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
        }
    }

    /// <summary>
    /// Applies or clears live TV subtitle burn-in based on plugin configuration and the probed media source.
    /// </summary>
    public static void ApplySubtitleBurnInPreferences(
        bool isVideoRequest,
        VideoRequestDto? videoRequest,
        MediaSourceInfo? mediaSource)
    {
        if (!isVideoRequest || videoRequest is null || !IsLiveTv(mediaSource))
        {
            return;
        }

        if (!ShouldBurnInSubtitles(mediaSource))
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
