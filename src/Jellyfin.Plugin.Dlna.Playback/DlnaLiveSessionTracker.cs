using System;
using System.Collections.Concurrent;
using MediaBrowser.Controller.MediaEncoding;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Counts DLNA clients on one live stream and how many tuner opens those clients made.
/// </summary>
public static class DlnaLiveSessionTracker
{
    private static readonly ConcurrentDictionary<string, Session> Sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns whether an existing live transcode is still running and can serve another client.
    /// </summary>
    /// <param name="job">The transcoding job already writing the output file, if any.</param>
    /// <returns>True when the job can be reused.</returns>
    public static bool CanReuseLiveTranscode(TranscodingJob? job)
        => job is not null
           && !job.HasExited
           && job.CancellationTokenSource is { IsCancellationRequested: false };

    /// <summary>
    /// Registers a client. <paramref name="openedTuner"/> is set when this request called <c>OpenLiveStream</c>.
    /// </summary>
    /// <param name="key">The transcoding job path, or the live stream id when there is no job.</param>
    /// <param name="openedTuner">Whether this request opened a tuner consumer.</param>
    public static void Attach(string key, bool openedTuner)
    {
        var session = Sessions.GetOrAdd(key, static _ => new Session());
        session.Attach(openedTuner);
    }

    /// <summary>
    /// Drops one client.
    /// </summary>
    /// <param name="key">The key passed to <see cref="Attach"/>.</param>
    /// <returns>How many clients remain, and whether this call was the one that reached zero.</returns>
    public static ClientRelease Detach(string key)
    {
        if (!Sessions.TryGetValue(key, out var session))
        {
            return new ClientRelease(0, false);
        }

        return session.Detach();
    }

    /// <summary>
    /// Returns whether any client is still attached.
    /// </summary>
    /// <param name="key">The key passed to <see cref="Attach"/>.</param>
    /// <returns>True when at least one client is attached.</returns>
    public static bool HasClients(string key)
    {
        if (!Sessions.TryGetValue(key, out var session))
        {
            return false;
        }

        return session.HasClients();
    }

    /// <summary>
    /// When no clients remain, takes the tuner opens so they can be closed once each.
    /// </summary>
    /// <param name="key">The key passed to <see cref="Attach"/>.</param>
    /// <param name="closes">The number of <c>CloseLiveStream</c> calls owed. Zero when another claim already took them.</param>
    /// <returns>False when a client is still attached.</returns>
    public static bool TryClaimIdle(string key, out int closes)
    {
        closes = 0;
        if (!Sessions.TryGetValue(key, out var session))
        {
            return false;
        }

        return session.TryClaimIdle(out closes);
    }

    /// <summary>
    /// Puts tuner opens back when a client attached after they were claimed.
    /// </summary>
    /// <param name="key">The key passed to <see cref="Attach"/>.</param>
    /// <param name="closes">The number of opens to restore.</param>
    public static void RestoreOpens(string key, int closes)
    {
        if (closes <= 0)
        {
            return;
        }

        var session = Sessions.GetOrAdd(key, static _ => new Session());
        session.RestoreOpens(closes);
    }

    private sealed class Session
    {
        private readonly object _gate = new();
        private int _clients;
        private int _opens;

        public void Attach(bool openedTuner)
        {
            lock (_gate)
            {
                _clients++;
                if (openedTuner)
                {
                    _opens++;
                }
            }
        }

        public ClientRelease Detach()
        {
            lock (_gate)
            {
                if (_clients > 0)
                {
                    _clients--;
                }

                return new ClientRelease(_clients, _clients == 0);
            }
        }

        public bool HasClients()
        {
            lock (_gate)
            {
                return _clients > 0;
            }
        }

        public bool TryClaimIdle(out int closes)
        {
            lock (_gate)
            {
                if (_clients > 0)
                {
                    closes = 0;
                    return false;
                }

                closes = _opens;
                _opens = 0;
                return true;
            }
        }

        public void RestoreOpens(int closes)
        {
            lock (_gate)
            {
                _opens += closes;
            }
        }
    }
}
