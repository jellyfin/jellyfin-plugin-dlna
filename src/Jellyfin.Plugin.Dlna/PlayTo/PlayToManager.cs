using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;
using IDlnaManager = Jellyfin.Plugin.Dlna.Model.IDlnaManager;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlayToManager" />.
/// </summary>
public sealed class PlayToManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly ISessionManager _sessionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDlnaManager _dlnaManager;
    private readonly IServerApplicationHost _appHost;
    private readonly IImageProcessor _imageProcessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserDataManager _userDataManager;
    private readonly ILocalizationManager _localization;
    private readonly IDeviceDiscovery _deviceDiscovery;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayToManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
    /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
    /// <param name="deviceDiscovery">Instance of the <see cref="IDeviceDiscovery"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    public PlayToManager(
        ILogger logger,
        ISessionManager sessionManager,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDlnaManager dlnaManager,
        IServerApplicationHost appHost,
        IImageProcessor imageProcessor,
        IDeviceDiscovery deviceDiscovery,
        IHttpClientFactory httpClientFactory,
        IUserDataManager userDataManager,
        ILocalizationManager localization,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dlnaManager = dlnaManager;
        _appHost = appHost;
        _imageProcessor = imageProcessor;
        _deviceDiscovery = deviceDiscovery;
        _httpClientFactory = httpClientFactory;
        _userDataManager = userDataManager;
        _localization = localization;
        _mediaSourceManager = mediaSourceManager;
        _mediaEncoder = mediaEncoder;
    }

    /// <summary>
    /// Starts device discovery.
    /// </summary>
    public void Start()
    {
        _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
    }

    private async void OnDeviceDiscoveryDeviceDiscovered(object? sender, GenericEventArgs<UpnpDeviceInfo> e)
    {
        if (_disposed)
        {
            return;
        }

        var info = e.Argument;

        if (!info.Headers.TryGetValue("USN", out string? usn))
        {
            usn = string.Empty;
        }

        if (!info.Headers.TryGetValue("NT", out string? nt))
        {
            nt = string.Empty;
        }

        // It has to report that it's a media renderer
        if (!usn.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase)
            && !nt.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cancellationToken = _disposeCancellationTokenSource.Token;

        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return;
            }

            if (_sessionManager.Sessions.Any(i => usn.Contains(i.DeviceId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await AddDevice(info, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PlayTo device.");
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    internal static string GetUuid(string usn)
    {
        const string UuidStr = "uuid:";
        const string UuidColonStr = "::";

        var index = usn.IndexOf(UuidStr, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            return usn.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        ReadOnlySpan<char> tmp = usn.AsSpan()[(index + UuidStr.Length)..];

        index = tmp.IndexOf(UuidColonStr, StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            tmp = tmp[..index];
        }

        index = tmp.IndexOf('{');
        if (index != -1)
        {
            int endIndex = tmp.IndexOf('}');
            if (endIndex != -1)
            {
                tmp = tmp[(index + 1)..endIndex];
            }
        }

        return tmp.ToString();
    }

    private async Task AddDevice(UpnpDeviceInfo info, CancellationToken cancellationToken)
    {
        var uri = info.Location;
        _logger.LogDebug("Attempting to create PlayToController from location {0}", uri);

        if (info.Headers.TryGetValue("USN", out string? uuid))
        {
            uuid = GetUuid(uuid);
        }
        else
        {
            uuid = uri.ToString().GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        var sessionInfo = await _sessionManager
            .LogSessionActivity("DLNA", _appHost.ApplicationVersionString, uuid, null, uri.OriginalString, null)
            .ConfigureAwait(false);

        var controller = sessionInfo.SessionControllers.OfType<PlayToController>().FirstOrDefault();

        if (controller is null)
        {
            var device = await Device.CreateuPnpDeviceAsync(uri, _httpClientFactory, _logger, cancellationToken).ConfigureAwait(false);
            if (device is null)
            {
                _logger.LogError("Ignoring device as xml response is invalid.");
                return;
            }

            string deviceName = device.Properties.Name;

            _sessionManager.UpdateDeviceName(sessionInfo.Id, deviceName);

            string serverAddress = _appHost.GetSmartApiUrl(info.RemoteIPAddress);

            controller = new PlayToController(
                sessionInfo,
                _sessionManager,
                _libraryManager,
                _logger,
                _dlnaManager,
                _userManager,
                _imageProcessor,
                serverAddress,
                null,
                _deviceDiscovery,
                _userDataManager,
                _localization,
                _mediaSourceManager,
                _mediaEncoder,
                device);

            sessionInfo.AddController(controller);

            var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                          _dlnaManager.GetDefaultProfile();

            _sessionManager.ReportCapabilities(sessionInfo.Id, new ClientCapabilities
            {
                PlayableMediaTypes = profile.FetchSupportedMediaTypes(),

                SupportedCommands = new[]
                {
                    GeneralCommandType.VolumeDown,
                    GeneralCommandType.VolumeUp,
                    GeneralCommandType.Mute,
                    GeneralCommandType.Unmute,
                    GeneralCommandType.ToggleMute,
                    GeneralCommandType.SetVolume,
                    GeneralCommandType.SetAudioStreamIndex,
                    GeneralCommandType.SetSubtitleStreamIndex,
                    GeneralCommandType.PlayMediaSource
                },

                SupportsMediaControl = true
            });

            _logger.LogInformation("DLNA Session created for {0} - {1} using profile {2}", device.Properties.Name, device.Properties.ModelName, profile.Name);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _deviceDiscovery.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;

        try
        {
            _disposeCancellationTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error while disposing PlayToManager");
        }

        _sessionLock.Dispose();
        _disposeCancellationTokenSource.Dispose();

        _disposed = true;
    }
}
