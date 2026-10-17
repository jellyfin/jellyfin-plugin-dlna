using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// Progressive file stream for DLNA live TV. Session end is tracked separately so a short
/// connection does not stop a client that is still watching.
/// </summary>
internal sealed class DlnaLiveProgressiveFileStream : Stream
{
    private readonly Stream _stream;
    private readonly TranscodingJob? _job;
    private readonly int _timeoutMs;
    private bool _disposed;

    public DlnaLiveProgressiveFileStream(
        string filePath,
        TranscodingJob? job,
        int timeoutMs = 90000)
    {
        _job = job;
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
        }

        _disposed = true;
        base.Dispose(disposing);
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
