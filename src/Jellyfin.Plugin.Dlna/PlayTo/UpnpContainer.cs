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
    public static UBaseObject Create(XElement container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return new UBaseObject
        {
            Id = container.GetAttributeValue(UPnpNamespaces.Id),
            ParentId = container.GetAttributeValue(UPnpNamespaces.ParentId),
            Title = container.GetValue(UPnpNamespaces.Title),
            IconUrl = container.GetValue(UPnpNamespaces.Artwork),
            UpnpClass = container.GetValue(UPnpNamespaces.Class)
        };
    }
}
