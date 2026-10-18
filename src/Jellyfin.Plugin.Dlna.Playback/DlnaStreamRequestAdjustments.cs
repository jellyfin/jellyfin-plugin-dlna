using System;
using System.Collections.Generic;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Adjusts DLNA stream requests once the probed media source is available.
/// </summary>
public static class DlnaStreamRequestAdjustments
{
    /// <summary>
    /// Returns whether the media source represents a live TV video stream.
    /// Live radio and other audio-only infinite streams do not qualify.
    /// </summary>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    /// <returns>True when the source is live TV video; otherwise false.</returns>
    public static bool IsLiveTv(MediaSourceInfo? mediaSource, MediaType? itemMediaType = null)
    {
        if (mediaSource?.IsInfiniteStream != true || itemMediaType == MediaType.Audio)
        {
            return false;
        }

        // A probed source that has streams but no video is live radio, or an M3U entry with no video.
        // An empty stream list has not been probed yet, so live TV browse can still advertise a transcode.
        var streams = mediaSource.MediaStreams;
        if (streams is not { Count: > 0 })
        {
            return true;
        }

        foreach (var stream in streams)
        {
            if (stream.Type == MediaStreamType.Video)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether DLNA subtitle burn-in should be applied for the given media source.
    /// </summary>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    /// <returns>True when burn-in is enabled and the source is live TV video; otherwise false.</returns>
    public static bool ShouldBurnInSubtitles(MediaSourceInfo? mediaSource, MediaType? itemMediaType = null)
        => DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn
           && IsLiveTv(mediaSource, itemMediaType);

    /// <summary>
    /// Returns whether burn-in can run: the setting is on, the source is live TV video, and the default user still exists.
    /// </summary>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="userManager">The user manager used to resolve the default user.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    /// <returns>True when a live TV transcode should burn in subtitles.</returns>
    public static bool CanBurnInSubtitles(MediaSourceInfo? mediaSource, IUserManager userManager, MediaType? itemMediaType = null)
        => ShouldBurnInSubtitles(mediaSource, itemMediaType) && HasDefaultUser(userManager);

    /// <summary>
    /// Returns whether the configured DLNA default user still exists.
    /// </summary>
    /// <param name="userManager">The user manager used to resolve the default user.</param>
    /// <returns>True when a default user id is set and that user can be loaded.</returns>
    public static bool HasDefaultUser(IUserManager userManager)
    {
        ArgumentNullException.ThrowIfNull(userManager);

        var defaultUserId = DlnaPluginConfigurationAccessor.DefaultUserId;
        if (defaultUserId is null || defaultUserId.Value.Equals(default))
        {
            return false;
        }

        return userManager.GetUserById(defaultUserId.Value) is not null;
    }

    /// <summary>
    /// Logs when burn-in is enabled for live TV but the default user is missing, so the server does not fail silently.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="userManager">The user manager used to resolve the default user.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    public static void WarnMissingBurnInUser(ILogger logger, MediaSourceInfo? mediaSource, IUserManager userManager, MediaType? itemMediaType = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (ShouldBurnInSubtitles(mediaSource, itemMediaType) && !HasDefaultUser(userManager))
        {
            logger.LogWarning("Live TV subtitle burn-in is enabled, but the DLNA default user is missing.");
        }
    }

    /// <summary>
    /// Tells the stream builder to emit a transcode. Burn-in cannot run inside a direct stream,
    /// and a source with no video codec would otherwise be copied.
    /// </summary>
    /// <param name="options">The media options passed to the stream builder.</param>
    public static void RequireBurnInTranscode(MediaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.EnableDirectPlay = false;
        options.EnableDirectStream = false;
        options.AllowVideoStreamCopy = false;
    }

    /// <summary>
    /// Copies a live media source so subtitle selection can mutate the copy.
    /// The open live stream is shared by every viewer, and
    /// <see cref="IMediaSourceManager.SetDefaultAudioAndSubtitleStreamIndices"/> writes the default
    /// subtitle index and subtitle scores onto the instance it is given.
    /// </summary>
    /// <param name="mediaSource">The shared live media source.</param>
    /// <returns>A deep copy of <paramref name="mediaSource"/> and its streams.</returns>
    public static MediaSourceInfo CopyForSubtitleSelection(MediaSourceInfo mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);

        var json = JsonSerializer.Serialize(mediaSource);
        return JsonSerializer.Deserialize<MediaSourceInfo>(json)
            ?? throw new InvalidOperationException("The live media source could not be copied.");
    }

    /// <summary>
    /// Picks a video codec that ffmpeg will encode. <c>copy</c>, including the fallback
    /// <c>EncodingHelper</c> uses when the source has no video codec, cannot burn subtitles in.
    /// </summary>
    /// <param name="outputVideoCodec">The codec selected for the output, if any.</param>
    /// <param name="supportedVideoCodecs">Codecs the device asked for, if any.</param>
    /// <returns>A codec other than <c>copy</c>.</returns>
    public static string SelectBurnInVideoCodec(string? outputVideoCodec, IReadOnlyList<string>? supportedVideoCodecs)
    {
        if (IsEncodableVideoCodec(outputVideoCodec))
        {
            return outputVideoCodec!;
        }

        if (supportedVideoCodecs is not null)
        {
            foreach (var codec in supportedVideoCodecs)
            {
                if (IsEncodableVideoCodec(codec))
                {
                    return codec;
                }
            }
        }

        return "h264";
    }

    /// <summary>
    /// Replaces a missing or copied video codec once burn-in has chosen a subtitle track.
    /// </summary>
    /// <param name="state">The DLNA stream state whose output codec may still be <c>copy</c>.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
    public static void EnsureBurnInVideoIsEncoded(DlnaStreamState state, MediaSourceInfo? mediaSource)
    {
        ArgumentNullException.ThrowIfNull(state);

        var videoRequest = state.VideoRequest;
        if (videoRequest is null
            || !ShouldBurnInSubtitles(mediaSource)
            || videoRequest.SubtitleMethod != SubtitleDeliveryMethod.Encode
            || videoRequest.SubtitleStreamIndex is null or < 0)
        {
            return;
        }

        videoRequest.AllowVideoStreamCopy = false;
        var codec = SelectBurnInVideoCodec(state.OutputVideoCodec, state.SupportedVideoCodecs);
        state.OutputVideoCodec = codec;
        videoRequest.VideoCodec = codec;
    }

    /// <summary>
    /// Clears live TV subtitle selection on browse/play-to stream info when burn-in is disabled.
    /// On-demand video is never touched.
    /// </summary>
    /// <param name="streamInfo">The browse or play-to stream info to adjust.</param>
    /// <param name="mediaSource">The media source used to detect live TV. Falls back to <see cref="StreamInfo.MediaSource"/>.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    public static void ApplyBrowseSubtitlePreferences(StreamInfo? streamInfo, MediaSourceInfo? mediaSource = null, MediaType? itemMediaType = null)
    {
        if (streamInfo is null)
        {
            return;
        }

        var source = mediaSource ?? streamInfo.MediaSource;
        if (!IsLiveTv(source, itemMediaType) || DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            return;
        }

        streamInfo.SubtitleStreamIndex = null;
        streamInfo.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop;
    }

    /// <summary>
    /// Applies or clears live TV subtitle burn-in on the stream request based on plugin configuration.
    /// </summary>
    /// <param name="state">The DLNA stream state to adjust.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    public static void ApplySubtitleBurnInPreferences(DlnaStreamState state, MediaSourceInfo? mediaSource, MediaType? itemMediaType = null)
        => ApplySubtitleBurnInPreferences(state.IsVideoRequest, state.VideoRequest, mediaSource, itemMediaType);

    /// <summary>
    /// Applies or clears live TV subtitle burn-in on the stream request based on plugin configuration.
    /// On-demand video is never touched.
    /// </summary>
    /// <param name="isVideoRequest">Whether the request is a video stream request.</param>
    /// <param name="videoRequest">The video request DTO to adjust.</param>
    /// <param name="mediaSource">The probed media source, if any.</param>
    /// <param name="itemMediaType">The item media type, when known. Audio never qualifies.</param>
    public static void ApplySubtitleBurnInPreferences(
        bool isVideoRequest,
        VideoRequestDto? videoRequest,
        MediaSourceInfo? mediaSource,
        MediaType? itemMediaType = null)
    {
        if (!isVideoRequest || videoRequest is null || mediaSource is null || !IsLiveTv(mediaSource, itemMediaType))
        {
            return;
        }

        if (!DlnaPluginConfigurationAccessor.EnableSubtitleBurnIn)
        {
            videoRequest.SubtitleStreamIndex = null;
            videoRequest.SubtitleMethod = SubtitleDeliveryMethod.Drop;
            return;
        }

        if (videoRequest.SubtitleStreamIndex.HasValue)
        {
            if (videoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode)
            {
                videoRequest.AllowVideoStreamCopy = false;
            }

            return;
        }

        var defaultIndex = mediaSource.DefaultSubtitleStreamIndex;
        if (!defaultIndex.HasValue || defaultIndex.Value < 0)
        {
            return;
        }

        videoRequest.SubtitleStreamIndex = defaultIndex;
        videoRequest.SubtitleMethod = SubtitleDeliveryMethod.Encode;
        videoRequest.AllowVideoStreamCopy = false;
    }

    private static bool IsEncodableVideoCodec(string? codec)
        => !string.IsNullOrEmpty(codec)
           && !string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase);
}
