#nullable disable

using System;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;

namespace Jellyfin.Plugin.Dlna.Model;

public class ResponseProfile
{
    public ResponseProfile()
    {
        Conditions = Array.Empty<ProfileCondition>();
    }

    [XmlAttribute("container")]
    public string Container { get; set; }

    [XmlAttribute("audioCodec")]
    public string AudioCodec { get; set; }

    [XmlAttribute("videoCodec")]
    public string VideoCodec { get; set; }

    [XmlAttribute("type")]
    public DlnaProfileType Type { get; set; }

    [XmlAttribute("orgPn")]
    public string OrgPn { get; set; }

    [XmlAttribute("mimeType")]
    public string MimeType { get; set; }

    public ProfileCondition[] Conditions { get; set; }

    public string[] GetContainers()
        => ContainerHelper.Split(Container);

    public string[] GetAudioCodecs()
        => ContainerHelper.Split(AudioCodec);

    public string[] GetVideoCodecs()
        => ContainerHelper.Split(VideoCodec);
}
