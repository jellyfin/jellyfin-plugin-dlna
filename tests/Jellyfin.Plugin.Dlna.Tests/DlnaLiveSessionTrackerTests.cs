using System;
using System.Threading;
using Jellyfin.Plugin.Dlna.Playback;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaLiveSessionTrackerTests
{
    [Fact]
    public void Detach_SecondClientBlocksTeardown()
    {
        var key = Guid.NewGuid().ToString("N");
        DlnaLiveSessionTracker.Attach(key, true);
        DlnaLiveSessionTracker.Attach(key, true);

        var first = DlnaLiveSessionTracker.Detach(key);

        Assert.Equal(1, first.RemainingClients);
        Assert.False(first.WentIdle);
        Assert.False(DlnaLiveSessionTracker.TryClaimIdle(key, out _));
    }

    [Fact]
    public void TryClaimIdle_SingleOpenClosesTunerOnce()
    {
        var key = Guid.NewGuid().ToString("N");
        DlnaLiveSessionTracker.Attach(key, true);

        var release = DlnaLiveSessionTracker.Detach(key);

        Assert.True(release.WentIdle);
        Assert.True(DlnaLiveSessionTracker.TryClaimIdle(key, out var closes));
        Assert.Equal(1, closes);
        Assert.True(DlnaLiveSessionTracker.TryClaimIdle(key, out var again));
        Assert.Equal(0, again);
    }

    [Fact]
    public void TryClaimIdle_ReconnectBeforeClaimKeepsTheTuner()
    {
        var key = Guid.NewGuid().ToString("N");
        DlnaLiveSessionTracker.Attach(key, true);
        DlnaLiveSessionTracker.Detach(key);
        DlnaLiveSessionTracker.Attach(key, false);

        Assert.False(DlnaLiveSessionTracker.TryClaimIdle(key, out _));
        Assert.True(DlnaLiveSessionTracker.HasClients(key));
    }

    [Fact]
    public void TryClaimIdle_TwoOpensCloseTwiceWhenIdle()
    {
        var key = Guid.NewGuid().ToString("N");
        DlnaLiveSessionTracker.Attach(key, true);
        DlnaLiveSessionTracker.Attach(key, true);
        DlnaLiveSessionTracker.Detach(key);
        DlnaLiveSessionTracker.Detach(key);

        Assert.True(DlnaLiveSessionTracker.TryClaimIdle(key, out var closes));
        Assert.Equal(2, closes);
    }

    [Fact]
    public void CanReuseLiveTranscode_RejectsExitedOrCancelledJob()
    {
        using var running = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
        {
            CancellationTokenSource = new CancellationTokenSource()
        };
        Assert.True(DlnaLiveSessionTracker.CanReuseLiveTranscode(running));

        running.HasExited = true;
        Assert.False(DlnaLiveSessionTracker.CanReuseLiveTranscode(running));

        using var cancelled = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
        {
            CancellationTokenSource = new CancellationTokenSource()
        };
        cancelled.CancellationTokenSource.Cancel();
        Assert.False(DlnaLiveSessionTracker.CanReuseLiveTranscode(cancelled));
        Assert.False(DlnaLiveSessionTracker.CanReuseLiveTranscode(null));
    }
}
