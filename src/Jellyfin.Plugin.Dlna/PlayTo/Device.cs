using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Ssdp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="Device" />.
/// </summary>
public class Device : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly object _timerLock = new();
    private Timer? _timer;
    private int _muteVol;
    private int _volume;
    private DateTime _lastVolumeRefresh;
    private bool _volumeRefreshActive;
    private int _connectFailureCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Device"/> class.
    /// </summary>
    /// <param name="deviceProperties">The <see cref="DeviceInfo"/>.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public Device(DeviceInfo deviceProperties, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        Properties = deviceProperties;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Raised when playback starts.
    /// </summary>
    public event EventHandler<PlaybackStartEventArgs>? PlaybackStart;

    /// <summary>
    /// Raised when playback progresses.
    /// </summary>
    public event EventHandler<PlaybackProgressEventArgs>? PlaybackProgress;

    /// <summary>
    /// Raised when playback stopped.
    /// </summary>
    public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;

    /// <summary>
    /// Raised when media changed.
    /// </summary>
    public event EventHandler<MediaChangedEventArgs>? MediaChanged;

    /// <summary>
    /// Gets or sets the properties.
    /// </summary>
    public DeviceInfo Properties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is muted.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    public int Volume
    {
        get
        {
            RefreshVolumeIfNeeded().GetAwaiter().GetResult();
            return _volume;
        }

        set => _volume = value;
    }

    /// <summary>
    /// Gets or sets the playback duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets the playback position.
    /// </summary>
    public TimeSpan Position { get; set; } = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Gets or sets the transport state.
    /// </summary>
    public TransportState TransportState { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is playing.
    /// </summary>
    public bool IsPlaying => TransportState == TransportState.PLAYING;

    /// <summary>
    /// Gets or sets a value indicating whether the device is paused.
    /// </summary>
    public bool IsPaused => TransportState == TransportState.PAUSED_PLAYBACK;

    /// <summary>
    /// Gets or sets a value indicating whether the device is stopped.
    /// </summary>
    public bool IsStopped => TransportState == TransportState.STOPPED;

    /// <summary>
    /// Gets or sets the action to be executed when the device becomes unavailable.
    /// </summary>
    public Action? OnDeviceUnavailable { get; set; }

    /// <summary>
    /// Gets or sets the AV commands.
    /// </summary>
    private TransportCommands? AvCommands { get; set; }

    /// <summary>
    /// Gets or sets the render commands.
    /// </summary>
    private TransportCommands? RendererCommands { get; set; }

    /// <summary>
    /// Gets or sets the current media info.
    /// </summary>
    public UBaseObject? CurrentMediaInfo { get; private set; }

    /// <summary>
    /// Starts the device.
    /// </summary>
    public void Start()
    {
        _logger.LogDebug("Dlna Device.Start");
        _timer = new Timer(TimerCallback, null, 1000, Timeout.Infinite);
    }

    private Task RefreshVolumeIfNeeded()
    {
        if (_volumeRefreshActive
            && DateTime.UtcNow >= _lastVolumeRefresh.AddSeconds(5))
        {
            _lastVolumeRefresh = DateTime.UtcNow;
            return RefreshVolume();
        }

        return Task.CompletedTask;
    }

    private async Task RefreshVolume(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await GetVolume(cancellationToken).ConfigureAwait(false);
            await GetMute(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device volume info for {DeviceName}", Properties.Name);
        }
    }

    private void RestartTimer(bool immediate = false)
    {
        lock (_timerLock)
        {
            if (_disposed)
            {
                return;
            }

            _volumeRefreshActive = true;

            var time = immediate ? 100 : 10000;
            _timer?.Change(time, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Restarts the timer in inactive mode.
    /// </summary>
    private void RestartTimerInactive()
    {
        lock (_timerLock)
        {
            if (_disposed)
            {
                return;
            }

            _volumeRefreshActive = false;

            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Lowers the volume.
    /// </summary>
    public Task VolumeDown(CancellationToken cancellationToken)
    {
        var sendVolume = Math.Max(Volume - 5, 0);

        return SetVolume(sendVolume, cancellationToken);
    }

    /// <summary>
    /// Rises the volume.
    /// </summary>
    public Task VolumeUp(CancellationToken cancellationToken)
    {
        var sendVolume = Math.Min(Volume + 5, 100);

        return SetVolume(sendVolume, cancellationToken);
    }

    /// <summary>
    /// Toggles mute.
    /// </summary>
    public Task ToggleMute(CancellationToken cancellationToken)
    {
        if (IsMuted)
        {
            return Unmute(cancellationToken);
        }

        return Mute(cancellationToken);
    }

    /// <summary>
    /// Mutes the device.
    /// </summary>
    public async Task Mute(CancellationToken cancellationToken)
    {
        var success = await SetMute(true, cancellationToken).ConfigureAwait(true);

        if (!success)
        {
            await SetVolume(0, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Un-mutes the device.
    /// </summary>
    public async Task Unmute(CancellationToken cancellationToken)
    {
        var success = await SetMute(false, cancellationToken).ConfigureAwait(true);

        if (!success)
        {
            var sendVolume = _muteVol <= 0 ? 20 : _muteVol;

            await SetVolume(sendVolume, cancellationToken).ConfigureAwait(false);
        }
    }

    private DeviceService? GetServiceRenderingControl()
    {
        var services = Properties.Services;

        return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:RenderingControl:1", StringComparison.OrdinalIgnoreCase)) ??
               services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:RenderingControl", StringComparison.OrdinalIgnoreCase));
    }

    private DeviceService? GetAvTransportService()
    {
        var services = Properties.Services;

        return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:AVTransport:1", StringComparison.OrdinalIgnoreCase)) ??
               services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:AVTransport", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> SetMute(bool mute, CancellationToken cancellationToken)
    {
        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetMute");
        if (command is null)
        {
            return false;
        }

        var service = GetServiceRenderingControl();

        if (service is null)
        {
            return false;
        }

        _logger.LogDebug("Setting mute");
        var value = mute ? 1 : 0;

        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands!.BuildPost(command, service.ServiceType, value), // null checked above
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        IsMuted = mute;

        return true;
    }

    /// <summary>
    /// Sets volume on a scale of 0-100.
    /// </summary>
    /// <param name="value">The volume on a scale of 0-100.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetVolume(int value, CancellationToken cancellationToken)
    {
        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetVolume");
        if (command is null)
        {
            return;
        }

        var service = GetServiceRenderingControl() ?? throw new InvalidOperationException("Unable to find service");

        // Set it early and assume it will succeed
        // Remote control will perform better
        Volume = value;

        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands!.BuildPost(command, service.ServiceType, value), // null checked above
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Seeks playback.
    /// </summary>
    /// <param name="value">The value to seek to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public async Task Seek(TimeSpan value, CancellationToken cancellationToken)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Seek");
        if (command is null)
        {
            return;
        }

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands!.BuildPost(command, service.ServiceType, string.Format(CultureInfo.InvariantCulture, "{0:hh}:{0:mm}:{0:ss}", value), "REL_TIME"), // null checked above
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        RestartTimer(true);
    }

    /// <summary>
    /// Sets AV transport.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="header">The header.</param>
    /// <param name="metaData">The meta data.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public async Task SetAvTransport(string url, string? header, string metaData, CancellationToken cancellationToken)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

        url = url.Replace("&", "&amp;", StringComparison.Ordinal);

        _logger.LogDebug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, header);

        var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetAVTransportURI");
        if (command is null)
        {
            return;
        }

        var dictionary = new Dictionary<string, string>
        {
            { "CurrentURI", url },
            { "CurrentURIMetaData", CreateDidlMeta(metaData) }
        };

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        var post = avCommands!.BuildPost(command, service.ServiceType, url, dictionary); // null checked above
        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                post,
                header: header,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        try
        {
            await SetPlay(avCommands, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Some devices will throw an error if you tell it to play when it's already playing
            // Others won't
        }

        RestartTimer(true);
    }

    /// <summary>
    /// Sets next AV transport.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="header">The header.</param>
    /// <param name="metaData">The meta data.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <remarks>
    /// SetNextAvTransport is used to specify to the DLNA device what is the next track to play.
    /// Without that information, the next track command on the device does not work.
    /// </remarks>
    public async Task SetNextAvTransport(string url, string? header, string metaData, CancellationToken cancellationToken = default)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

        url = url.Replace("&", "&amp;", StringComparison.Ordinal);

        _logger.LogDebug("{PropertyName} - SetNextAvTransport Uri: {Url} DlnaHeaders: {Header}", Properties.Name, url, header);

        var command = avCommands?.ServiceActions.FirstOrDefault(c => string.Equals(c.Name, "SetNextAVTransportURI", StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            return;
        }

        var dictionary = new Dictionary<string, string>
        {
            { "NextURI", url },
            { "NextURIMetaData", CreateDidlMeta(metaData) }
        };

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        var post = avCommands!.BuildPost(command, service.ServiceType, url, dictionary); // null checked above
        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(Properties.BaseUrl, service, command.Name, post, header, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string CreateDidlMeta(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return SecurityElement.Escape(value);
    }

    private Task SetPlay(TransportCommands avCommands, CancellationToken cancellationToken)
    {
        var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "Play");
        if (command is null)
        {
            return Task.CompletedTask;
        }

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        return new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            avCommands.BuildPost(command, service.ServiceType, 1),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends play command.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public async Task SetPlay(CancellationToken cancellationToken)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);
        if (avCommands is null)
        {
            return;
        }

        await SetPlay(avCommands, cancellationToken).ConfigureAwait(false);

        RestartTimer(true);
    }

    /// <summary>
    /// Sends stop command.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public async Task SetStop(CancellationToken cancellationToken)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Stop");
        if (command is null)
        {
            return;
        }

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands!.BuildPost(command, service.ServiceType, 1), // null checked above
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        RestartTimer(true);
    }

    /// <summary>
    /// Sends pause command.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public async Task SetPause(CancellationToken cancellationToken)
    {
        var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Pause");
        if (command is null)
        {
            return;
        }

        var service = GetAvTransportService() ?? throw new InvalidOperationException("Unable to find service");
        await new DlnaHttpClient(_logger, _httpClientFactory)
            .SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands!.BuildPost(command, service.ServiceType, 1), // null checked above
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        TransportState = TransportState.PAUSED_PLAYBACK;

        RestartTimer(true);
    }

    private async void TimerCallback(object? sender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var cancellationToken = CancellationToken.None;

            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            if (avCommands is null)
            {
                return;
            }

            var transportState = await GetTransportInfo(avCommands, cancellationToken).ConfigureAwait(false);

            if (_disposed)
            {
                return;
            }

            if (transportState.HasValue)
            {
                // If we're not playing anything no need to get additional data
                if (transportState.Value == TransportState.STOPPED)
                {
                    UpdateMediaInfo(null, transportState.Value);
                }
                else
                {
                    var tuple = await GetPositionInfo(avCommands, cancellationToken).ConfigureAwait(false);

                    var currentObject = tuple.Track;

                    if (tuple.Success && currentObject is null)
                    {
                        currentObject = await GetMediaInfo(avCommands, cancellationToken).ConfigureAwait(false);
                    }

                    if (currentObject is not null)
                    {
                        UpdateMediaInfo(currentObject, transportState.Value);
                    }
                }

                _connectFailureCount = 0;

                if (_disposed)
                {
                    return;
                }

                // If we're not playing anything make sure we don't get data more often than necessary to keep the Session alive
                if (transportState.Value == TransportState.STOPPED)
                {
                    RestartTimerInactive();
                }
                else
                {
                    RestartTimer();
                }
            }
            else
            {
                RestartTimerInactive();
            }
        }
        catch (Exception ex)
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogError(ex, "Error updating device info for {DeviceName}", Properties.Name);

            _connectFailureCount++;

            if (_connectFailureCount >= 3)
            {
                var action = OnDeviceUnavailable;
                if (action is not null)
                {
                    _logger.LogDebug("Disposing device due to loss of connection");
                    action();
                    return;
                }
            }

            RestartTimerInactive();
        }
    }

    private async Task GetVolume(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "GetVolume");
        if (command is null)
        {
            return;
        }

        var service = GetServiceRenderingControl();

        if (service is null)
        {
            return;
        }

        var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            rendererCommands!.BuildPost(command, service.ServiceType), // null checked above
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.Document is null)
        {
            return;
        }

        var volume = result.Document.Descendants(UPnpNamespaces.RenderingControl + "GetVolumeResponse").Select(i => i.Element("CurrentVolume")).FirstOrDefault(i => i is not null);
        var volumeValue = volume?.Value;

        if (string.IsNullOrWhiteSpace(volumeValue))
        {
            return;
        }

        Volume = int.Parse(volumeValue, CultureInfo.InvariantCulture);

        if (Volume > 0)
        {
            _muteVol = Volume;
        }
    }

    private async Task GetMute(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

        var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "GetMute");
        if (command is null)
        {
            return;
        }

        var service = GetServiceRenderingControl();

        if (service is null)
        {
            return;
        }

        var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            rendererCommands!.BuildPost(command, service.ServiceType), // null checked above
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.Document is null)
        {
            return;
        }

        var valueNode = result.Document.Descendants(UPnpNamespaces.RenderingControl + "GetMuteResponse")
            .Select(i => i.Element("CurrentMute"))
            .FirstOrDefault(i => i is not null);

        IsMuted = string.Equals(valueNode?.Value, "1", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TransportState?> GetTransportInfo(TransportCommands avCommands, CancellationToken cancellationToken)
    {
        var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetTransportInfo");
        if (command is null)
        {
            return null;
        }

        var service = GetAvTransportService();
        if (service is null)
        {
            return null;
        }

        var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            avCommands.BuildPost(command, service.ServiceType),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.Document is null)
        {
            return null;
        }

        var transportState =
            result.Document.Descendants(UPnpNamespaces.AvTransport + "GetTransportInfoResponse").Select(i => i.Element("CurrentTransportState")).FirstOrDefault(i => i is not null);

        var transportStateValue = transportState?.Value;

        if (transportStateValue is not null
            && Enum.TryParse(transportStateValue, true, out TransportState state))
        {
            return state;
        }

        return null;
    }

    private async Task<UBaseObject?> GetMediaInfo(TransportCommands avCommands, CancellationToken cancellationToken)
    {
        var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetMediaInfo");
        if (command is null)
        {
            return null;
        }

        var service = GetAvTransportService();
        if (service is null)
        {
            throw new InvalidOperationException("Unable to find service");
        }

        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);
        if (rendererCommands is null)
        {
            return null;
        }

        var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            rendererCommands.BuildPost(command, service.ServiceType),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.Document is null)
        {
            return null;
        }

        var track = result.Document.Descendants("CurrentURIMetaData").FirstOrDefault();

        if (track is null)
        {
            return null;
        }

        var e = track.Element(UPnpNamespaces.Items) ?? track;

        var elementString = (string)e;

        if (!string.IsNullOrWhiteSpace(elementString))
        {
            return UpnpContainer.Create(e);
        }

        track = result.Document.Descendants("CurrentURI").FirstOrDefault();

        if (track is null)
        {
            return null;
        }

        e = track.Element(UPnpNamespaces.Items) ?? track;

        elementString = (string)e;

        if (!string.IsNullOrWhiteSpace(elementString))
        {
            return new UBaseObject
            {
                Url = elementString
            };
        }

        return null;
    }

    private async Task<(bool Success, UBaseObject? Track)> GetPositionInfo(TransportCommands avCommands, CancellationToken cancellationToken)
    {
        var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetPositionInfo");
        if (command is null)
        {
            return (false, null);
        }

        var service = GetAvTransportService();

        if (service is null)
        {
            throw new InvalidOperationException("Unable to find service");
        }

        var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

        if (rendererCommands is null)
        {
            return (false, null);
        }

        var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
            Properties.BaseUrl,
            service,
            command.Name,
            rendererCommands.BuildPost(command, service.ServiceType),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result is null || result.Document is null)
        {
            return (false, null);
        }

        var trackUriElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackURI")).FirstOrDefault(i => i is not null);
        var trackUri = trackUriElem?.Value;

        var durationElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackDuration")).FirstOrDefault(i => i is not null);
        var duration = durationElem?.Value;

        if (!string.IsNullOrWhiteSpace(duration)
            && !string.Equals(duration, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
        {
            Duration = TimeSpan.Parse(duration, CultureInfo.InvariantCulture);
        }
        else
        {
            Duration = null;
        }

        var positionElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("RelTime")).FirstOrDefault(i => i is not null);
        var position = positionElem?.Value;

        if (!string.IsNullOrWhiteSpace(position) && !string.Equals(position, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
        {
            Position = TimeSpan.Parse(position, CultureInfo.InvariantCulture);
        }

        var track = result.Document.Descendants("TrackMetaData").FirstOrDefault();

        if (track is null)
        {
            // If track is null, some vendors do this, use GetMediaInfo instead.
            return (true, null);
        }

        var trackString = (string)track;

        if (string.IsNullOrWhiteSpace(trackString) || string.Equals(trackString, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null);
        }

        XElement? uPnpResponse = null;

        try
        {
            uPnpResponse = ParseResponse(trackString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uncaught exception while parsing xml");
        }

        if (uPnpResponse is null)
        {
            _logger.LogError("Failed to parse xml: \n {Xml}", trackString);
            return (true, null);
        }

        var e = uPnpResponse.Element(UPnpNamespaces.Items);

        var uTrack = CreateUBaseObject(e, trackUri);

        return (true, uTrack);
    }

    private static XElement? ParseResponse(string xml)
    {
        // Handle different variations sent back by devices.
        try
        {
            return XElement.Parse(xml);
        }
        catch (XmlException)
        {
        }

        // first try to add a root node with a dlna namespace.
        try
        {
            return XElement.Parse("<data xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">" + xml + "</data>")
                .Descendants()
                .First();
        }
        catch (XmlException)
        {
        }

        // some devices send back invalid xml
        try
        {
            return XElement.Parse(xml.Replace("&", "&amp;", StringComparison.Ordinal));
        }
        catch (XmlException)
        {
        }

        return null;
    }

    private static UBaseObject CreateUBaseObject(XElement? container, string? trackUri)
    {
        ArgumentNullException.ThrowIfNull(container);

        var url = container.GetValue(UPnpNamespaces.Res);

        if (string.IsNullOrWhiteSpace(url))
        {
            url = trackUri;
        }

        return new UBaseObject
        {
            Id = container.GetAttributeValue(UPnpNamespaces.Id),
            ParentId = container.GetAttributeValue(UPnpNamespaces.ParentId),
            Title = container.GetValue(UPnpNamespaces.Title),
            IconUrl = container.GetValue(UPnpNamespaces.Artwork),
            SecondText = string.Empty,
            Url = url,
            ProtocolInfo = GetProtocolInfo(container),
            MetaData = container.ToString()
        };
    }

    private static string[] GetProtocolInfo(XElement container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var resElement = container.Element(UPnpNamespaces.Res);

        var info = resElement?.Attribute(UPnpNamespaces.ProtocolInfo);

        if (info is not null && !string.IsNullOrWhiteSpace(info.Value))
        {
            return info.Value.Split(':');
        }

        return new string[4];
    }

    private async Task<TransportCommands?> GetAVProtocolAsync(CancellationToken cancellationToken)
    {
        if (AvCommands is not null)
        {
            return AvCommands;
        }

        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);

        var avService = GetAvTransportService();
        if (avService is null)
        {
            return null;
        }

        string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

        var httpClient = new DlnaHttpClient(_logger, _httpClientFactory);

        var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        AvCommands = TransportCommands.Create(document);
        return AvCommands;
    }

    private async Task<TransportCommands?> GetRenderingProtocolAsync(CancellationToken cancellationToken)
    {
        if (RendererCommands is not null)
        {
            return RendererCommands;
        }

        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);

        var avService = GetServiceRenderingControl();
        ArgumentNullException.ThrowIfNull(avService);

        string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

        var httpClient = new DlnaHttpClient(_logger, _httpClientFactory);
        _logger.LogDebug("Dlna Device.GetRenderingProtocolAsync");
        var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        RendererCommands = TransportCommands.Create(document);
        return RendererCommands;
    }

    private static string NormalizeUrl(string baseUrl, string url)
    {
        // If it's already a complete url, don't stick anything onto the front of it
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (!url.Contains('/', StringComparison.Ordinal))
        {
            url = "/dmr/" + url;
        }

        if (!url.StartsWith('/'))
        {
            url = "/" + url;
        }

        return baseUrl + url;
    }

    /// <summary>
    /// Creates uPNP device.
    /// </summary>
    /// <param name="url">The <see cref="Uri"/>.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    public static async Task<Device?> CreateuPnpDeviceAsync(Uri url, IHttpClientFactory httpClientFactory, ILogger logger, CancellationToken cancellationToken)
    {
        var ssdpHttpClient = new DlnaHttpClient(logger, httpClientFactory);

        var document = await ssdpHttpClient.GetDataAsync(url.ToString(), cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        var friendlyNames = new List<string>();

        var name = document.Descendants(UPnpNamespaces.Ud.GetName("friendlyName")).FirstOrDefault();
        if (name is not null && !string.IsNullOrWhiteSpace(name.Value))
        {
            friendlyNames.Add(name.Value);
        }

        var room = document.Descendants(UPnpNamespaces.Ud.GetName("roomName")).FirstOrDefault();
        if (room is not null && !string.IsNullOrWhiteSpace(room.Value))
        {
            friendlyNames.Add(room.Value);
        }

        var deviceProperties = new DeviceInfo()
        {
            Name = string.Join(' ', friendlyNames),
            BaseUrl = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}", url.Host, url.Port),
            Services = GetServices(document)
        };

        var model = document.Descendants(UPnpNamespaces.Ud.GetName("modelName")).FirstOrDefault();
        if (model is not null)
        {
            deviceProperties.ModelName = model.Value;
        }

        var modelNumber = document.Descendants(UPnpNamespaces.Ud.GetName("modelNumber")).FirstOrDefault();
        if (modelNumber is not null)
        {
            deviceProperties.ModelNumber = modelNumber.Value;
        }

        var uuid = document.Descendants(UPnpNamespaces.Ud.GetName("UDN")).FirstOrDefault();
        if (uuid is not null)
        {
            deviceProperties.UUID = uuid.Value;
        }

        var manufacturer = document.Descendants(UPnpNamespaces.Ud.GetName("manufacturer")).FirstOrDefault();
        if (manufacturer is not null)
        {
            deviceProperties.Manufacturer = manufacturer.Value;
        }

        var manufacturerUrl = document.Descendants(UPnpNamespaces.Ud.GetName("manufacturerURL")).FirstOrDefault();
        if (manufacturerUrl is not null)
        {
            deviceProperties.ManufacturerUrl = manufacturerUrl.Value;
        }

        var presentationUrl = document.Descendants(UPnpNamespaces.Ud.GetName("presentationURL")).FirstOrDefault();
        if (presentationUrl is not null)
        {
            deviceProperties.PresentationUrl = presentationUrl.Value;
        }

        var modelUrl = document.Descendants(UPnpNamespaces.Ud.GetName("modelURL")).FirstOrDefault();
        if (modelUrl is not null)
        {
            deviceProperties.ModelUrl = modelUrl.Value;
        }

        var serialNumber = document.Descendants(UPnpNamespaces.Ud.GetName("serialNumber")).FirstOrDefault();
        if (serialNumber is not null)
        {
            deviceProperties.SerialNumber = serialNumber.Value;
        }

        var modelDescription = document.Descendants(UPnpNamespaces.Ud.GetName("modelDescription")).FirstOrDefault();
        if (modelDescription is not null)
        {
            deviceProperties.ModelDescription = modelDescription.Value;
        }

        var icon = document.Descendants(UPnpNamespaces.Ud.GetName("icon")).FirstOrDefault();
        if (icon is not null)
        {
            deviceProperties.Icon = CreateIcon(icon);
        }

        return new Device(deviceProperties, httpClientFactory, logger);
    }

    private static DeviceIcon CreateIcon(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var width = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("width"));
        var height = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("height"));

        _ = int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var widthValue);
        _ = int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var heightValue);

        return new DeviceIcon
        {
            Depth = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("depth")) ?? string.Empty,
            Height = heightValue,
            MimeType = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("mimetype")) ?? string.Empty,
            Url = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("url")) ?? string.Empty,
            Width = widthValue
        };
    }

    private static DeviceService Create(XElement element)
        => new()
        {
            ControlUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("controlURL")) ?? string.Empty,
            EventSubUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("eventSubURL")) ?? string.Empty,
            ScpdUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("SCPDURL")) ?? string.Empty,
            ServiceId = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceId")) ?? string.Empty,
            ServiceType = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceType")) ?? string.Empty
        };

        private static List<DeviceService> GetServices(XDocument document)
        {
            List<DeviceService> deviceServices = [];
            foreach (var services in document.Descendants(UPnpNamespaces.Ud.GetName("serviceList")))
            {
                if (services is null)
                {
                    continue;
                }

                var servicesList = services.Descendants(UPnpNamespaces.Ud.GetName("service"));
                if (servicesList is null)
                {
                    continue;
                }

                foreach (var element in servicesList)
                {
                    var service = Create(element);

                    if (service is not null)
                    {
                        deviceServices.Add(service);
                    }
                }
            }

            return deviceServices;
        }

    private void UpdateMediaInfo(UBaseObject? mediaInfo, TransportState state)
    {
        TransportState = state;

        var previousMediaInfo = CurrentMediaInfo;
        CurrentMediaInfo = mediaInfo;

        if (mediaInfo is null)
        {
            if (previousMediaInfo is not null)
            {
                OnPlaybackStop(previousMediaInfo);
            }
        }
        else if (previousMediaInfo is null)
        {
            if (state != TransportState.STOPPED)
            {
                OnPlaybackStart(mediaInfo);
            }
        }
        else if (mediaInfo.Equals(previousMediaInfo))
        {
            OnPlaybackProgress(mediaInfo);
        }
        else
        {
            OnMediaChanged(previousMediaInfo, mediaInfo);
        }
    }

    private void OnPlaybackStart(UBaseObject mediaInfo)
    {
        if (string.IsNullOrWhiteSpace(mediaInfo.Url))
        {
            return;
        }

        PlaybackStart?.Invoke(this, new PlaybackStartEventArgs(mediaInfo));
    }

    private void OnPlaybackProgress(UBaseObject mediaInfo)
    {
        if (string.IsNullOrWhiteSpace(mediaInfo.Url))
        {
            return;
        }

        PlaybackProgress?.Invoke(this, new PlaybackProgressEventArgs(mediaInfo));
    }

    private void OnPlaybackStop(UBaseObject mediaInfo)
    {
        PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(mediaInfo));
    }

    private void OnMediaChanged(UBaseObject old, UBaseObject newMedia)
    {
        MediaChanged?.Invoke(this, new MediaChangedEventArgs(old, newMedia));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer?.Dispose();
            _timer = null;
            Properties = null!;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Properties.Name, Properties.BaseUrl);
    }
}
