#nullable disable

#pragma warning disable CA1711

using System.Xml.Serialization;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="XmlAttribute" />.
/// </summary>
public class XmlAttribute
{
    /// <summary>
    /// Gets or sets the name of the attribute.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the attribute.
    /// </summary>
    [XmlAttribute("value")]
    public string Value { get; set; }
}
