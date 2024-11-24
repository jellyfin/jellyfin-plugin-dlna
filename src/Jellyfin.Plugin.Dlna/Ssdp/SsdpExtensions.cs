using System.Linq;
using System.Xml.Linq;

namespace Jellyfin.Plugin.Dlna.Ssdp;

/// <summary>
/// Defines the <see cref="SsdpExtensions" />.
/// </summary>
public static class SsdpExtensions
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="container">The <see cref="XElement"/>.</param>
    /// <param name="name">The <see cref="XName"/>.</param>
    public static string? GetValue(this XElement container, XName name)
    {
        var node = container.Element(name);

        return node?.Value;
    }

    /// <summary>
    /// Gets the attribute value.
    /// </summary>
    /// <param name="container">The <see cref="XElement"/>.</param>
    /// <param name="name">The <see cref="XName"/>.</param>
    public static string? GetAttributeValue(this XElement container, XName name)
    {
        var node = container.Attribute(name);

        return node?.Value;
    }

    /// <summary>
    /// Gets the descendant value.
    /// </summary>
    /// <param name="container">The <see cref="XElement"/>.</param>
    /// <param name="name">The <see cref="XName"/>.</param>
    public static string? GetDescendantValue(this XElement container, XName name)
        => container.Descendants(name).FirstOrDefault()?.Value;
}
