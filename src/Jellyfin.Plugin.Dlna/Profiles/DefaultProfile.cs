using System;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.Profiles;

/// <summary>
/// Defines the <see cref="DefaultProfile" />.
/// </summary>
[System.Xml.Serialization.XmlRoot("Profile")]
public class DefaultProfile : DlnaDeviceProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProfile"/> class.
    /// </summary>
    public DefaultProfile()
    {
        Id = Guid.NewGuid();
        Name = "Generic Device";

        ProtocolInfo = "http-get:*:video/mpeg:*,http-get:*:video/mp4:*,http-get:*:video/vnd.dlna.mpeg-tts:*,http-get:*:video/avi:*,http-get:*:video/x-matroska:*,http-get:*:video/x-ms-wmv:*,http-get:*:video/wtv:*,http-get:*:audio/mpeg:*,http-get:*:audio/mp3:*,http-get:*:audio/mp4:*,http-get:*:audio/x-ms-wma:*,http-get:*:audio/wav:*,http-get:*:audio/L16:*,http-get:*:image/jpeg:*,http-get:*:image/png:*,http-get:*:image/gif:*,http-get:*:image/tiff:*";

        Manufacturer = "Jellyfin";
        ModelDescription = "UPnP/AV 1.0 Compliant Media Server";
        ModelName = "Jellyfin Server";
        ModelNumber = "01";
        ModelUrl = "https://github.com/jellyfin/jellyfin";
        ManufacturerUrl = "https://github.com/jellyfin/jellyfin";

        AlbumArtPn = "JPEG_SM";

        MaxAlbumArtHeight = 480;
        MaxAlbumArtWidth = 480;

        MaxIconWidth = 48;
        MaxIconHeight = 48;

        MaxStreamingBitrate = 140000000;
        MaxStaticBitrate = 140000000;
        MusicStreamingTranscodingBitrate = 192000;

        EnableAlbumArtInDidl = false;

        TranscodingProfiles =
        [
            new TranscodingProfile
            {
                Container = "mp3",
                AudioCodec = "mp3",
                Type = DlnaProfileType.Audio
            },

            new TranscodingProfile
            {
                Container = "ts",
                Type = DlnaProfileType.Video,
                AudioCodec = "aac",
                VideoCodec = "h264"
            },

            new TranscodingProfile
            {
                Container = "jpeg",
                Type = DlnaProfileType.Photo
            }
        ];

        DirectPlayProfiles =
        [
            new DirectPlayProfile
            {
                // play all
                Container = string.Empty,
                Type = DlnaProfileType.Video
            },

            new DirectPlayProfile
            {
                // play all
                Container = string.Empty,
                Type = DlnaProfileType.Audio
            }
        ];

        SubtitleProfiles =
        [
            new SubtitleProfile
            {
                Format = "srt",
                Method = SubtitleDeliveryMethod.External,
            },

            new SubtitleProfile
            {
                Format = "sub",
                Method = SubtitleDeliveryMethod.External,
            },

            new SubtitleProfile
            {
                Format = "sup",
                Method = SubtitleDeliveryMethod.External
            },

            new SubtitleProfile
            {
                Format = "srt",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "ass",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "ssa",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "smi",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "dvdsub",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "pgs",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "pgssub",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "sub",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "sup",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "subrip",
                Method = SubtitleDeliveryMethod.Embed
            },

            new SubtitleProfile
            {
                Format = "vtt",
                Method = SubtitleDeliveryMethod.Embed
            }
        ];

        ResponseProfiles =
        [
            new ResponseProfile
            {
                Container = "m4v",
                Type = DlnaProfileType.Video,
                MimeType = "video/mp4"
            }
        ];
    }
}
