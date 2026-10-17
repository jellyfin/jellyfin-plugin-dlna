using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Stops a DLNA live transcode and releases the tuner opens for that session once nobody is watching.
/// </summary>
internal static class DlnaLiveStreamTeardown
{
    private static readonly TimeSpan PlaybackEndGrace = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan ProbeTeardownDelay = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Tracks one live request. A later request on the same key keeps the transcode alive.
    /// The tuner is closed once per open, and only after the last client has been gone for the grace period.
    /// </summary>
    /// <param name="httpContext">The request to follow until it ends.</param>
    /// <param name="job">The transcoding job, if this request is transcoding.</param>
    /// <param name="transcodeManager">The transcode manager, if <paramref name="job"/> is set.</param>
    /// <param name="mediaSourceManager">The media source manager used to close the tuner.</param>
    /// <param name="liveStreamId">The live stream id opened for this request.</param>
    /// <param name="openedTuner">Whether this request opened a tuner consumer.</param>
    /// <param name="probe">Whether this is a HEAD or other short probe, which waits longer before giving up.</param>
    /// <param name="owner">The stream state that should stop closing the tuner once tracking has started.</param>
    public static void Track(
        HttpContext httpContext,
        TranscodingJob? job,
        ITranscodeManager? transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId,
        bool openedTuner,
        bool probe,
        DlnaStreamState? owner = null)
    {
        var key = !string.IsNullOrEmpty(job?.Path) ? job.Path : liveStreamId;
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (owner is not null)
        {
            owner.CloseLiveStreamOnFailure = false;
        }

        DlnaLiveSessionTracker.Attach(key, openedTuner);
        var released = 0;

        void Release()
        {
            if (Interlocked.Exchange(ref released, 1) != 0)
            {
                return;
            }

            if (!DlnaLiveSessionTracker.Detach(key).WentIdle)
            {
                return;
            }

            var delay = probe ? ProbeTeardownDelay : PlaybackEndGrace;
            ScheduleIdleTeardown(key, job, transcodeManager, mediaSourceManager, liveStreamId, delay);
        }

        httpContext.RequestAborted.Register(Release);
        httpContext.Response.OnCompleted(() =>
        {
            Release();
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Closes a tuner this request opened when playback tracking never took ownership.
    /// </summary>
    /// <param name="state">The stream state for the failed request.</param>
    /// <param name="mediaSourceManager">The media source manager used to close the tuner.</param>
    /// <returns>A task that completes when the tuner has been closed, or immediately when there is nothing to close.</returns>
    public static async Task CloseIfUntrackedAsync(DlnaStreamState state, IMediaSourceManager mediaSourceManager)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mediaSourceManager);

        if (!state.CloseLiveStreamOnFailure)
        {
            return;
        }

        state.CloseLiveStreamOnFailure = false;
        var liveStreamId = state.MediaSource?.LiveStreamId;
        if (string.IsNullOrWhiteSpace(liveStreamId))
        {
            liveStreamId = state.Request.LiveStreamId;
        }

        if (!string.IsNullOrWhiteSpace(liveStreamId))
        {
            await mediaSourceManager.CloseLiveStream(liveStreamId).ConfigureAwait(false);
        }
    }

    private static void ScheduleIdleTeardown(
        string key,
        TranscodingJob? job,
        ITranscodeManager? transcodeManager,
        IMediaSourceManager? mediaSourceManager,
        string? liveStreamId,
        TimeSpan delay)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay).ConfigureAwait(false);

            if (!DlnaLiveSessionTracker.TryClaimIdle(key, out var closes))
            {
                return;
            }

            if (DlnaLiveSessionTracker.HasClients(key))
            {
                DlnaLiveSessionTracker.RestoreOpens(key, closes);
                return;
            }

            var streamId = !string.IsNullOrWhiteSpace(liveStreamId) ? liveStreamId : job?.LiveStreamId;
            if (job is not null)
            {
                if (transcodeManager is not null && !string.IsNullOrEmpty(job.Path))
                {
                    using var transcodingLock = await transcodeManager.LockAsync(job.Path, CancellationToken.None).ConfigureAwait(false);
                    await StopDeadTranscodeAsync(job, transcodeManager).ConfigureAwait(false);
                }
                else
                {
                    await StopDeadTranscodeAsync(job, transcodeManager).ConfigureAwait(false);
                }
            }

            if (mediaSourceManager is null || string.IsNullOrWhiteSpace(streamId))
            {
                return;
            }

            for (var i = 0; i < closes; i++)
            {
                await mediaSourceManager.CloseLiveStream(streamId).ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    /// Removes a live transcode from the active list and stops ffmpeg.
    /// The server kill path is not used, so a tuner another viewer still has open stays open.
    /// </summary>
    /// <param name="job">The transcoding job to remove.</param>
    /// <param name="transcodeManager">The transcode manager that owns <paramref name="job"/>, if any.</param>
    /// <returns>A task that completes when the job has been removed, or when the process has been stopped.</returns>
    internal static async Task StopDeadTranscodeAsync(TranscodingJob job, ITranscodeManager? transcodeManager)
    {
        ArgumentNullException.ThrowIfNull(job);

        // The server kill timer closes the live stream again. Clear the id first so that
        // close cannot drop a tuner another viewer, such as a recording, is still using.
        job.StopKillTimer();
        job.LiveStreamId = null;

        if (transcodeManager is not null && !string.IsNullOrWhiteSpace(job.PlaySessionId))
        {
            await transcodeManager.KillTranscodingJobs(
                job.DeviceId ?? string.Empty,
                job.PlaySessionId,
                path => string.Equals(path, job.Path, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
            return;
        }

        var deviceId = job.DeviceId;
        if (transcodeManager is not null && !string.IsNullOrWhiteSpace(deviceId))
        {
            await transcodeManager.KillTranscodingJobs(
                deviceId,
                null,
                path => string.Equals(path, job.Path, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
            return;
        }

        if (!job.HasExited)
        {
            job.Stop();
        }
    }
}
