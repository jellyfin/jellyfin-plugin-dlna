using System.Xml.Serialization;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="HttpHeaderInfo" />.
/// </summary>
public class HttpHeaderInfo
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="HeaderMatchType"/>.
    /// </summary>
    [XmlAttribute("match")]
    public HeaderMatchType Match { get; set; }
}
