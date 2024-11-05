#nullable disable

#pragma warning disable CA2227

using System;
using System.Collections.Generic;
using System.Net;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="UpnpDeviceInfo" />.
/// </summary>
public class UpnpDeviceInfo
{
    /// <summary>
    /// Gets or sets the location.
    /// </summary>
    public Uri Location { get; set; }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Gets or sets local IP address.
    /// </summary>
    public IPAddress LocalIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the local port.
    /// </summary>
    public int LocalPort { get; set; }

    /// <summary>
    /// Gets or sets the remote IP address.
    /// </summary>
    public IPAddress RemoteIPAddress { get; set; }
}
