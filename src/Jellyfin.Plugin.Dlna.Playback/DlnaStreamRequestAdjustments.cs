using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Adjusts DLNA stream requests once the probed media source is available.
/// </summary>
public static class DlnaStreamRequestAdjustments
{
    /// <summary>
    /// Live TV browse URLs often omit MaxWidth/MaxHeight. Without caps, playback upgrades to native
    /// 4K when bitrate allows it. Limit DLNA live transcodes to 1080p.
    /// </summary>
    public static void CapLiveStreamTranscodeResolution(DlnaStreamState state, MediaSourceInfo? mediaSource)
        => CapLiveStreamTranscodeResolution(state.VideoRequest, mediaSource);

    /// <summary>
    /// Caps live stream transcode resolution to 1080p when width/height are missing or higher.
    /// </summary>
    public static void CapLiveStreamTranscodeResolution(VideoRequestDto? videoRequest, MediaSourceInfo? mediaSource)
    {
        if (videoRequest is null || mediaSource is null || !mediaSource.IsInfiniteStream)
        {
            return;
        }

        const int maxWidth = 1920;
        const int maxHeight = 1080;

        if (!videoRequest.MaxWidth.HasValue || videoRequest.MaxWidth.Value > maxWidth)
        {
            videoRequest.MaxWidth = maxWidth;
        }

        if (!videoRequest.MaxHeight.HasValue || videoRequest.MaxHeight.Value > maxHeight)
        {
            videoRequest.MaxHeight = maxHeight;
        }
    }
}
