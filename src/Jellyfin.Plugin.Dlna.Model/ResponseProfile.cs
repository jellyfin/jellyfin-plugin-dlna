#pragma warning disable CA1819 // Properties should not return arrays
#nullable disable

using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="ResponseProfile" />.
/// </summary>
public class ResponseProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseProfile"/> class.
    /// </summary>
    public ResponseProfile()
    {
        Conditions = [];
    }

    /// <summary>
    /// Gets or sets the container.
    /// </summary>
    [XmlAttribute("container")]
    public string Container { get; set; }

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    [XmlAttribute("audioCodec")]
    public string AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the video codec.
    /// </summary>
    [XmlAttribute("videoCodec")]
    public string VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    [XmlAttribute("type")]
    public DlnaProfileType Type { get; set; }

    /// <summary>
    /// Gets or sets the orgPn.
    /// </summary>
    [XmlAttribute("orgPn")]
    public string OrgPn { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    [XmlAttribute("mimeType")]
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the conditions.
    /// </summary>
    public ProfileCondition[] Conditions { get; set; }

    /// <summary>
    /// Gets the containers.
    /// </summary>
    public string[] GetContainers()
        => ContainerHelper.Split(Container);

    /// <summary>
    /// Gets the audio codecs.
    /// </summary>
    public string[] GetAudioCodecs()
        => ContainerHelper.Split(AudioCodec);

    /// <summary>
    /// Gets the video codecs.
    /// </summary>
    public string[] GetVideoCodecs()
        => ContainerHelper.Split(VideoCodec);
}
