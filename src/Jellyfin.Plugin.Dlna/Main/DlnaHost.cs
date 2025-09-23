using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.PlayTo;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rssdp;
using Rssdp.Infrastructure;

namespace Jellyfin.Plugin.Dlna.Main;

/// <summary>
/// An <see cref="IHostedService"/> that manages a DLNA server.
/// </summary>
public sealed class DlnaHost : IHostedService, IDisposable
{
    private readonly ILogger<DlnaHost> _logger;
    private readonly IServerConfigurationManager _config;
    private readonly IServerApplicationHost _appHost;
    private readonly ISessionManager _sessionManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDlnaManager _dlnaManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly IUserDataManager _userDataManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IDeviceDiscovery _deviceDiscovery;
    private readonly ISsdpCommunicationsServer _communicationsServer;
    private readonly INetworkManager _networkManager;
    private readonly object _syncLock = new();

    private SsdpDevicePublisher? _publisher;
    private PlayToManager? _manager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaHost"/> class.
    /// </summary>
    /// <param name="config">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
    /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="userManager">The <see cref="IUserManager"/>.</param>
    /// <param name="dlnaManager">The <see cref="IDlnaManager"/>.</param>
    /// <param name="imageProcessor">The <see cref="IImageProcessor"/>.</param>
    /// <param name="userDataManager">The <see cref="IUserDataManager"/>.</param>
    /// <param name="localizationManager">The <see cref="ILocalizationManager"/>.</param>
    /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/>.</param>
    /// <param name="deviceDiscovery">The <see cref="IDeviceDiscovery"/>.</param>
    /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/>.</param>
    /// <param name="communicationsServer">The <see cref="ISsdpCommunicationsServer"/>.</param>
    /// <param name="networkManager">The <see cref="INetworkManager"/>.</param>
    public DlnaHost(
        IServerConfigurationManager config,
        ILoggerFactory loggerFactory,
        IServerApplicationHost appHost,
        ISessionManager sessionManager,
        IHttpClientFactory httpClientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDlnaManager dlnaManager,
        IImageProcessor imageProcessor,
        IUserDataManager userDataManager,
        ILocalizationManager localizationManager,
        IMediaSourceManager mediaSourceManager,
        IDeviceDiscovery deviceDiscovery,
        IMediaEncoder mediaEncoder,
        ISsdpCommunicationsServer communicationsServer,
        INetworkManager networkManager)
    {
        _config = config;
        _appHost = appHost;
        _sessionManager = sessionManager;
        _httpClientFactory = httpClientFactory;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dlnaManager = dlnaManager;
        _imageProcessor = imageProcessor;
        _userDataManager = userDataManager;
        _localization = localizationManager;
        _mediaSourceManager = mediaSourceManager;
        _deviceDiscovery = deviceDiscovery;
        _mediaEncoder = mediaEncoder;
        _communicationsServer = communicationsServer;
        _networkManager = networkManager;
        _logger = loggerFactory.CreateLogger<DlnaHost>();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var netConfig = _config.GetConfiguration<NetworkConfiguration>(NetworkConfigurationStore.StoreKey);
        if (_appHost.ListenWithHttps && netConfig.RequireHttps)
        {
            // No use starting as dlna won't work, as we're running purely on HTTPS.
            _logger.LogError("The DLNA specification does not support HTTPS.");
            return;
        }

        await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);
        ReloadComponents();

        _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Stop();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }

    private void OnNamedConfigurationUpdated(object? sender, ConfigurationUpdateEventArgs e)
    {
        if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
        {
            ReloadComponents();
        }
    }

    private void ReloadComponents()
    {
        var options = DlnaPlugin.Instance.Configuration;
        StartDeviceDiscovery();
        StartDevicePublisher(options);

        if (options.EnablePlayTo)
        {
            StartPlayToManager();
        }
        else
        {
            DisposePlayToManager();
        }
    }

    private static string CreateUuid(string text)
    {
        if (!Guid.TryParse(text, out var guid))
        {
            guid = text.GetMD5();
        }

        return guid.ToString("D", CultureInfo.InvariantCulture);
    }

    private static void SetProperties(SsdpDevice device, string fullDeviceType)
    {
        var serviceParts = fullDeviceType
            .Replace("urn:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(":1", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split(':');

        device.DeviceTypeNamespace = serviceParts[0].Replace('.', '-');
        device.DeviceClass = serviceParts[1];
        device.DeviceType = serviceParts[2];
    }

    private void StartDeviceDiscovery()
    {
        try
        {
            ((DeviceDiscovery)_deviceDiscovery).Start(_communicationsServer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting device discovery");
        }
    }

    private void StartDevicePublisher(DlnaPluginConfiguration options)
    {
        if (_publisher is not null)
        {
            return;
        }

        try
        {
            _publisher = new SsdpDevicePublisher(
                _communicationsServer,
                Environment.OSVersion.Platform.ToString(),
                // Can not use VersionString here since that includes OS and version
                Environment.OSVersion.Version.ToString(),
                options.SendOnlyMatchedHost)
            {
                LogFunction = msg => _logger.LogDebug("{Msg}", msg),
                SupportPnpRootDevice = false
            };

            RegisterServerEndpoints();

            if (options.BlastAliveMessages)
            {
                _publisher.StartSendingAliveNotifications(TimeSpan.FromSeconds(options.AliveMessageIntervalSeconds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering endpoint");
        }
    }

    private void RegisterServerEndpoints()
    {
        var udn = CreateUuid(_appHost.SystemId);
        var netConfig = _config.GetConfiguration<NetworkConfiguration>(NetworkConfigurationStore.StoreKey);
        var baseURL = netConfig.BaseUrl;
        var descriptorUri = baseURL + "/dlna/" + udn + "/description.xml";

        // Only get bind addresses in LAN
        // IPv6 is currently unsupported
        var validInterfaces =  _networkManager.GetInternalBindAddresses()
            .Where(x => x.Address is not null)
            .Where(x => x.AddressFamily != AddressFamily.InterNetworkV6)
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
            .Where(x => x.SupportsMulticast)
            .Where(x => !x.Address.Equals(IPAddress.Loopback))
            .ToList();

        var httpBindPort = _appHost.HttpPort;

        if (validInterfaces.Count == 0)
        {
            // No interfaces returned, fall back to loopback
            validInterfaces = _networkManager.GetLoopbacks().ToList();
        }

        foreach (var intf in validInterfaces)
        {
            var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

            var uri = new UriBuilder(intf.Address + descriptorUri);
            uri.Scheme = "http://";
            uri.Port = httpBindPort;

            _logger.LogInformation("Registering publisher for {ResourceName} on {DeviceAddress} with uri {FullUri}", fullService, intf.Address, uri);

            var device = new SsdpRootDevice
            {
                CacheLifetime = TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                Location = uri.Uri, // Must point to the URL that serves your devices UPnP description document.
                Address = intf.Address,
                PrefixLength = NetworkUtils.MaskToCidr(intf.Subnet.Prefix),
                FriendlyName = "Jellyfin",
                Manufacturer = "Jellyfin",
                ModelName = "Jellyfin Server",
                Uuid = udn
                // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
            };

            SetProperties(device, fullService);
            _publisher!.AddDevice(device);

            var embeddedDevices = new[]
            {
                "urn:schemas-upnp-org:service:ContentDirectory:1",
                "urn:schemas-upnp-org:service:ConnectionManager:1",
                // "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1"
            };

            foreach (var subDevice in embeddedDevices)
            {
                var embeddedDevice = new SsdpEmbeddedDevice
                {
                    FriendlyName = device.FriendlyName,
                    Manufacturer = device.Manufacturer,
                    ModelName = device.ModelName,
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
                };

                SetProperties(embeddedDevice, subDevice);
                device.AddDevice(embeddedDevice);
            }
        }
    }

    private void StartPlayToManager()
    {
        lock (_syncLock)
        {
            if (_manager is not null)
            {
                return;
            }

            try
            {
                _manager = new PlayToManager(
                    _logger,
                    _sessionManager,
                    _libraryManager,
                    _userManager,
                    _dlnaManager,
                    _appHost,
                    _imageProcessor,
                    _deviceDiscovery,
                    _httpClientFactory,
                    _userDataManager,
                    _localization,
                    _mediaSourceManager,
                    _mediaEncoder);

                _manager.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting PlayTo manager");
            }
        }
    }

    private void DisposePlayToManager()
    {
        lock (_syncLock)
        {
            if (_manager is not null)
            {
                try
                {
                    _logger.LogInformation("Disposing PlayToManager");
                    _manager.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing PlayTo manager");
                }

                _manager = null;
            }
        }
    }

    private void DisposeDevicePublisher()
    {
        if (_publisher is not null)
        {
            _logger.LogInformation("Disposing SsdpDevicePublisher");
            _publisher.Dispose();
            _publisher = null;
        }
    }

    private void Stop()
    {
        DisposeDevicePublisher();
        DisposePlayToManager();
    }
}
