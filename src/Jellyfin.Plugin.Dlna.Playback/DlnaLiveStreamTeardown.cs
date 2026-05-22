using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Stops DLNA live-TV transcoding and releases all tuner consumers opened by repeated HEAD/GET probes.
/// </summary>
internal static class DlnaLiveStreamTeardown
{
    /// <summary>
    /// DLNA often opens the same live stream several times (HEAD, GET, metadata). Each close drops one consumer.
    /// </summary>
    private const int MaxCloseAttempts = 16;

    private const int MaxEndRequestAttempts = 12;

    private static readonly TimeSpan PlaybackEndGrace = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan ProbeTeardownDelay = TimeSpan.FromSeconds(45);

    private static readonly ConcurrentDictionary<string, byte> _pendingTeardowns = new(StringComparer.OrdinalIgnoreCase);

    public static void RegisterClientDisconnect(
        HttpContext httpContext,
        TranscodingJob? job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId,
        long bytesAtOpen)
    {
        if (job is null || string.IsNullOrEmpty(job.Path))
        {
            return;
        }

        httpContext.RequestAborted.Register(() =>
        {
            var sessionBytes = job.BytesDownloaded - bytesAtOpen;
            if (sessionBytes >= DlnaLiveProgressiveFileStream.ProbeBytesThreshold)
            {
                ScheduleAfterPlayback(job, transcodeManager, mediaSourceManager, liveStreamId);
            }
            else
            {
                ScheduleAfterProbe(job, transcodeManager, mediaSourceManager, liveStreamId, bytesAtOpen);
            }
        });
    }

    public static void ScheduleAfterPlayback(
        TranscodingJob? job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId)
    {
        ScheduleTeardown(job, transcodeManager, mediaSourceManager, liveStreamId, PlaybackEndGrace);
    }

    public static void ScheduleAfterProbe(
        TranscodingJob? job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId,
        long bytesAtOpen)
    {
        if (job is null || string.IsNullOrEmpty(job.Path))
        {
            return;
        }

        if (!_pendingTeardowns.TryAdd(job.Path + ":probe", 0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ProbeTeardownDelay).ConfigureAwait(false);

                if (job.HasExited)
                {
                    return;
                }

                if (job.BytesDownloaded - bytesAtOpen >= DlnaLiveProgressiveFileStream.ProbeBytesThreshold)
                {
                    return;
                }

                await ForceTeardownAsync(job, transcodeManager, mediaSourceManager, liveStreamId).ConfigureAwait(false);
            }
            finally
            {
                _pendingTeardowns.TryRemove(job.Path + ":probe", out _);
            }
        });
    }

    private static void ScheduleTeardown(
        TranscodingJob? job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId,
        TimeSpan delay)
    {
        if (job is null || string.IsNullOrEmpty(job.Path))
        {
            return;
        }

        if (!_pendingTeardowns.TryAdd(job.Path, 0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
                await ForceTeardownAsync(job, transcodeManager, mediaSourceManager, liveStreamId).ConfigureAwait(false);
            }
            finally
            {
                _pendingTeardowns.TryRemove(job.Path, out _);
            }
        });
    }

    private static async Task ForceTeardownAsync(
        TranscodingJob job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId)
    {
        if (job.HasExited)
        {
            await ReleaseLiveStreamAsync(mediaSourceManager, !string.IsNullOrWhiteSpace(liveStreamId) ? liveStreamId : job.LiveStreamId).ConfigureAwait(false);
            return;
        }

        var streamId = !string.IsNullOrWhiteSpace(liveStreamId) ? liveStreamId : job.LiveStreamId;

        // Stop ffmpeg immediately (progressive kill timer is unreliable for DLNA).
        if (job.CancellationTokenSource is { IsCancellationRequested: false })
        {
            await job.CancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        await ReleaseLiveStreamAsync(mediaSourceManager, streamId).ConfigureAwait(false);

        for (var i = 0; i < MaxEndRequestAttempts && job.ActiveRequestCount > 0; i++)
        {
            transcodeManager.OnTranscodeEndRequest(job);
        }

        if (job.ActiveRequestCount > 0)
        {
            job.ActiveRequestCount = 0;
            transcodeManager.OnTranscodeEndRequest(job);
        }
    }

    private static async Task ReleaseLiveStreamAsync(IMediaSourceManager? mediaSourceManager, string? liveStreamId)
    {
        if (mediaSourceManager is null || string.IsNullOrWhiteSpace(liveStreamId))
        {
            return;
        }

        for (var i = 0; i < MaxCloseAttempts; i++)
        {
            try
            {
                await mediaSourceManager.CloseLiveStream(liveStreamId).ConfigureAwait(false);
            }
            catch
            {
                break;
            }
        }
    }
}
