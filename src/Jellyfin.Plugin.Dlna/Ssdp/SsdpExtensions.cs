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
    /// <returns>The value, or <c>null</c> if the element is not present.</returns>
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
    /// <returns>The attribute value, or <c>null</c> if the attribute is not present.</returns>
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
    /// <returns>The descendant value, or <c>null</c> if no descendant matches.</returns>
    public static string? GetDescendantValue(this XElement container, XName name)
        => container.Descendants(name).FirstOrDefault()?.Value;
}
