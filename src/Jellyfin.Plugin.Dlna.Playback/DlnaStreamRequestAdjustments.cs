using System;
using System.Linq;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Adjusts DLNA stream requests once the probed media source is available.
/// </summary>
public static class DlnaStreamRequestAdjustments
{
    /// <summary>
    /// Analyze duration used when a cached live probe has no real video size.
    /// </summary>
    public const int LiveReprobeAnalyzeDurationMs = 10000;

    /// <summary>
    /// Live TV browse URLs often omit MaxWidth/MaxHeight. Without caps, playback upgrades to native
    /// 4K when bitrate allows it. Limit DLNA live transcodes to 1080p.
    /// </summary>
    /// <param name="state">The DLNA stream state to adjust.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
    public static void CapLiveStreamTranscodeResolution(DlnaStreamState state, MediaSourceInfo? mediaSource)
        => CapLiveStreamTranscodeResolution(state.VideoRequest, mediaSource);

    /// <summary>
    /// Caps live stream transcode resolution to 1080p when width/height are missing or higher.
    /// </summary>
    /// <param name="videoRequest">The video request DTO to adjust.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
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

    /// <summary>
    /// Returns whether a live source has no positive video width and height.
    /// </summary>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <returns>True when the source is live and its video size is missing.</returns>
    public static bool NeedsFreshLiveProbe(MediaSourceInfo? mediaSource)
        => mediaSource is not null && mediaSource.IsInfiniteStream && !HasUsableVideoSize(mediaSource);

    /// <summary>
    /// Returns whether the primary video stream has a positive width and height.
    /// </summary>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <returns>True when width and height are both greater than zero.</returns>
    public static bool HasUsableVideoSize(MediaSourceInfo? mediaSource)
    {
        var video = GetPrimaryVideoStream(mediaSource);
        return video?.Width is > 0 && video.Height is > 0;
    }

    /// <summary>
    /// Copies a probe onto the live source when the probe contains a real video size.
    /// </summary>
    /// <param name="target">The live source to update.</param>
    /// <param name="probed">The newly probed media info, if any.</param>
    /// <returns>True when the probed video size replaced the previous streams.</returns>
    public static bool TryApplyProbedLiveMediaInfo(MediaSourceInfo target, MediaSourceInfo? probed)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!HasUsableVideoSize(probed))
        {
            return false;
        }

        target.MediaStreams = probed!.MediaStreams;
        if (!string.IsNullOrWhiteSpace(probed.Container))
        {
            target.Container = probed.Container;
        }

        if (probed.Bitrate.HasValue)
        {
            target.Bitrate = probed.Bitrate;
        }

        if (probed.Formats is not null)
        {
            target.Formats = probed.Formats;
        }

        if (probed.DefaultAudioStreamIndex.HasValue)
        {
            target.DefaultAudioStreamIndex = probed.DefaultAudioStreamIndex;
        }

        return true;
    }

    private static MediaStream? GetPrimaryVideoStream(MediaSourceInfo? mediaSource)
        => mediaSource?.MediaStreams?.FirstOrDefault(stream => stream.Type == MediaStreamType.Video);
}
