using System;
using Jellyfin.Data.Events;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="IDeviceDiscovery" /> interface.
/// </summary>

public interface IDeviceDiscovery
{
    /// <summary>
    /// Occurs when [device discovered].
    /// </summary>
    event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;

    /// <summary>
    /// Occurs when [device left].
    /// </summary>
    event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;
}
