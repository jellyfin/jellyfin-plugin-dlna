using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Extensions;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ContentFeatureBuilder = Jellyfin.Plugin.Dlna.Model.ContentFeatureBuilder;
using IDeviceDiscovery = Jellyfin.Plugin.Dlna.Model.IDeviceDiscovery;
using IDlnaManager = Jellyfin.Plugin.Dlna.Model.IDlnaManager;
using Photo = MediaBrowser.Controller.Entities.Photo;
using UpnpDeviceInfo = Jellyfin.Plugin.Dlna.Model.UpnpDeviceInfo;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlayToSession" />.
/// This used to be PlayToController but that gets automatically registered.
/// </summary>
public class PlayToSession : ISessionController, IDisposable
{
    private readonly SessionInfo _session;
    private readonly ISessionManager _sessionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly IDlnaManager _dlnaManager;
    private readonly IUserManager _userManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly IUserDataManager _userDataManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IDeviceDiscovery _deviceDiscovery;
    private readonly string _serverAddress;
    private readonly string? _accessToken;
    private readonly List<PlaylistItem> _playlist = [];
    private Device _device;
    private int _currentPlaylistIndex;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayToSession"/> class.
    /// </summary>
    /// <param name="session">The <see cref="SessionInfo"/>.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
    /// <param name="serverAddress">The server address.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="deviceDiscovery">Instance of the <see cref="IDeviceDiscovery"/> interface.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="device">The <see cref="Device"/>.</param>
    public PlayToSession(
        SessionInfo session,
        ISessionManager sessionManager,
        ILibraryManager libraryManager,
        ILogger logger,
        IDlnaManager dlnaManager,
        IUserManager userManager,
        IImageProcessor imageProcessor,
        string serverAddress,
        string? accessToken,
        IDeviceDiscovery deviceDiscovery,
        IUserDataManager userDataManager,
        ILocalizationManager localization,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder,
        Device device)
    {
        _session = session;
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
        _logger = logger;
        _dlnaManager = dlnaManager;
        _userManager = userManager;
        _imageProcessor = imageProcessor;
        _serverAddress = serverAddress;
        _accessToken = accessToken;
        _deviceDiscovery = deviceDiscovery;
        _userDataManager = userDataManager;
        _localization = localization;
        _mediaSourceManager = mediaSourceManager;
        _mediaEncoder = mediaEncoder;

        _device = device;
        _device.OnDeviceUnavailable = OnDeviceUnavailable;
        _device.PlaybackStart += OnDevicePlaybackStart;
        _device.PlaybackProgress += OnDevicePlaybackProgress;
        _device.PlaybackStopped += OnDevicePlaybackStopped;
        _device.MediaChanged += OnDeviceMediaChanged;

        _device.Start();

        _deviceDiscovery.DeviceLeft += OnDeviceDiscoveryDeviceLeft;
    }

    /// <summary>
    /// Gets or sets a value indicating the session is active.
    /// </summary>
    public bool IsSessionActive => !_disposed;

    /// <summary>
    /// Gets or sets a value indicating whether media control is supported.
    /// </summary>
    public bool SupportsMediaControl => IsSessionActive;

    /*
     * Send a message to the DLNA device to notify what is the next track in the playlist.
     */
    private async Task SendNextTrackMessage(int currentPlayListItemIndex, CancellationToken cancellationToken)
    {
        if (currentPlayListItemIndex >= 0 && currentPlayListItemIndex < _playlist.Count - 1)
        {
            // The current playing item is indeed in the play list and we are not yet at the end of the playlist.
            var nextItemIndex = currentPlayListItemIndex + 1;
            var nextItem = _playlist[nextItemIndex];

            if (nextItem is null)
            {
                return;
            }

            // Send the SetNextAvTransport message.
            await _device.SetNextAvTransport(nextItem.StreamUrl, GetDlnaHeaders(nextItem), nextItem.Didl, cancellationToken).ConfigureAwait(false);
        }
    }

    private async void OnDeviceUnavailable()
    {
        try
        {
            await _sessionManager.ReportSessionEnded(_session.Id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Could throw if the session is already gone
            _logger.LogError(ex, "Error reporting the end of session {Id}", _session.Id);
        }
    }

    private void OnDeviceDiscoveryDeviceLeft(object? sender, GenericEventArgs<UpnpDeviceInfo> e)
    {
        var info = e.Argument;

        if (!_disposed
            && info.Headers.TryGetValue("USN", out string? usn)
            && usn.Contains(_device.Properties.UUID, StringComparison.OrdinalIgnoreCase)
            && (usn.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase)
                || (info.Headers.TryGetValue("NT", out string? nt)
                    && nt.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase))))
        {
            OnDeviceUnavailable();
        }
    }

    private async void OnDeviceMediaChanged(object? sender, MediaChangedEventArgs e)
    {
        if (_disposed || string.IsNullOrEmpty(e.OldMediaInfo.Url))
        {
            return;
        }

        try
        {
            var streamInfo = StreamParams.ParseFromUrl(e.OldMediaInfo.Url, _libraryManager, _mediaSourceManager);
            if (streamInfo.Item is not null)
            {
                var positionTicks = GetProgressPositionTicks(streamInfo);

                await ReportPlaybackStopped(streamInfo, positionTicks).ConfigureAwait(false);
            }

            streamInfo = StreamParams.ParseFromUrl(e.NewMediaInfo.Url, _libraryManager, _mediaSourceManager);
            if (streamInfo.Item is null)
            {
                return;
            }

            var newItemProgress = GetProgressInfo(streamInfo);

            await _sessionManager.OnPlaybackStart(newItemProgress).ConfigureAwait(false);

            // Send a message to the DLNA device to notify what is the next track in the playlist.
            var currentItemIndex = _playlist.FindIndex(item => item.StreamInfo.ItemId.Equals(streamInfo.ItemId));
            if (currentItemIndex >= 0)
            {
                _currentPlaylistIndex = currentItemIndex;
            }

            await SendNextTrackMessage(currentItemIndex, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress");
        }
    }

    private async void OnDevicePlaybackStopped(object? sender, PlaybackStoppedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var streamInfo = StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager);

            if (streamInfo.Item is null)
            {
                return;
            }

            var positionTicks = GetProgressPositionTicks(streamInfo);

            await ReportPlaybackStopped(streamInfo, positionTicks).ConfigureAwait(false);

            var mediaSource = await streamInfo.GetMediaSource(CancellationToken.None).ConfigureAwait(false);

            var duration = mediaSource is null
                ? _device.Duration?.Ticks
                : mediaSource.RunTimeTicks;

            var playedToCompletion = positionTicks.HasValue && positionTicks.Value == 0;

            if (!playedToCompletion && duration.HasValue && positionTicks.HasValue)
            {
                double percent = positionTicks.Value;
                percent /= duration.Value;

                playedToCompletion = Math.Abs(1 - percent) <= .1;
            }

            if (playedToCompletion)
            {
                await SetPlaylistIndex(_currentPlaylistIndex + 1).ConfigureAwait(false);
            }
            else
            {
                _playlist.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting playback stopped");
        }
    }

    private async Task ReportPlaybackStopped(StreamParams streamInfo, long? positionTicks)
    {
        try
        {
            await _sessionManager.OnPlaybackStopped(new PlaybackStopInfo
            {
                ItemId = streamInfo.ItemId,
                SessionId = _session.Id,
                PositionTicks = positionTicks,
                MediaSourceId = streamInfo.MediaSourceId
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress");
        }
    }

    private async void OnDevicePlaybackStart(object? sender, PlaybackStartEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var info = StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager);

            if (info.Item is not null)
            {
                var progress = GetProgressInfo(info);

                await _sessionManager.OnPlaybackStart(progress).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress");
        }
    }

    private async void OnDevicePlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var mediaUrl = e.MediaInfo.Url;

            if (string.IsNullOrWhiteSpace(mediaUrl))
            {
                return;
            }

            var info = StreamParams.ParseFromUrl(mediaUrl, _libraryManager, _mediaSourceManager);

            if (info.Item is not null)
            {
                var progress = GetProgressInfo(info);

                await _sessionManager.OnPlaybackProgress(progress).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress");
        }
    }

    private long? GetProgressPositionTicks(StreamParams info)
    {
        var ticks = _device.Position.Ticks;

        if (!EnableClientSideSeek(info))
        {
            ticks += info.StartPositionTicks;
        }

        return ticks;
    }

    private PlaybackStartInfo GetProgressInfo(StreamParams info)
    {
        return new PlaybackStartInfo
        {
            ItemId = info.ItemId,
            SessionId = _session.Id,
            PositionTicks = GetProgressPositionTicks(info),
            IsMuted = _device.IsMuted,
            IsPaused = _device.IsPaused,
            MediaSourceId = info.MediaSourceId,
            AudioStreamIndex = info.AudioStreamIndex,
            SubtitleStreamIndex = info.SubtitleStreamIndex,
            VolumeLevel = _device.Volume,

            CanSeek = true,

            PlayMethod = info.IsDirectStream ? PlayMethod.DirectStream : PlayMethod.Transcode
        };
    }

    /// <summary>
    /// Sends a play command.
    /// </summary>
    /// <param name="command">The <see cref="PlayRequest"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous play command sending operation.</returns>
    public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{0} - Received PlayRequest: {1}", _session.DeviceName, command.PlayCommand);

        var user = command.ControllingUserId.IsEmpty()
            ? null :
            _userManager.GetUserById(command.ControllingUserId);

        var items = new List<BaseItem>();
        foreach (var id in command.ItemIds)
        {
            AddItemFromId(id, items);
        }

        var startIndex = command.StartIndex ?? 0;

        if (startIndex > items.Count)
        {
            _logger.LogDebug("{DeviceName} - Play command resulted in no items", _session.DeviceName);
            return Task.CompletedTask;
        }

        int len = items.Count - startIndex;
        if (startIndex > 0)
        {
            items = items.GetRange(startIndex, len);
        }

        var playlist = new PlaylistItem[len];

        // Not nullable enabled - so this is required.
        playlist[0] = CreatePlaylistItem(
            items[0],
            user,
            command.StartPositionTicks ?? 0,
            command.MediaSourceId ?? string.Empty,
            command.AudioStreamIndex,
            command.SubtitleStreamIndex);

        for (int i = 1; i < len; i++)
        {
            playlist[i] = CreatePlaylistItem(items[i], user, 0, string.Empty, null, null);
        }

        _logger.LogDebug("{0} - Playlist created", _session.DeviceName);

        if (command.PlayCommand == PlayCommand.PlayLast)
        {
            _playlist.AddRange(playlist);
        }

        if (command.PlayCommand == PlayCommand.PlayNext)
        {
            _playlist.AddRange(playlist);
        }

        if (!command.ControllingUserId.IsEmpty())
        {
            _sessionManager.LogSessionActivity(
                _session.Client,
                _session.ApplicationVersion,
                _session.DeviceId,
                _session.DeviceName,
                _session.RemoteEndPoint,
                user);
        }

        return PlayItems(playlist, cancellationToken);
    }

    private Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
    {
        switch (command.Command)
        {
            case PlaystateCommand.Stop:
                _playlist.Clear();
                return _device.SetStop(CancellationToken.None);

            case PlaystateCommand.Pause:
                return _device.SetPause(CancellationToken.None);

            case PlaystateCommand.Unpause:
                return _device.SetPlay(CancellationToken.None);

            case PlaystateCommand.PlayPause:
                return _device.IsPaused ? _device.SetPlay(CancellationToken.None) : _device.SetPause(CancellationToken.None);

            case PlaystateCommand.Seek:
                return Seek(command.SeekPositionTicks ?? 0);

            case PlaystateCommand.NextTrack:
                return SetPlaylistIndex(_currentPlaylistIndex + 1, cancellationToken);

            case PlaystateCommand.PreviousTrack:
                return SetPlaylistIndex(_currentPlaylistIndex - 1, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task Seek(long newPosition)
    {
        var media = _device.CurrentMediaInfo;

        if (media is not null)
        {
            var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

            if (info.Item is not null && !EnableClientSideSeek(info))
            {
                var user = _session.UserId.IsEmpty()
                    ? null
                    : _userManager.GetUserById(_session.UserId);
                var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, info.SubtitleStreamIndex);

                await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl, CancellationToken.None).ConfigureAwait(false);

                // Send a message to the DLNA device to notify what is the next track in the play list.
                var newItemIndex = _playlist.FindIndex(item => item.StreamUrl == newItem.StreamUrl);
                await SendNextTrackMessage(newItemIndex, CancellationToken.None).ConfigureAwait(false);

                return;
            }

            await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static bool EnableClientSideSeek(StreamParams info)
    {
        return info.IsDirectStream;
    }

    private static bool EnableClientSideSeek(StreamInfo info)
    {
        return info.IsDirectStream;
    }

    private void AddItemFromId(Guid id, List<BaseItem> list)
    {
        var item = _libraryManager.GetItemById(id);
        if (item?.MediaType == MediaType.Audio || item?.MediaType == MediaType.Video)
        {
            list.Add(item);
        }
    }

    private PlaylistItem CreatePlaylistItem(
        BaseItem item,
        User? user,
        long startPostionTicks,
        string? mediaSourceId,
        int? audioStreamIndex,
        int? subtitleStreamIndex)
    {
        var deviceInfo = _device.Properties;

        var profile = _dlnaManager.GetProfile(deviceInfo.ToDeviceIdentification()) ??
                      _dlnaManager.GetDefaultProfile();

        var mediaSources = item is IHasMediaSources
            ? _mediaSourceManager.GetStaticMediaSources(item, true, user).ToArray()
            : [];

        var playlistItem = GetPlaylistItem(item, mediaSources, profile, _session.DeviceId, mediaSourceId, audioStreamIndex, subtitleStreamIndex);
        playlistItem.StreamInfo.StartPositionTicks = startPostionTicks;

        playlistItem.StreamUrl = DidlBuilder.NormalizeDlnaMediaUrl(playlistItem.StreamInfo.ToDlnaUrl(_serverAddress, _accessToken));

        var itemXml = new DidlBuilder(
                profile,
                user,
                _imageProcessor,
                _serverAddress,
                _accessToken,
                _userDataManager,
                _localization,
                _mediaSourceManager,
                _logger,
                _mediaEncoder,
                _libraryManager)
            .GetItemDidl(item, user, null, _session.DeviceId, new Filter(), playlistItem.StreamInfo);

        playlistItem.Didl = itemXml;

        return playlistItem;
    }

    private static string? GetDlnaHeaders(PlaylistItem item)
    {
        var profile = item.Profile;
        var streamInfo = item.StreamInfo;

        if (streamInfo.MediaType == DlnaProfileType.Audio)
        {
            return ContentFeatureBuilder.BuildAudioHeader(
                profile,
                streamInfo.Container,
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                streamInfo.TargetAudioBitrate,
                streamInfo.TargetAudioSampleRate,
                streamInfo.TargetAudioChannels,
                streamInfo.TargetAudioBitDepth,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks ?? 0,
                streamInfo.TranscodeSeekInfo);
        }

        if (streamInfo.MediaType == DlnaProfileType.Video)
        {
            var list = ContentFeatureBuilder.BuildVideoHeader(
                profile,
                streamInfo.Container,
                streamInfo.TargetVideoCodec.FirstOrDefault(),
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                streamInfo.TargetWidth,
                streamInfo.TargetHeight,
                streamInfo.TargetVideoBitDepth,
                streamInfo.TargetVideoBitrate,
                streamInfo.TargetTimestamp,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks ?? 0,
                streamInfo.TargetVideoProfile,
                streamInfo.TargetVideoRangeType,
                streamInfo.TargetVideoLevel,
                streamInfo.TargetFramerate ?? 0,
                streamInfo.TargetPacketLength,
                streamInfo.TranscodeSeekInfo,
                streamInfo.IsTargetAnamorphic,
                streamInfo.IsTargetInterlaced,
                streamInfo.TargetRefFrames,
                streamInfo.TargetVideoStreamCount,
                streamInfo.TargetAudioStreamCount,
                streamInfo.GetStreamCount(),
                streamInfo.TargetVideoCodecTag,
                streamInfo.IsTargetAVC);

            return list.FirstOrDefault();
        }

        return null;
    }

    private PlaylistItem GetPlaylistItem(BaseItem item, MediaSourceInfo[] mediaSources, DlnaDeviceProfile profile, string deviceId, string? mediaSourceId, int? audioStreamIndex, int? subtitleStreamIndex)
        => item.MediaType switch
        {
            MediaType.Video => new PlaylistItem
            {
                StreamInfo = new StreamBuilder(_mediaEncoder, _logger).GetOptimalVideoStream(new MediaOptions
                {
                    ItemId = item.Id,
                    MediaSources = mediaSources,
                    Profile = profile,
                    DeviceId = deviceId,
                    MaxBitrate = profile.MaxStreamingBitrate,
                    MediaSourceId = mediaSourceId,
                    AudioStreamIndex = audioStreamIndex,
                    SubtitleStreamIndex = subtitleStreamIndex,
                    EnableDirectStream = false
                }),
                Profile = profile
            },
            MediaType.Audio => new PlaylistItem
            {
                StreamInfo = new StreamBuilder(_mediaEncoder, _logger).GetOptimalAudioStream(new MediaOptions
                {
                    ItemId = item.Id,
                    MediaSources = mediaSources,
                    Profile = profile,
                    DeviceId = deviceId,
                    MaxBitrate = profile.MaxStreamingBitrate,
                    MediaSourceId = mediaSourceId
                }),
                Profile = profile
            },
            MediaType.Photo => PlaylistItemFactory.Create((Photo)item, profile),
            _ => throw new ArgumentException("Unrecognized item type.")
        };

    /// <summary>
    /// Plays the items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> on success.</returns>
    private async Task<bool> PlayItems(IEnumerable<PlaylistItem> items, CancellationToken cancellationToken = default)
    {
        _playlist.Clear();
        _playlist.AddRange(items);
        _logger.LogDebug("{0} - Playing {1} items", _session.DeviceName, _playlist.Count);

        await SetPlaylistIndex(0, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task SetPlaylistIndex(int index, CancellationToken cancellationToken = default)
    {
        if (index < 0 || index >= _playlist.Count)
        {
            _playlist.Clear();
            await _device.SetStop(cancellationToken).ConfigureAwait(false);
            return;
        }

        _currentPlaylistIndex = index;
        var currentitem = _playlist[index];

        await _device.SetAvTransport(currentitem.StreamUrl, GetDlnaHeaders(currentitem), currentitem.Didl, cancellationToken).ConfigureAwait(false);

        // Send a message to the DLNA device to notify what is the next track in the play list.
        await SendNextTrackMessage(index, cancellationToken).ConfigureAwait(false);

        var streamInfo = currentitem.StreamInfo;
        if (streamInfo.StartPositionTicks > 0 && EnableClientSideSeek(streamInfo))
        {
            await SeekAfterTransportChange(streamInfo.StartPositionTicks, CancellationToken.None).ConfigureAwait(false);
        }
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
            _device.PlaybackStart -= OnDevicePlaybackStart;
            _device.PlaybackProgress -= OnDevicePlaybackProgress;
            _device.PlaybackStopped -= OnDevicePlaybackStopped;
            _device.MediaChanged -= OnDeviceMediaChanged;
            _deviceDiscovery.DeviceLeft -= OnDeviceDiscoveryDeviceLeft;
            _device.OnDeviceUnavailable = null;
            _device.Dispose();
        }

        _disposed = true;
    }

    private Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
    {
        switch (command.Name)
        {
            case GeneralCommandType.VolumeDown:
                return _device.VolumeDown(cancellationToken);
            case GeneralCommandType.VolumeUp:
                return _device.VolumeUp(cancellationToken);
            case GeneralCommandType.Mute:
                return _device.Mute(cancellationToken);
            case GeneralCommandType.Unmute:
                return _device.Unmute(cancellationToken);
            case GeneralCommandType.ToggleMute:
                return _device.ToggleMute(cancellationToken);
            case GeneralCommandType.SetAudioStreamIndex:
                if (command.Arguments.TryGetValue("Index", out string? index))
                {
                    if (int.TryParse(index, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                    {
                        return SetAudioStreamIndex(val);
                    }

                    throw new ArgumentException("Unsupported SetAudioStreamIndex value supplied.");
                }

                throw new ArgumentException("SetAudioStreamIndex argument cannot be null");
            case GeneralCommandType.SetSubtitleStreamIndex:
                if (command.Arguments.TryGetValue("Index", out index))
                {
                    if (int.TryParse(index, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                    {
                        return SetSubtitleStreamIndex(val);
                    }

                    throw new ArgumentException("Unsupported SetSubtitleStreamIndex value supplied.");
                }

                throw new ArgumentException("SetSubtitleStreamIndex argument cannot be null");
            case GeneralCommandType.SetVolume:
                if (command.Arguments.TryGetValue("Volume", out string? vol))
                {
                    if (int.TryParse(vol, NumberStyles.Integer, CultureInfo.InvariantCulture, out var volume))
                    {
                        return _device.SetVolume(volume, cancellationToken);
                    }

                    throw new ArgumentException("Unsupported volume value supplied.");
                }

                throw new ArgumentException("Volume argument cannot be null");
            default:
                return Task.CompletedTask;
        }
    }

    private async Task SetAudioStreamIndex(int? newIndex)
    {
        var media = _device.CurrentMediaInfo;

        if (media is not null)
        {
            var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

            if (info.Item is not null)
            {
                var newPosition = GetProgressPositionTicks(info) ?? 0;

                var user = _session.UserId.IsEmpty()
                    ? null
                    : _userManager.GetUserById(_session.UserId);
                var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, newIndex, info.SubtitleStreamIndex);

                await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl, CancellationToken.None).ConfigureAwait(false);

                // Send a message to the DLNA device to notify what is the next track in the play list.
                var newItemIndex = _playlist.FindIndex(item => item.StreamUrl == newItem.StreamUrl);
                await SendNextTrackMessage(newItemIndex, CancellationToken.None).ConfigureAwait(false);

                if (EnableClientSideSeek(newItem.StreamInfo))
                {
                    await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task SetSubtitleStreamIndex(int? newIndex)
    {
        var media = _device.CurrentMediaInfo;

        if (media is not null)
        {
            var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

            if (info.Item is not null)
            {
                var newPosition = GetProgressPositionTicks(info) ?? 0;

                var user = _session.UserId.IsEmpty()
                    ? null
                    : _userManager.GetUserById(_session.UserId);
                var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, newIndex);

                await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl, CancellationToken.None).ConfigureAwait(false);

                // Send a message to the DLNA device to notify what is the next track in the play list.
                var newItemIndex = _playlist.FindIndex(item => item.StreamUrl == newItem.StreamUrl);
                await SendNextTrackMessage(newItemIndex, CancellationToken.None).ConfigureAwait(false);

                if (EnableClientSideSeek(newItem.StreamInfo) && newPosition > 0)
                {
                    await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task SeekAfterTransportChange(long positionTicks, CancellationToken cancellationToken)
    {
        const int MaxWait = 15000000;
        const int Interval = 500;

        var currentWait = 0;
        while (_device.TransportState != TransportState.PLAYING && currentWait < MaxWait)
        {
            await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
            currentWait += Interval;
        }

        await _device.Seek(TimeSpan.FromTicks(positionTicks), cancellationToken).ConfigureAwait(false);
    }

    private static int? GetIntValue(IReadOnlyDictionary<string, string> values, string name)
    {
        var value = values.GetValueOrDefault(name);

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static long GetLongValue(IReadOnlyDictionary<string, string> values, string name)
    {
        var value = values.GetValueOrDefault(name);

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0;
    }

    /// <inheritdoc />
    public Task SendMessage<T>(SessionMessageType name, Guid messageId, T data, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return name switch
        {
            SessionMessageType.Play => SendPlayCommand((data as PlayRequest)!, cancellationToken),
            SessionMessageType.Playstate => SendPlaystateCommand((data as PlaystateRequest)!, cancellationToken),
            SessionMessageType.GeneralCommand => SendGeneralCommand((data as GeneralCommand)!, cancellationToken),
            _ => Task.CompletedTask // Not supported or needed right now
        };
    }

    private sealed class StreamParams
    {
        private MediaSourceInfo? _mediaSource;
        private IMediaSourceManager? _mediaSourceManager;

        public Guid ItemId { get; set; }

        public bool IsDirectStream { get; set; }

        public long StartPositionTicks { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public string? DeviceProfileId { get; set; }

        public string? DeviceId { get; set; }

        public string? MediaSourceId { get; set; }

        public string? LiveStreamId { get; set; }

        public BaseItem? Item { get; set; }

        public async Task<MediaSourceInfo?> GetMediaSource(CancellationToken cancellationToken)
        {
            if (_mediaSource is not null)
            {
                return _mediaSource;
            }

            if (Item is not IHasMediaSources)
            {
                return null;
            }

            if (_mediaSourceManager is not null)
            {
                _mediaSource = await _mediaSourceManager.GetMediaSource(Item, MediaSourceId, LiveStreamId, false, cancellationToken).ConfigureAwait(false);
            }

            return _mediaSource;
        }

        private static Guid GetItemId(string url)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);

            var parts = url.Split('/');

            for (var i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];

                if (string.Equals(part, "audio", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part, "videos", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(parts[i + 1], out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        public static StreamParams ParseFromUrl(string url, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);

            var request = new StreamParams
            {
                ItemId = GetItemId(url)
            };

            if (request.ItemId.IsEmpty())
            {
                return request;
            }

            var index = url.IndexOf('?', StringComparison.Ordinal);
            if (index == -1)
            {
                return request;
            }

            var query = url[(index + 1)..];
            Dictionary<string, string> values = QueryHelpers.ParseQuery(query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            request.DeviceProfileId = values.GetValueOrDefault("DeviceProfileId");
            request.DeviceId = values.GetValueOrDefault("DeviceId");
            request.MediaSourceId = values.GetValueOrDefault("MediaSourceId");
            request.LiveStreamId = values.GetValueOrDefault("LiveStreamId");
            request.IsDirectStream = string.Equals("true", values.GetValueOrDefault("Static"), StringComparison.OrdinalIgnoreCase);
            request.AudioStreamIndex = GetIntValue(values, "AudioStreamIndex");
            request.SubtitleStreamIndex = GetIntValue(values, "SubtitleStreamIndex");
            request.StartPositionTicks = GetLongValue(values, "StartPositionTicks");

            request.Item = libraryManager.GetItemById(request.ItemId);

            request._mediaSourceManager = mediaSourceManager;

            return request;
        }
    }
}
