using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.PlayTo;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IDeviceDiscovery = Jellyfin.Plugin.Dlna.Model.IDeviceDiscovery;
using IDlnaManager = Jellyfin.Plugin.Dlna.Model.IDlnaManager;

namespace Jellyfin.Plugin.Dlna.Tests;

public sealed class PlayToSessionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AudioPreparationPreservesPublishedIdsOnlyWhenParentIsKnown(bool hasParent)
    {
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        using var session = CreateSession(device);
        var audio = new Audio { Id = Guid.NewGuid(), ParentId = hasParent ? Guid.NewGuid() : Guid.Empty };
        var sources = new[] { new MediaSourceInfo
        {
            Id = "source", Path = "/music/track.mp3", Container = "mp3", Protocol = MediaProtocol.File,
            SupportsDirectPlay = true, SupportsDirectStream = true,
            MediaStreams = new[] { new MediaStream { Type = MediaStreamType.Audio, Codec = "mp3", Index = 0, Channels = 2, SampleRate = 44100, BitRate = 128000 } }
        } };
        var profile = new DlnaDeviceProfile { DirectPlayProfiles = new[] { new DirectPlayProfile { Type = DlnaProfileType.Audio, Container = "mp3", AudioCodec = "mp3" } } };
        var item = (PlaylistItem)typeof(PlayToSession).GetMethod("GetPlaylistItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(session, new object?[] { audio, sources, profile, "test", null, null, null })!;

        if (hasParent)
            Assert.Equal((DidlBuilder.GetClientId(audio, null), DidlBuilder.GetClientId(audio.DisplayParentId, null)), item.DlnaIds);
        else
            Assert.Null(item.DlnaIds);
    }

    [Fact]
    public async Task DisabledOptionUsesClassicTransportEvenForDenon()
    {
        ApplyConfiguration(false);
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        device.Properties.Manufacturer = "Denon";
        using var session = CreateSession(device);
        Assert.True(await PlayAsync(session, HeosQueueTests.Track(1), HeosQueueTests.Track(2)));
        await session.SendMessage(SessionMessageType.Playstate, Guid.NewGuid(), new PlaystateRequest { Command = PlaystateCommand.NextTrack }, CancellationToken.None);
        Assert.Null(GetField(session, "_heosQueue"));
        Assert.Equal(2, renderer.Commands.Count(c => c.Action == "SetAVTransportURI"));
        Assert.DoesNotContain(renderer.Commands, c => c.Action == "Next");
    }

    [Fact]
    public async Task OtherManufacturersKeepClassicTransportWhenOptionEnabled()
    {
        ApplyConfiguration(true);
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        using var session = CreateSession(device);
        Assert.True(await PlayAsync(session, HeosQueueTests.Track(1)));
        Assert.Null(GetField(session, "_heosQueue"));
        Assert.Contains(renderer.Commands, c => c.Action == "SetAVTransportURI");
    }

    [Fact]
    public async Task SharedDlnaSenderPreservesPlayPauseAndStop()
    {
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        await device.SetPlay(CancellationToken.None);
        await device.SetPause(CancellationToken.None);
        Assert.True(device.IsPaused);
        await device.SetStop(CancellationToken.None);
        Assert.Equal(new[] { "Play", "Pause", "Stop" }, renderer.Commands.Select(c => c.Action));
    }

    [Fact]
    public async Task NativeModeUsesDlnaNavigationWithoutReloadingTrackOrContactingHeos()
    {
        await using var server = new HeosQueueTests.FakeHeos();
        var native = server.CreateQueue();
        var items = new[] { HeosQueueTests.Track(1), HeosQueueTests.Track(2) };
        Assert.True(await native.QueueAsync(items, PlayCommand.PlayNow, CancellationToken.None));
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        using var session = CreateSession(device);
        ((List<PlaylistItem>)GetField(session, "_playlist")!).AddRange(items);
        SetField(session, "_heosQueue", native);
        SetField(device, "<TransportState>k__BackingField", TransportState.PLAYING);
        var infoType = typeof(PlayToSession).GetNestedType("StreamParams", BindingFlags.NonPublic)!;
        var info = Activator.CreateInstance(infoType)!;
        infoType.GetProperty("ItemId")!.SetValue(info, items[0].StreamInfo.ItemId);
        await (Task)typeof(PlayToSession).GetMethod("SyncWithReportedMedia", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(session, new[] { info, items[0].StreamUrl, CancellationToken.None })!;

        var heosCommands = server.Commands.ToArray();
        await session.SendMessage(SessionMessageType.Playstate, Guid.NewGuid(), new PlaystateRequest { Command = PlaystateCommand.NextTrack }, CancellationToken.None);
        await session.SendMessage(SessionMessageType.Playstate, Guid.NewGuid(), new PlaystateRequest { Command = PlaystateCommand.PreviousTrack }, CancellationToken.None);

        Assert.Equal(heosCommands, server.Commands.ToArray());
        Assert.Contains(renderer.Commands, c => c.Action == "Next");
        Assert.Contains(renderer.Commands, c => c.Action == "Previous");
        Assert.DoesNotContain(renderer.Commands, c => c.Action is "SetNextAVTransportURI" or "SetAVTransportURI");
    }

    [Fact]
    public async Task MissingDlnaNavigationIsReportedInsteadOfReloadingOrCallingHeos()
    {
        using var renderer = new Renderer { SupportsNavigation = false };
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        using var session = CreateSession(device);
        SetField(session, "_heosQueue", new HeosQueue("127.0.0.1", "Music"));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SendMessage(SessionMessageType.Playstate, Guid.NewGuid(),
            new PlaystateRequest { Command = PlaystateCommand.NextTrack }, CancellationToken.None));
        Assert.DoesNotContain(renderer.Commands, c => c.Action is "Next" or "SetAVTransportURI");
    }

    [Fact]
    public async Task NativeFailureReturnsControlToClassicDlna()
    {
        await using var server = new HeosQueueTests.FakeHeos { Scenario = "fail_second_add" };
        using var renderer = new Renderer();
        using var device = new Device(renderer.Properties(), renderer, NullLogger.Instance);
        using var session = CreateSession(device);
        SetField(session, "_heosQueue", server.CreateQueue());
        var items = new[] { HeosQueueTests.Track(1), HeosQueueTests.Track(2) };
        var queued = await (Task<bool>)typeof(PlayToSession).GetMethod("QueueInHeosAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(session, new object[] { items, PlayCommand.PlayNow, CancellationToken.None })!;
        Assert.False(queued);
        Assert.Null(GetField(session, "_heosQueue"));
        Assert.Contains(renderer.Commands, c => c.Action == "SetNextAVTransportURI" && string.IsNullOrEmpty(c.NextUri));
    }

    internal static void ApplyConfiguration(bool enabled)
    {
        var paths = new Mock<IApplicationPaths>();
        paths.SetReturnsDefault(Path.Combine(Path.GetTempPath(), "heos-tests-" + Guid.NewGuid()));
        paths.SetupGet(p => p.PluginConfigurationsPath).Returns(Path.Combine(Path.GetTempPath(), "heos-tests-" + Guid.NewGuid()));
        var serializer = new Mock<IXmlSerializer>();
        serializer.Setup(s => s.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>())).Returns(new DlnaPluginConfiguration());
        var plugin = new DlnaPlugin(paths.Object, serializer.Object);
        plugin.Configuration.EnableNativeHeosQueue = enabled;
    }

    internal static PlayToSession CreateSession(Device device, string serverName = "Music")
    {
        var manager = new Mock<IDlnaManager>();
        manager.Setup(m => m.GetServerName()).Returns(serverName);
        var library = new Mock<ILibraryManager>();
        library.Setup(l => l.GetItemById(It.IsAny<Guid>())).Returns((Guid id) => new Audio { Id = id, RunTimeTicks = TimeSpan.FromMinutes(3).Ticks });
        var sessions = Mock.Of<ISessionManager>();
        return new PlayToSession(new SessionInfo(sessions, NullLogger.Instance) { Id = "heos-test" }, sessions, library.Object,
            NullLogger.Instance, manager.Object, null!, null!, "http://media.invalid", null,
            Mock.Of<IDeviceDiscovery>(), null!, null!, Mock.Of<IMediaSourceManager>(), null!, device);
    }

    internal static Task<bool> PlayAsync(PlayToSession session, params PlaylistItem[] items)
        => (Task<bool>)typeof(PlayToSession).GetMethod("PlayItems", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(session, new object[] { items, CancellationToken.None })!;

    private static object? GetField(object target, string name)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);

    private static void SetField(object target, string name, object value)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private sealed class Renderer : HttpMessageHandler, IHttpClientFactory
    {
        private readonly HttpClient _client;
        private string _state = "STOPPED";
        private string _uri = string.Empty;
        private string _metadata = string.Empty;

        public Renderer() => _client = new HttpClient(this, disposeHandler: false);

        internal ConcurrentQueue<(string Action, string? NextUri)> Commands { get; } = new();

        internal bool SupportsNavigation { get; set; } = true;

        internal DeviceInfo Properties() => new()
        {
            Manufacturer = "Other", BaseUrl = "http://renderer.invalid",
            Services = new[]
            {
                new DeviceService { ServiceType = "urn:schemas-upnp-org:service:AVTransport:1", ScpdUrl = "/av.xml", ControlUrl = "/av" },
                new DeviceService { ServiceType = "urn:schemas-upnp-org:service:RenderingControl:1", ScpdUrl = "/render.xml", ControlUrl = "/render" },
                new DeviceService { ServiceType = "urn:schemas-denon-com:service:ACT:1" }
            }
        };

        public HttpClient CreateClient(string name) => _client;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string xml;
            if (request.Method == HttpMethod.Get)
            {
                var actions = new[] { "SetAVTransportURI", "SetNextAVTransportURI", "Stop", "Play", "Pause", "GetTransportInfo", "GetPositionInfo", "Next", "Previous" }
                    .Where(a => SupportsNavigation || a is not ("Next" or "Previous"));
                xml = "<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\"><actionList>"
                    + string.Concat(actions.Select(a => $"<action><name>{a}</name><argumentList>"
                        + string.Concat((a switch
                        {
                            "SetAVTransportURI" => new[] { "InstanceID", "CurrentURI", "CurrentURIMetaData" },
                            "SetNextAVTransportURI" => new[] { "InstanceID", "NextURI", "NextURIMetaData" },
                            "Play" => new[] { "InstanceID", "Speed" },
                            _ => new[] { "InstanceID" }
                        }).Select(n => $"<argument><name>{n}</name><direction>in</direction></argument>"))
                        + "</argumentList></action>"))
                    + "</actionList><serviceStateTable /></scpd>";
            }
            else
            {
                var command = request.Headers.GetValues("SOAPACTION").Single().Trim('"').Split('#')[1];
                var body = XDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Assert.Equal("0", body.Descendants("InstanceID").Single().Value);
                if (command == "Play") Assert.Equal("1", body.Descendants("Speed").Single().Value);
                Commands.Enqueue((command, body.Descendants("NextURI").FirstOrDefault()?.Value));
                if (command == "Stop") _state = "STOPPED";
                if (command == "Play") _state = "PLAYING";
                if (command == "Pause") _state = "PAUSED_PLAYBACK";
                if (command == "SetAVTransportURI")
                {
                    _uri = body.Descendants("CurrentURI").Single().Value;
                    _metadata = body.Descendants("CurrentURIMetaData").Single().Value;
                }

                var response = new XElement(UPnpNamespaces.AvTransport + (command + "Response"));
                if (command == "GetTransportInfo") response.Add(new XElement("CurrentTransportState", _state));
                if (command == "GetPositionInfo") response.Add(new XElement("TrackURI", _uri), new XElement("TrackMetaData", _metadata), new XElement("RelTime", "00:00:01"), new XElement("TrackDuration", "00:03:00"));
                xml = response.ToString();
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml) };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _client.Dispose();
            base.Dispose(disposing);
        }
    }
}
