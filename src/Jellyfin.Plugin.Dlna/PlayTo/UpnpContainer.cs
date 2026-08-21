using System;
using System.Xml.Linq;
using Jellyfin.Plugin.Dlna.Ssdp;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="UpnpContainer" />.
/// </summary>
public class UpnpContainer : UBaseObject
{
    /// <summary>
    /// Create a <see cref="UBaseObject"/>.
    /// </summary>
    /// <param name="container">The <see cref="XElement"/>.</param>
    /// <returns>The <see cref="UBaseObject"/>.</returns>
    public static UBaseObject Create(XElement container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return new UBaseObject
        {
            Id = container.GetAttributeValue(UPnpNamespaces.Id) ?? string.Empty,
            ParentId = container.GetAttributeValue(UPnpNamespaces.ParentId) ?? string.Empty,
            Title = container.GetValue(UPnpNamespaces.Title) ?? string.Empty,
            IconUrl = container.GetValue(UPnpNamespaces.Artwork) ?? string.Empty,
            UpnpClass = container.GetValue(UPnpNamespaces.Class) ?? string.Empty
        };
    }
}
