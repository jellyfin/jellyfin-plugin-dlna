#nullable disable

using System.Xml.Serialization;

namespace Jellyfin.Plugin.Dlna.Model;

public class HttpHeaderInfo
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("value")]
    public string Value { get; set; }

    [XmlAttribute("match")]
    public HeaderMatchType Match { get; set; }
}
