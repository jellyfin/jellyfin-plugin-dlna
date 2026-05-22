using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Progressive file stream for DLNA live TV. Short probe connections do not stop transcoding;
/// sustained playback does, and all tuner consumers opened by DLNA probes are released when playback stops.
/// </summary>
internal sealed class DlnaLiveProgressiveFileStream : Stream
{
    /// <summary>
    /// Bytes below this are treated as a HEAD/GET probe, not a viewing session.
    /// </summary>
    public const long ProbeBytesThreshold = 65536;

    private readonly Stream _stream;
    private readonly TranscodingJob? _job;
    private readonly ITranscodeManager? _transcodeManager;
    private readonly IMediaSourceManager? _mediaSourceManager;
    private readonly string? _liveStreamId;
    private readonly int _timeoutMs;
    private readonly long _bytesAtOpen;
    private bool _disposed;

    public DlnaLiveProgressiveFileStream(
        string filePath,
        TranscodingJob? job,
        ITranscodeManager transcodeManager,
        IMediaSourceManager mediaSourceManager,
        string? liveStreamId,
        int timeoutMs = 90000)
    {
        _job = job;
        _transcodeManager = transcodeManager;
        _mediaSourceManager = mediaSourceManager;
        _liveStreamId = liveStreamId;
        _bytesAtOpen = job?.BytesDownloaded ?? 0;
        _timeoutMs = timeoutMs;
        _stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            IODefaults.FileStreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int totalBytesRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            totalBytesRead += _stream.Read(buffer);
            if (StopReading(totalBytesRead, stopwatch.ElapsedMilliseconds))
            {
                break;
            }

            Thread.Sleep(50);
        }

        UpdateBytesWritten(totalBytesRead);

        return totalBytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int totalBytesRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            totalBytesRead += await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (StopReading(totalBytesRead, stopwatch.ElapsedMilliseconds))
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        UpdateBytesWritten(totalBytesRead);

        return totalBytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _stream.Dispose();
            ScheduleTeardownIfNeeded();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private void ScheduleTeardownIfNeeded()
    {
        if (_job is null || _transcodeManager is null)
        {
            return;
        }

        var sessionBytes = _job.BytesDownloaded - _bytesAtOpen;
        if (sessionBytes >= ProbeBytesThreshold)
        {
            DlnaLiveStreamTeardown.ScheduleAfterPlayback(
                _job,
                _transcodeManager,
                _mediaSourceManager,
                _liveStreamId);
            return;
        }

        DlnaLiveStreamTeardown.ScheduleAfterProbe(
            _job,
            _transcodeManager,
            _mediaSourceManager,
            _liveStreamId,
            _bytesAtOpen);
    }

    private void UpdateBytesWritten(int totalBytesRead)
    {
        if (_job is not null)
        {
            _job.BytesDownloaded += totalBytesRead;
        }
    }

    private bool StopReading(int bytesRead, long elapsed)
        => bytesRead > 0 || (_job?.HasExited ?? elapsed >= _timeoutMs);
}
