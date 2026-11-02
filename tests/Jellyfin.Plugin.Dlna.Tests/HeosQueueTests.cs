using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.PlayTo;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public sealed class HeosQueueTests
{
    [Theory]
    [InlineData("Denon", true)]
    [InlineData("Marantz", true)]
    [InlineData("Samsung", false)]
    [InlineData("", false)]
    public void OnlyCandidatesUseHeos(string manufacturer, bool expected)
    {
        var device = new DeviceInfo
        {
            Manufacturer = manufacturer,
            BaseUrl = "http://192.0.2.4:60006",
            Services = new[] { new DeviceService { ServiceType = "urn:schemas-denon-com:service:ACT:1" } }
        };
        Assert.False(new DlnaPluginConfiguration().EnableNativeHeosQueue);
        Assert.Null(HeosQueue.Create(device, "Music", false));
        Assert.Equal(expected, HeosQueue.Create(device, "Music", true) is not null);
        device.Services = Array.Empty<DeviceService>();
        Assert.Null(HeosQueue.Create(device, "Music", true));
    }

    [Fact]
    public async Task QueueUsesDiscoveredIdsAndPreservesSelectionOrderAndDuplicates()
    {
        await using var server = new FakeHeos();
        var queue = server.CreateQueue();
        var items = new[] { Track(2), Track(1), Track(2) };

        Assert.True(await queue.QueueAsync(items, PlayCommand.PlayNow, CancellationToken.None));

        var mutations = server.Commands.Where(c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, mutations.Length);
        Assert.Contains("pid=928&sid=-72", mutations[0], StringComparison.Ordinal);
        Assert.Contains("mid=" + items[0].StreamInfo.ItemId.ToString("N"), mutations[0], StringComparison.Ordinal);
        Assert.EndsWith("aid=4", mutations[0], StringComparison.Ordinal);
        Assert.EndsWith("aid=3", mutations[1], StringComparison.Ordinal);
        Assert.EndsWith("aid=3", mutations[2], StringComparison.Ordinal);
        Assert.Contains("mid=" + items[2].StreamInfo.ItemId.ToString("N"), mutations[2], StringComparison.Ordinal);
        Assert.True(Array.FindLastIndex(server.Commands.ToArray(), c => c.StartsWith("browse/browse", StringComparison.Ordinal))
            < Array.FindIndex(server.Commands.ToArray(), c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal)));

    }

    [Theory]
    [InlineData(PlayCommand.PlayNext, "aid=2", 2)]
    [InlineData(PlayCommand.PlayLast, "aid=3", 1)]
    public async Task AdditionsUseNativeQueueAndReversePlayNextInsertionOrder(PlayCommand mode, string criteria, int first)
    {
        await using var server = new FakeHeos();
        Assert.True(await server.CreateQueue().QueueAsync(new[] { Track(1), Track(2) }, mode, CancellationToken.None));
        var mutations = server.Commands.Where(c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal)).ToArray();
        Assert.Contains("mid=" + Track(first).StreamInfo.ItemId.ToString("N"), mutations[0], StringComparison.Ordinal);
        Assert.All(mutations, c => Assert.EndsWith(criteria, c, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate_source")]
    [InlineData("different_player")]
    [InlineData("wrong_source")]
    public async Task UnconfirmedTargetOrContentDoesNotMutateQueue(string scenario)
    {
        await using var server = new FakeHeos { Scenario = scenario };
        var queue = server.CreateQueue();
        Assert.False(await queue.QueueAsync(new[] { Track(1), Track(2) }, PlayCommand.PlayNow, CancellationToken.None));
        Assert.False(queue.QueueChanged);
        Assert.DoesNotContain(server.Commands, c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("video")]
    [InlineData("transcode")]
    [InlineData("resume")]
    [InlineData("ids")]
    public async Task UnsupportedSelectionDoesNotContactHeos(string scenario)
    {
        await using var server = new FakeHeos();
        var item = Track(1);
        if (scenario == "video") item.StreamInfo.MediaType = DlnaProfileType.Video;
        if (scenario == "transcode") item.StreamInfo.PlayMethod = PlayMethod.Transcode;
        if (scenario == "resume") item.StreamInfo.StartPositionTicks = 100;
        if (scenario == "ids") item.DlnaIds = null;

        Assert.False(await server.CreateQueue().QueueAsync(new[] { item }, PlayCommand.PlayNow, CancellationToken.None));
        Assert.Empty(server.Commands);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid XML")]
    [InlineData("<DIDL-Lite><item id=\"different\" parentID=\"other\" /></DIDL-Lite>")]
    public async Task QueueUsesStructuredIdsIndependentlyOfDidl(string didl)
    {
        await using var server = new FakeHeos();
        var item = Track(1);
        item.Didl = didl;
        Assert.True(await server.CreateQueue().QueueAsync(new[] { item }, PlayCommand.PlayNow, CancellationToken.None));
        Assert.Contains(server.Commands, c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal)
            && c.Contains($"cid={item.DlnaIds!.Value.Parent}&mid={item.DlnaIds.Value.Id}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailedMutationIsReportedAndDoesNotContinueBuildingQueue()
    {
        await using var server = new FakeHeos { Scenario = "fail_second_add" };
        await Assert.ThrowsAsync<IOException>(() => server.CreateQueue().QueueAsync(new[] { Track(1), Track(2), Track(1) }, PlayCommand.PlayNow, CancellationToken.None));
        Assert.Equal(2, server.Commands.Count(c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task PendingResponseAndEventsAreIgnoredAndBrowseIsPaginated()
    {
        await using var server = new FakeHeos { Scenario = "pagination" };
        Assert.True(await server.CreateQueue().QueueAsync(new[] { Track(2) }, PlayCommand.PlayNow, CancellationToken.None));
        Assert.Contains(server.Commands, c => c.Contains("range=100,199", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancellationInterruptsPendingResponseBeforeQueueMutation()
    {
        await using var server = new FakeHeos { Scenario = "hang" };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.CreateQueue().QueueAsync(new[] { Track(1) }, PlayCommand.PlayNow, cancellation.Token));
        Assert.DoesNotContain(server.Commands, c => c.StartsWith("browse/add_to_queue", StringComparison.Ordinal));
    }

    internal static PlaylistItem Track(int number)
    {
        var id = Guid.Parse($"00000000-0000-0000-0000-{number:D12}");
        return new PlaylistItem
        {
            DlnaIds = (id.ToString("N"), "00000000000000000000000000000099"),
            StreamInfo = new StreamInfo { ItemId = id, MediaType = DlnaProfileType.Audio, PlayMethod = PlayMethod.DirectPlay, DeviceProfile = new DlnaDeviceProfile() },
            Profile = new DlnaDeviceProfile(),
            StreamUrl = $"http://media.invalid/dlna/audio/{id}/stream.mp3?Static=true",
            Didl = $"<DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\" xmlns:u=\"urn:schemas-upnp-org:metadata-1-0/upnp/\"><item id=\"{id:N}\" parentID=\"00000000000000000000000000000099\"><u:class>object.item.audioItem.musicTrack</u:class></item></DIDL-Lite>"
        };
    }

    internal sealed class FakeHeos : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _accept;
        private readonly List<Task> _clients = new();
        private int _adds;

        internal FakeHeos()
        {
            _listener.Start();
            _accept = AcceptAsync();
        }

        internal ConcurrentQueue<string> Commands { get; } = new();

        internal string Scenario { get; set; } = string.Empty;

        internal HeosQueue CreateQueue() => new("127.0.0.1", "Music", ((IPEndPoint)_listener.LocalEndpoint).Port);

        private async Task AcceptAsync()
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                    _clients.Add(ServeAsync(client));
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ServeAsync(TcpClient client)
        {
            using (client)
            using (var reader = new StreamReader(client.GetStream()))
            using (var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true })
            {
                try
                {
                    while (await reader.ReadLineAsync(_stop.Token) is { } line)
                    {
                        Assert.StartsWith("heos://", line, StringComparison.Ordinal);
                        var command = line[7..];
                        Commands.Enqueue(command);
                        var name = command.Split('?')[0];
                        if (Scenario == "hang")
                        {
                            await Task.Delay(Timeout.Infinite, _stop.Token);
                        }

                        await writer.WriteLineAsync(Response("event/player_state_changed", new { }));
                        await writer.WriteLineAsync(Response(name, null, "command under process&sid=-72"));
                        object payload = Array.Empty<object>();
                        var fail = false;
                        if (name == "player/get_players")
                        {
                            payload = new[] { new { ip = Scenario == "different_player" ? "192.0.2.1" : "127.0.0.1", pid = 928 } };
                        }
                        else if (command.StartsWith("browse/browse?sid=1024", StringComparison.Ordinal))
                        {
                            var source = new { name = Scenario == "wrong_source" ? "Other" : "Music", sid = -72 };
                            payload = Scenario == "duplicate_source" ? new[] { source, source } : new[] { source };
                        }
                        else if (name == "browse/browse")
                        {
                            payload = Scenario == "pagination" && !command.Contains("range=100,", StringComparison.Ordinal)
                                ? Enumerable.Range(0, 100).Select(i => new { mid = Guid.Empty.ToString("N"), playable = "yes" }).ToArray()
                                : new[] { new { mid = Track(1).StreamInfo.ItemId.ToString("N"), playable = "yes" },
                                          new { mid = Track(2).StreamInfo.ItemId.ToString("N"), playable = Scenario == "missing" ? "no" : "yes" } };
                        }
                        else if (name == "browse/add_to_queue")
                        {
                            fail = ++_adds == 2 && Scenario == "fail_second_add";
                        }

                        await writer.WriteLineAsync(Response(name, payload, fail ? "eid=12&text=Failure" : "", !fail));
                    }
                }
                catch (OperationCanceledException) { }
                catch (IOException) { }
            }
        }

        private static string Response(string name, object? payload, string message = "", bool ok = true)
            => JsonSerializer.Serialize(new { heos = new { command = name, result = ok ? "success" : "fail", message }, payload });

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            await _accept;
            _listener.Stop();
            await Task.WhenAll(_clients);
            _stop.Dispose();
        }
    }
}
