using System;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the MIME types mandated by the DLNA guidelines, which for some containers differ from the
/// generic ones returned by <see cref="MediaBrowser.Model.Net.MimeTypes"/>.
/// </summary>
public static class DlnaMimeTypes
{
    /// <summary>
    /// The MIME type for MPEG-2 transport streams that carry a timestamp, i.e. the media format profiles
    /// with a <c>_T</c> suffix and those with no suffix at all.
    /// </summary>
    public const string Mpeg2TransportStream = "video/vnd.dlna.mpeg-tts";

    /// <summary>
    /// The MIME type for MPEG program streams and for timestamp free MPEG-2 transport streams, i.e. the
    /// media format profiles with an <c>_ISO</c> suffix.
    /// </summary>
    public const string MpegProgramStream = "video/mpeg";

    /// <summary>
    /// The MIME type for AAC in an ADTS container.
    /// </summary>
    public const string AacAdts = "audio/vnd.dlna.adts";

    /// <summary>
    /// Gets the MIME type DLNA mandates for a video container.
    /// </summary>
    /// <remarks>
    /// The containers are the ones handled by <see cref="MediaFormatProfileResolver.ResolveVideoFormat"/>, and the
    /// timestamp is evaluated the same way, so that the MIME type of a resource cannot contradict its DLNA.ORG_PN.
    /// </remarks>
    /// <param name="container">The container.</param>
    /// <param name="timestamp">The <see cref="TransportStreamTimestamp"/> of the stream.</param>
    /// <returns>The MIME type, or <c>null</c> if DLNA does not mandate one for <paramref name="container"/>.</returns>
    public static string? GetVideoMimeType(string? container, TransportStreamTimestamp timestamp)
    {
        if (string.IsNullOrEmpty(container))
        {
            return null;
        }

        // "mts" is not resolved to a media format profile, but it is the same AVCHD container as "m2ts" and
        // would otherwise be served as the bogus "model/vnd.mts".
        if (string.Equals(container, "mpeg2ts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(container, "mpegts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(container, "m2ts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(container, "mts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(container, "ts", StringComparison.OrdinalIgnoreCase))
        {
            // A transport stream without a timestamp resolves to an _ISO profile, which is served as video/mpeg.
            // A valid or a zeroed timestamp resolves to a _T or an unsuffixed profile, both of which are tts.
            return timestamp == TransportStreamTimestamp.None ? MpegProgramStream : Mpeg2TransportStream;
        }

        if (string.Equals(container, "mpeg2ps", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(container, "mpeg1video", StringComparison.OrdinalIgnoreCase))
        {
            return MpegProgramStream;
        }

        return null;
    }

    /// <summary>
    /// Gets the MIME type DLNA mandates for an audio stream.
    /// </summary>
    /// <remarks>
    /// The MIME type is derived from the media format profile the stream resolves to, so that it cannot
    /// contradict the DLNA.ORG_PN built from the same profile. The parameters are the ones
    /// <see cref="MediaFormatProfileResolver.ResolveAudioFormat"/> takes.
    /// </remarks>
    /// <param name="container">The container.</param>
    /// <param name="bitrate">The bitrate.</param>
    /// <param name="frequency">The sample rate.</param>
    /// <param name="channels">The channel count.</param>
    /// <returns>The MIME type, or <c>null</c> if DLNA does not mandate one for the stream.</returns>
    public static string? GetAudioMimeType(string? container, int? bitrate, int? frequency, int? channels)
    {
        var formatProfile = MediaFormatProfileResolver.ResolveAudioFormat(container, bitrate, frequency, channels);

        return formatProfile is null ? null : GetAudioMimeType(formatProfile.Value);
    }

    /// <summary>
    /// Gets the MIME type DLNA mandates for an audio media format profile.
    /// </summary>
    /// <param name="formatProfile">The <see cref="MediaFormatProfile"/>.</param>
    /// <returns>The MIME type, or <c>null</c> if <paramref name="formatProfile"/> is not an audio profile.</returns>
    public static string? GetAudioMimeType(MediaFormatProfile formatProfile)
        => formatProfile switch
        {
            MediaFormatProfile.MP3 => "audio/mpeg",
            MediaFormatProfile.WMA_BASE or MediaFormatProfile.WMA_FULL => "audio/x-ms-wma",
            MediaFormatProfile.AAC_ISO or MediaFormatProfile.AAC_ISO_320 => "audio/mp4",
            MediaFormatProfile.AAC_ADTS or MediaFormatProfile.AAC_ADTS_320 => AacAdts,

            // The rate and the channel count are part of the MIME type, and both are already encoded in the profile.
            MediaFormatProfile.LPCM16_44_MONO => "audio/L16;rate=44100;channels=1",
            MediaFormatProfile.LPCM16_44_STEREO => "audio/L16;rate=44100;channels=2",
            MediaFormatProfile.LPCM16_48_MONO => "audio/L16;rate=48000;channels=1",
            MediaFormatProfile.LPCM16_48_STEREO => "audio/L16;rate=48000;channels=2",

            // Not DLNA profiles, but the MIME types the bundled device profiles advertise for them.
            MediaFormatProfile.FLAC => "audio/flac",
            MediaFormatProfile.OGG => "audio/ogg",
            _ => null
        };
}
