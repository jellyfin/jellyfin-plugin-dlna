#nullable disable

using System.Xml.Serialization;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="HttpHeaderInfo" />.
/// </summary>
public class HttpHeaderInfo
{
    /// <summary>
    /// The name.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; }

    /// <summary>
    /// The value.
    /// </summary>
    [XmlAttribute("value")]
    public string Value { get; set; }

    /// <summary>
    /// The <see cref="HeaderMatchType"/>.
    /// </summary>
    [XmlAttribute("match")]
    public HeaderMatchType Match { get; set; }
}
