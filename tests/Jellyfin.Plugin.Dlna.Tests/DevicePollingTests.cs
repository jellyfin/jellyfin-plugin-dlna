using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.PlayTo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

// Exercise the actual one-shot timer and HTTP polling, without a real renderer or timing hooks
// in production code. Each retry uses the normal ten-second polling interval.
public sealed class DevicePollingTests
{
    private static readonly TimeSpan _pollTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _quietWindow = TimeSpan.FromSeconds(11);

    [Fact]
    public async Task TransientFailuresRecoverAndSuccessfulPollResetsFailureCount()
    {
        using var renderer = new Renderer((attempt, _) => attempt switch
        {
            1 or 4 => throw new HttpRequestException("Simulated disconnect"),
            3 => throw new TaskCanceledException("Simulated request timeout"),
            2 => Task.FromResult("PLAYING"),
            _ => Task.FromResult("STOPPED")
        });
        using var device = renderer.CreateDevice();
        var starts = 0;
        var unavailable = 0;
        device.PlaybackStart += (_, _) => Interlocked.Increment(ref starts);
        device.OnDeviceUnavailable = () => Interlocked.Increment(ref unavailable);

        device.Start();
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await renderer.WaitForPollAsync();
        }

        await WaitUntilAsync(() => device.IsStopped);
        Assert.Equal(1, Volatile.Read(ref starts));
        Assert.Equal(0, Volatile.Read(ref unavailable));
        Assert.Equal(5, renderer.Polls);
        Assert.All(renderer.Commands, command => Assert.Contains(command, new[] { "GetTransportInfo", "GetPositionInfo" }));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThreeConsecutiveFailuresStopPollingWithOrWithoutUnavailableHandler(bool subscribe)
    {
        using var renderer = new Renderer((_, _) => throw new HttpRequestException("Simulated disconnect"));
        using var device = renderer.CreateDevice();
        var unavailable = 0;
        if (subscribe)
        {
            device.OnDeviceUnavailable = () => Interlocked.Increment(ref unavailable);
        }

        device.Start();
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await renderer.WaitForPollAsync();
        }

        await Task.Delay(_quietWindow);
        Assert.Equal(3, renderer.Polls);
        Assert.Equal(subscribe ? 1 : 0, Volatile.Read(ref unavailable));
    }

    [Fact]
    public async Task DisposeCancelsPendingRetry()
    {
        using var renderer = new Renderer((_, _) => throw new HttpRequestException("Simulated disconnect"));
        using var device = renderer.CreateDevice();
        device.Start();
        await renderer.WaitForPollAsync();
        // Let the synchronous failure finish scheduling its retry before disposing.
        await Task.Delay(100);
        device.Dispose();

        await Task.Delay(_quietWindow);
        Assert.Equal(1, renderer.Polls);
    }

    [Fact]
    public async Task FailureCompletingAfterDisposeDoesNotRetryOrNotifyUnavailable()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var renderer = new Renderer(async (_, _) =>
        {
            await release.Task;
            throw new HttpRequestException("Simulated disconnect after disposal");
        });
        using var device = renderer.CreateDevice();
        var unavailable = 0;
        device.OnDeviceUnavailable = () => Interlocked.Increment(ref unavailable);
        device.Start();
        await renderer.WaitForPollAsync();
        device.Dispose();
        release.SetResult();

        await Task.Delay(_quietWindow);
        Assert.Equal(1, renderer.Polls);
        Assert.Equal(0, Volatile.Read(ref unavailable));
    }

    [Fact]
    public async Task StoppedRendererRemainsInactive()
    {
        using var renderer = new Renderer((_, _) => Task.FromResult("STOPPED"));
        using var device = renderer.CreateDevice();
        device.Start();
        await renderer.WaitForPollAsync();
        await WaitUntilAsync(() => device.IsStopped);

        await Task.Delay(_quietWindow);
        Assert.Equal(1, renderer.Polls);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(_pollTimeout);
        while (!predicate())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    private sealed class Renderer : HttpMessageHandler, IHttpClientFactory
    {
        private const string AvType = "urn:schemas-upnp-org:service:AVTransport:1";
        private readonly Func<int, CancellationToken, Task<string>> _state;
        private readonly SemaphoreSlim _pollsStarted = new(0);
        private readonly HttpClient _client;
        private int _polls;

        public Renderer(Func<int, CancellationToken, Task<string>> state)
        {
            _state = state;
            _client = new HttpClient(this, disposeHandler: false);
        }

        public int Polls => Volatile.Read(ref _polls);

        public ConcurrentQueue<string> Commands { get; } = new();

        public Device CreateDevice() => new(
            new DeviceInfo
            {
                BaseUrl = "http://renderer.invalid",
                Services = new[]
                {
                    new DeviceService { ServiceType = AvType, ScpdUrl = "/av.xml", ControlUrl = "/av" },
                    new DeviceService { ServiceType = "urn:schemas-upnp-org:service:RenderingControl:1", ScpdUrl = "/render.xml", ControlUrl = "/render" }
                }
            },
            this,
            NullLogger.Instance);

        public HttpClient CreateClient(string name) => _client;

        public async Task WaitForPollAsync() => Assert.True(await _pollsStarted.WaitAsync(_pollTimeout), "Expected a scheduled DLNA poll");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string xml;
            if (request.Method == HttpMethod.Get)
            {
                var actions = request.RequestUri!.AbsolutePath == "/av.xml"
                    ? "<action><name>GetTransportInfo</name></action><action><name>GetPositionInfo</name></action>"
                    : string.Empty;
                xml = $"<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\"><actionList>{actions}</actionList><serviceStateTable /></scpd>";
            }
            else
            {
                var command = request.Headers.GetValues("SOAPACTION").Single().Trim('"').Split('#')[1];
                Commands.Enqueue(command);
                string fields;
                if (command == "GetTransportInfo")
                {
                    var attempt = Interlocked.Increment(ref _polls);
                    _pollsStarted.Release();
                    fields = $"<CurrentTransportState>{await _state(attempt, cancellationToken)}</CurrentTransportState>";
                }
                else if (command == "GetPositionInfo")
                {
                    fields = "<TrackDuration>00:03:00</TrackDuration><RelTime>00:00:10</RelTime>"
                        + "<TrackURI>http://media.invalid/track.mp3</TrackURI><TrackMetaData>"
                        + "&lt;DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\"&gt;"
                        + "&lt;item id=\"1\" parentID=\"0\"&gt;&lt;res&gt;http://media.invalid/track.mp3&lt;/res&gt;&lt;/item&gt;"
                        + "&lt;/DIDL-Lite&gt;</TrackMetaData>";
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected command: {command}");
                }

                xml = $"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><u:{command}Response xmlns:u=\"{AvType}\">{fields}</u:{command}Response></s:Body></s:Envelope>";
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml) };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
                _pollsStarted.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
