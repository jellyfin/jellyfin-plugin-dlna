using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.Dlna.Model;

[XmlRoot("Profile")]
public class DlnaDeviceProfile : DeviceProfile
{
    /// <summary>
    /// Gets or sets the Identification.
    /// </summary>
    public DeviceIdentification? Identification { get; set; }

    /// <summary>
    /// Gets or sets the friendly name of the device profile, which can be shown to users.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer of the device which this profile represents.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets an url for the manufacturer of the device which this profile represents.
    /// </summary>
    public string? ManufacturerUrl { get; set; }

    /// <summary>
    /// Gets or sets the model name of the device which this profile represents.
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the model description of the device which this profile represents.
    /// </summary>
    public string? ModelDescription { get; set; }

    /// <summary>
    /// Gets or sets the model number of the device which this profile represents.
    /// </summary>
    public string? ModelNumber { get; set; }

    /// <summary>
    /// Gets or sets the ModelUrl.
    /// </summary>
    public string? ModelUrl { get; set; }

    /// <summary>
    /// Gets or sets the serial number of the device which this profile represents.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether EnableAlbumArtInDidl.
    /// </summary>
    [DefaultValue(false)]
    public bool EnableAlbumArtInDidl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether EnableSingleAlbumArtLimit.
    /// </summary>
    [DefaultValue(false)]
    public bool EnableSingleAlbumArtLimit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether EnableSingleSubtitleLimit.
    /// </summary>
    [DefaultValue(false)]
    public bool EnableSingleSubtitleLimit { get; set; }

    /// <summary>
    /// Gets or sets the SupportedMediaTypes.
    /// </summary>
    public string SupportedMediaTypes { get; set; } = "Audio,Photo,Video";

    /// <summary>
    /// Gets or sets the UserId.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the AlbumArtPn.
    /// </summary>
    public string? AlbumArtPn { get; set; }

    /// <summary>
    /// Gets or sets the MaxAlbumArtWidth.
    /// </summary>
    public int? MaxAlbumArtWidth { get; set; }

    /// <summary>
    /// Gets or sets the MaxAlbumArtHeight.
    /// </summary>
    public int? MaxAlbumArtHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed width of embedded icons.
    /// </summary>
    public int? MaxIconWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed height of embedded icons.
    /// </summary>
    public int? MaxIconHeight { get; set; }

    /// <summary>
    /// Gets or sets the content of the aggregationFlags element in the urn:schemas-sonycom:av namespace.
    /// </summary>
    public string? SonyAggregationFlags { get; set; }

    /// <summary>
    /// Gets or sets the ProtocolInfo.
    /// </summary>
    public string? ProtocolInfo { get; set; }

    /// <summary>
    /// Gets or sets the TimelineOffsetSeconds.
    /// </summary>
    [DefaultValue(0)]
    public int TimelineOffsetSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether RequiresPlainVideoItems.
    /// </summary>
    [DefaultValue(false)]
    public bool RequiresPlainVideoItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether RequiresPlainFolders.
    /// </summary>
    [DefaultValue(false)]
    public bool RequiresPlainFolders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether EnableMSMediaReceiverRegistrar.
    /// </summary>
    [DefaultValue(false)]
    public bool EnableMSMediaReceiverRegistrar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether IgnoreTranscodeByteRangeRequests.
    /// </summary>
    [DefaultValue(false)]
    public bool IgnoreTranscodeByteRangeRequests { get; set; }

    /// <summary>
    /// Gets or sets the XmlRootAttributes.
    /// </summary>
    public XmlAttribute[] XmlRootAttributes { get; set; } = Array.Empty<XmlAttribute>();

    /// <summary>
    /// Gets or sets the ResponseProfiles.
    /// </summary>
    public ResponseProfile[] ResponseProfiles { get; set; } = Array.Empty<ResponseProfile>();

    /// <summary>
    /// The GetSupportedMediaTypes.
    /// </summary>
    /// <returns>The .</returns>
    public MediaType[] GetSupportedMediaTypes()
    {
        return ContainerHelper.Split(SupportedMediaTypes)
            .Select(m => Enum.TryParse<MediaType>(m, out var parsed) ? parsed : MediaType.Unknown)
            .Where(m => m != MediaType.Unknown)
            .ToArray();
    }

    /// <summary>
    /// Gets the audio transcoding profile.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="audioCodec">The audio Codec.</param>
    /// <returns>A <see cref="TranscodingProfile"/>.</returns>
    public TranscodingProfile? GetAudioTranscodingProfile(string? container, string? audioCodec)
    {
        container = (container ?? string.Empty).TrimStart('.');

        return TranscodingProfiles
            .Where(i => i.Type == DlnaProfileType.Audio)
            .Where(i => string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(i => ContainerHelper.ContainsContainer(i.AudioCodec, audioCodec));
    }

    /// <summary>
    /// Gets the video transcoding profile.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="audioCodec">The audio Codec.</param>
    /// <param name="videoCodec">The video Codec.</param>
    /// <returns>The <see cref="TranscodingProfile"/>.</returns>
    public TranscodingProfile? GetVideoTranscodingProfile(string? container, string? audioCodec, string? videoCodec)
    {
        container = (container ?? string.Empty).TrimStart('.');

        return TranscodingProfiles
            .Where(i => i.Type == DlnaProfileType.Video)
            .Where(i => string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
            .Where(i => ContainerHelper.ContainsContainer(i.AudioCodec, audioCodec))
            .FirstOrDefault(i => string.Equals(videoCodec, i.VideoCodec, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the audio media profile.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="audioCodec">The audio codec.</param>
    /// <param name="audioChannels">The audio channels.</param>
    /// <param name="audioBitrate">The audio bitrate.</param>
    /// <param name="audioSampleRate">The audio sample rate.</param>
    /// <param name="audioBitDepth">The audio bit depth.</param>
    /// <returns>The <see cref="ResponseProfile"/>.</returns>
    public ResponseProfile? GetAudioMediaProfile(string? container, string? audioCodec, int? audioChannels, int? audioBitrate, int? audioSampleRate, int? audioBitDepth)
    {
        foreach (var i in ResponseProfiles)
        {
            if (i.Type != DlnaProfileType.Audio)
            {
                continue;
            }

            if (!ContainerHelper.ContainsContainer(i.Container, container))
            {
                continue;
            }

            var audioCodecs = i.GetAudioCodecs();
            if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var anyOff = false;
            foreach (ProfileCondition c in i.Conditions)
            {
                if (!ConditionProcessor.IsAudioConditionSatisfied(GetModelProfileCondition(c), audioChannels, audioBitrate, audioSampleRate, audioBitDepth))
                {
                    anyOff = true;
                    break;
                }
            }

            if (anyOff)
            {
                continue;
            }

            return i;
        }

        return null;
    }

    /// <summary>
    /// Gets the image media profile.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The <see cref="ResponseProfile"/>.</returns>
    public ResponseProfile? GetImageMediaProfile(string container, int? width, int? height)
    {
        foreach (var i in ResponseProfiles)
        {
            if (i.Type != DlnaProfileType.Photo)
            {
                continue;
            }

            if (!ContainerHelper.ContainsContainer(i.Container, container))
            {
                continue;
            }

            var anyOff = false;
            foreach (var c in i.Conditions)
            {
                if (!ConditionProcessor.IsImageConditionSatisfied(GetModelProfileCondition(c), width, height))
                {
                    anyOff = true;
                    break;
                }
            }

            if (anyOff)
            {
                continue;
            }

            return i;
        }

        return null;
    }

    /// <summary>
    /// Gets the video media profile.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="audioCodec">The audio codec.</param>
    /// <param name="videoCodec">The video codec.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="bitDepth">The bit depth.</param>
    /// <param name="videoBitrate">The video bitrate.</param>
    /// <param name="videoProfile">The video profile.</param>
    /// <param name="videoRangeType">The video range type.</param>
    /// <param name="videoLevel">The video level.</param>
    /// <param name="videoFramerate">The video framerate.</param>
    /// <param name="packetLength">The packet length.</param>
    /// <param name="timestamp">The timestamp<see cref="TransportStreamTimestamp"/>.</param>
    /// <param name="isAnamorphic">True if anamorphic.</param>
    /// <param name="isInterlaced">True if interlaced.</param>
    /// <param name="refFrames">The ref frames.</param>
    /// <param name="numVideoStreams">The number of video streams.</param>
    /// <param name="numAudioStreams">The number of audio streams.</param>
    /// <param name="numStreams">The number of streams.</param>
    /// <param name="videoCodecTag">The video Codec tag.</param>
    /// <param name="isAvc">True if Avc.</param>
    /// <returns>The <see cref="ResponseProfile"/>.</returns>
    public ResponseProfile? GetVideoMediaProfile(
        string? container,
        string? audioCodec,
        string? videoCodec,
        int? width,
        int? height,
        int? bitDepth,
        int? videoBitrate,
        string? videoProfile,
        VideoRangeType videoRangeType,
        double? videoLevel,
        float? videoFramerate,
        int? packetLength,
        TransportStreamTimestamp timestamp,
        bool? isAnamorphic,
        bool? isInterlaced,
        int? refFrames,
        int? numVideoStreams,
        int? numAudioStreams,
        int numStreams,
        string? videoCodecTag,
        bool? isAvc)
    {
        foreach (var i in ResponseProfiles)
        {
            if (i.Type != DlnaProfileType.Video)
            {
                continue;
            }

            if (!ContainerHelper.ContainsContainer(i.Container, container))
            {
                continue;
            }

            var audioCodecs = i.GetAudioCodecs();
            if (audioCodecs.Length > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var videoCodecs = i.GetVideoCodecs();
            if (videoCodecs.Length > 0 && !videoCodecs.Contains(videoCodec ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var anyOff = false;
            foreach (ProfileCondition c in i.Conditions)
            {
                if (!ConditionProcessor.IsVideoConditionSatisfied(
                        GetModelProfileCondition(c),
                        width,
                        height,
                        bitDepth,
                        videoBitrate,
                        videoProfile,
                        videoRangeType,
                        videoLevel,
                        videoFramerate,
                        packetLength,
                        timestamp,
                        isAnamorphic,
                        isInterlaced,
                        refFrames,
                        numStreams,
                        numVideoStreams,
                        numAudioStreams,
                        videoCodecTag,
                        isAvc))
                {
                    anyOff = true;
                    break;
                }
            }

            if (anyOff)
            {
                continue;
            }

            return i;
        }

        return null;
    }

    private static ProfileCondition GetModelProfileCondition(ProfileCondition c)
    {
        return new ProfileCondition
        {
            Condition = c.Condition,
            IsRequired = c.IsRequired,
            Property = c.Property,
            Value = c.Value
        };
    }
}
