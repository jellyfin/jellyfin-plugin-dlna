using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Loads local music into a confirmed HEOS player's native queue, without replacing DLNA discovery or status reporting.
/// </summary>
internal sealed class HeosQueue
{
    private readonly string _host;
    private readonly string _serverName;
    private readonly int _port;

    internal HeosQueue(string host, string serverName, int port = 1255)
    {
        _host = host;
        _serverName = serverName;
        _port = port;
    }

    internal bool QueueChanged { get; private set; }

    internal static HeosQueue? Create(DeviceInfo device, string serverName, bool enabled)
    {
        if (!enabled || (!device.Manufacturer.Contains("Denon", StringComparison.OrdinalIgnoreCase)
             && !device.Manufacturer.Contains("Marantz", StringComparison.OrdinalIgnoreCase))
            || !device.Services.Any(s => s.ServiceType.StartsWith("urn:schemas-denon-com:service:ACT:", StringComparison.OrdinalIgnoreCase))
            || string.IsNullOrWhiteSpace(serverName)
            || !Uri.TryCreate(device.BaseUrl, UriKind.Absolute, out var address))
        {
            return null;
        }

        return new HeosQueue(address.Host, serverName);
    }

    internal async Task<bool> QueueAsync(IReadOnlyList<PlaylistItem> items, PlayCommand mode, CancellationToken cancellationToken, Func<Task>? beforeReplace = null)
    {
        QueueChanged = false;
        if (items.Count == 0 || items.Any(item => item.DlnaIds is null || item.StreamInfo.MediaType != DlnaProfileType.Audio
            || !item.StreamInfo.IsDirectStream || item.StreamInfo.StartPositionTicks != 0))
        {
            return false;
        }

        var entries = items.Select(item => item.DlnaIds!.Value).ToArray();
        using var client = new Connection();
        await client.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
        var players = await client.SendAsync("player/get_players", cancellationToken).ConfigureAwait(false);
        var player = players.GetProperty("payload").EnumerateArray().SingleOrDefault(p => p.GetProperty("ip").GetString() == _host);
        if (player.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        var playerId = player.GetProperty("pid").ToString();
        var sources = await BrowseAsync(client, "sid=1024", cancellationToken).ConfigureAwait(false);
        var matches = sources.Where(p => string.Equals(p.GetProperty("name").GetString(), _serverName, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        var sourceId = matches[0].GetProperty("sid").ToString();
        // Check every selected item before changing the queue; names alone do not identify library content.
        foreach (var group in entries.GroupBy(e => e.Parent))
        {
            var content = await BrowseAsync(client, $"sid={sourceId}&cid={Uri.EscapeDataString(group.Key)}", cancellationToken).ConfigureAwait(false);
            if (group.Any(entry => !content.Any(p => p.TryGetProperty("mid", out var mid)
                && string.Equals(mid.GetString(), entry.Id, StringComparison.OrdinalIgnoreCase)
                && p.TryGetProperty("playable", out var playable) && playable.GetString() == "yes")))
            {
                return false;
            }
        }

        QueueChanged = true;
        if (mode == PlayCommand.PlayNow && beforeReplace is not null)
        {
            await beforeReplace().ConfigureAwait(false);
        }

        var ordered = mode == PlayCommand.PlayNext ? entries.AsEnumerable().Reverse() : entries;
        var first = true;
        foreach (var entry in ordered)
        {
            var criteria = mode == PlayCommand.PlayNext ? 2 : mode == PlayCommand.PlayNow && first ? 4 : 3;
            await client.SendAsync(
                $"browse/add_to_queue?pid={playerId}&sid={sourceId}&cid={Uri.EscapeDataString(entry.Parent)}&mid={Uri.EscapeDataString(entry.Id)}&aid={criteria}",
                cancellationToken).ConfigureAwait(false);
            first = false;
        }

        return true;
    }

    private static async Task<List<JsonElement>> BrowseAsync(Connection client, string query, CancellationToken cancellationToken)
    {
        var entries = new List<JsonElement>();
        for (var start = 0; ; start += 100)
        {
            var response = await client.SendAsync($"browse/browse?{query}&range={start.ToString(CultureInfo.InvariantCulture)},{(start + 99).ToString(CultureInfo.InvariantCulture)}", cancellationToken).ConfigureAwait(false);
            var page = response.GetProperty("payload").EnumerateArray().ToArray();
            entries.AddRange(page);
            if (page.Length < 100)
            {
                return entries;
            }
        }
    }

    private sealed class Connection : IDisposable
    {
        private readonly TcpClient _client = new();
        private StreamReader? _reader;
        private StreamWriter? _writer;

        internal async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream(), new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
        }

        internal async Task<JsonElement> SendAsync(string command, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _writer!.WriteLineAsync(("heos://" + command).AsMemory(), timeout.Token).ConfigureAwait(false);
            var name = command.Split('?')[0];
            while (true)
            {
                var line = await _reader!.ReadLineAsync(timeout.Token).ConfigureAwait(false);
                if (line is null)
                {
                    throw new IOException("HEOS closed the connection.");
                }

                using var response = JsonDocument.Parse(line);
                var header = response.RootElement.GetProperty("heos");
                if (header.GetProperty("command").GetString() != name
                    || header.GetProperty("message").GetString()?.StartsWith("command under process", StringComparison.Ordinal) == true)
                {
                    continue;
                }

                if (header.GetProperty("result").GetString() != "success")
                {
                    throw new IOException("HEOS refused " + name + ": " + header.GetProperty("message").GetString());
                }

                return response.RootElement.Clone();
            }
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _client.Dispose();
        }
    }
}
