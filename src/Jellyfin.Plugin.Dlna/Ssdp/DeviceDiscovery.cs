#nullable disable

using System;
using System.Linq;
using Jellyfin.Data.Events;
using Jellyfin.Plugin.Dlna.Model;
using Rssdp;
using Rssdp.Infrastructure;

namespace Jellyfin.Plugin.Dlna.Ssdp;

/// <summary>
/// Defines the <see cref="DeviceDiscovery" />.
/// </summary>
public sealed class DeviceDiscovery : IDeviceDiscovery, IDisposable
{
    private readonly object _syncLock = new();

    private SsdpDeviceLocator _deviceLocator;
    private ISsdpCommunicationsServer _commsServer;

    private int _listenerCount;
    private bool _disposed;

    private event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscoveredInternal;

    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered
    {
        add
        {
            lock (_syncLock)
            {
                _listenerCount++;
                DeviceDiscoveredInternal += value;
            }

            StartInternal();
        }

        remove
        {
            lock (_syncLock)
            {
                _listenerCount--;
                DeviceDiscoveredInternal -= value;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

    /// <summary>
    /// Starts device discovery.
    /// </summary>
    public void Start(ISsdpCommunicationsServer communicationsServer)
    {
        _commsServer = communicationsServer;

        StartInternal();
    }

    private void StartInternal()
    {
        lock (_syncLock)
        {
            if (_listenerCount > 0 && _deviceLocator is null && _commsServer is not null)
            {
                _deviceLocator = new SsdpDeviceLocator(
                    _commsServer,
                    Environment.OSVersion.Platform.ToString(),
                    // Can not use VersionString here since that includes OS and version
                    Environment.OSVersion.Version.ToString());

                // (Optional) Set the filter so we only see notifications for devices we care about
                // (can be any search target value i.e device type, uuid value etc - any value that appears in the
                // DiscoverdSsdpDevice.NotificationType property or that is used with the searchTarget parameter of the Search method).
                // _DeviceLocator.NotificationFilter = "upnp:rootdevice";

                // Connect our event handler so we process devices as they are found
                _deviceLocator.DeviceAvailable += OnDeviceLocatorDeviceAvailable;
                _deviceLocator.DeviceUnavailable += OnDeviceLocatorDeviceUnavailable;

                var dueTime = TimeSpan.FromSeconds(5);
                var options = DlnaPlugin.Instance.Configuration;
                var interval = TimeSpan.FromSeconds(options.ClientDiscoveryIntervalSeconds);

                _deviceLocator.RestartBroadcastTimer(dueTime, interval);
            }
        }
    }

    // Process each found device in the event handler
    private void OnDeviceLocatorDeviceAvailable(object sender, DeviceAvailableEventArgs e)
    {
        var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

        var headerDict = originalHeaders is null ? [] : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

        var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

        var args = new GenericEventArgs<UpnpDeviceInfo>(
            new UpnpDeviceInfo
            {
                Location = e.DiscoveredDevice.DescriptionLocation,
                Headers = headers,
                RemoteIPAddress = e.RemoteIPAddress
            });

        DeviceDiscoveredInternal?.Invoke(this, args);
    }

    private void OnDeviceLocatorDeviceUnavailable(object sender, DeviceUnavailableEventArgs e)
    {
        var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

        var headerDict = originalHeaders is null ? [] : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

        var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

        var args = new GenericEventArgs<UpnpDeviceInfo>(
            new UpnpDeviceInfo
            {
                Location = e.DiscoveredDevice.DescriptionLocation,
                Headers = headers
            });

        DeviceLeft?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_deviceLocator is not null)
            {
                _deviceLocator.Dispose();
                _deviceLocator = null;
            }
        }
    }
}
