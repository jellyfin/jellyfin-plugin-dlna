using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback.Extensions;
using Jellyfin.Plugin.Dlna.Playback.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// The stream response helpers.
/// </summary>
public static class FileStreamResponseHelpers
{
    /// <summary>
    /// Returns a static file from a remote source.
    /// </summary>
    /// <param name="state">The current <see cref="StreamState"/>.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> making the remote request.</param>
    /// <param name="httpContext">The current http context.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
    public static async Task<ActionResult> GetStaticRemoteStreamResult(
        StreamState state,
        HttpClient httpClient,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(state.MediaPath));

        // Forward User-Agent if provided
        if (state.RemoteHttpHeaders.TryGetValue(HeaderNames.UserAgent, out var useragent))
        {
            // Clear default and add specific one if exists, otherwise HttpClient default might be used
            requestMessage.Headers.UserAgent.Clear();
            requestMessage.Headers.TryAddWithoutValidation(HeaderNames.UserAgent, useragent);
        }

        // Forward Range header if present in the client request
        if (httpContext.Request.Headers.TryGetValue(HeaderNames.Range, out var rangeValue))
        {
            var rangeString = rangeValue.ToString();
            if (!string.IsNullOrEmpty(rangeString))
            {
                requestMessage.Headers.Range = System.Net.Http.Headers.RangeHeaderValue.Parse(rangeString);
            }
        }

        // Send the request to the upstream server
        // Use ResponseHeadersRead to avoid downloading the whole content immediately
        var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        // Check if the upstream server supports range requests and acted upon our Range header
        bool upstreamSupportsRange = response.StatusCode == HttpStatusCode.PartialContent;
        string acceptRangesValue = "none";
        if (response.Headers.TryGetValues(HeaderNames.AcceptRanges, out var acceptRangesHeaders))
        {
            // Prefer upstream server's Accept-Ranges header if available
            acceptRangesValue = string.Join(", ", acceptRangesHeaders);
            upstreamSupportsRange |= acceptRangesValue.Contains("bytes", StringComparison.OrdinalIgnoreCase);
        }
        else if (upstreamSupportsRange)
        {
            // If we got 206 but no Accept-Ranges header, assume bytes
            acceptRangesValue = "bytes";
        }

        // Set Accept-Ranges header for the client based on upstream support
        httpContext.Response.Headers[HeaderNames.AcceptRanges] = acceptRangesValue;

        // Set Content-Range header if upstream provided it (implies partial content)
        if (response.Content.Headers.ContentRange is not null)
        {
            httpContext.Response.Headers[HeaderNames.ContentRange] = response.Content.Headers.ContentRange.ToString();
        }

        // Set Content-Length header. For partial content, this is the length of the partial segment.
        if (response.Content.Headers.ContentLength.HasValue)
        {
            httpContext.Response.ContentLength = response.Content.Headers.ContentLength.Value;
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? MediaTypeNames.Application.Octet;

        // Set the status code for the client response (e.g., 200 OK or 206 Partial Content)
        httpContext.Response.StatusCode = (int)response.StatusCode;

        // Can't dispose the response as the stream is required up the call chain; FileStreamResult owns it.
        return new FileStreamResult(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), contentType);
    }

    /// <summary>
    /// Returns a static file from the server.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <returns>An <see cref="ActionResult"/> the file.</returns>
    public static ActionResult GetStaticFileResult(
        string path,
        string contentType)
    {
        return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
    }

    /// <summary>
    /// Returns a transcoded file from the server.
    /// </summary>
    /// <param name="state">The current <see cref="StreamState"/>.</param>
    /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
    /// <param name="httpContext">The current http context.</param>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager"/> singleton.</param>
    /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> singleton.</param>
    /// <param name="ffmpegCommandLineArguments">The command line arguments to start ffmpeg.</param>
    /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
    /// <returns>A <see cref="Task{ActionResult}"/> containing the transcoded file.</returns>
    public static async Task<ActionResult> GetTranscodedFile(
        StreamState state,
        bool isHeadRequest,
        HttpContext httpContext,
        ITranscodeManager transcodeManager,
        IMediaSourceManager mediaSourceManager,
        string ffmpegCommandLineArguments,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource)
    {
        // Use the command line args with a dummy playlist path
        var outputPath = state.OutputFilePath;

        httpContext.Response.Headers[HeaderNames.AcceptRanges] = "none";

        // Keep the served Content-Type in line with the MIME type advertised in the DIDL-Lite protocolInfo.
        var contentType = (state.IsVideoRequest
                              ? DlnaMimeTypes.GetVideoMimeType(state.OutputContainer, state.TargetTimestamp)
                              : DlnaMimeTypes.GetAudioMimeType(state.OutputContainer, state.OutputAudioBitrate, state.OutputAudioSampleRate, state.OutputAudioChannels))
                          ?? state.GetMimeType(outputPath);
        var isLiveTv = state.MediaSource.IsInfiniteStream;

        // Library HEAD only needs the headers. Starting ffmpeg here leaves it running after the client stops.
        if (isHeadRequest && !isLiveTv)
        {
            httpContext.Response.Headers[HeaderNames.ContentType] = contentType;
            return new OkResult();
        }

        using var transcodingLock = await transcodeManager.LockAsync(outputPath, cancellationTokenSource.Token).ConfigureAwait(false);

        TranscodingJob? job = null;
        var existingLiveJob = isLiveTv && File.Exists(outputPath)
            ? transcodeManager.GetTranscodingJob(outputPath, TranscodingJobType.Progressive)
            : null;
        var startNewLiveJob = isLiveTv && (existingLiveJob is null || !DlnaLiveSessionTracker.CanReuseLiveTranscode(existingLiveJob));

        if (existingLiveJob is not null && !DlnaLiveSessionTracker.CanReuseLiveTranscode(existingLiveJob))
        {
            await DlnaLiveStreamTeardown.StopDeadTranscodeAsync(existingLiveJob, transcodeManager).ConfigureAwait(false);
        }

        if (!File.Exists(outputPath) || startNewLiveJob)
        {
            if (isLiveTv && string.IsNullOrWhiteSpace(state.Request.PlaySessionId))
            {
                state.Request.PlaySessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            if (startNewLiveJob && File.Exists(outputPath))
            {
                try
                {
                    File.Delete(outputPath);
                }
                catch (IOException ex)
                {
                    LogDeleteFailure(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogDeleteFailure(ex);
                }
            }

            job = await transcodeManager.StartFfMpeg(
                state,
                outputPath,
                ffmpegCommandLineArguments,
                httpContext.User.GetUserId(),
                transcodingJobType,
                cancellationTokenSource).ConfigureAwait(false);

            if (isLiveTv)
            {
                job.IsLiveOutput = true;
                job.StopKillTimer();
            }
        }
        else
        {
            // A live HEAD already counted this job. A second begin request would keep ffmpeg up after disconnect.
            job = isLiveTv
                ? existingLiveJob
                : transcodeManager.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);

            job?.StopKillTimer();
        }

        var dlnaState = state as DlnaStreamState;
        var openedTuner = dlnaState is not null && dlnaState.OpenedLiveStream;
        if (isLiveTv)
        {
            DlnaLiveStreamTeardown.Track(
                httpContext,
                job,
                transcodeManager,
                mediaSourceManager,
                state.MediaSource.LiveStreamId,
                openedTuner,
                isHeadRequest,
                dlnaState);
        }

        // Live TV HEAD starts the transcode so the following GET has data ready.
        if (isHeadRequest)
        {
            httpContext.Response.Headers[HeaderNames.ContentType] = contentType;
            return new OkResult();
        }

        if (isLiveTv && job is not null)
        {
            return TakeOwnership(new DlnaLiveProgressiveFileStream(outputPath, job), contentType);
        }

        return TakeOwnership(new ProgressiveFileStream(outputPath, job, transcodeManager), contentType);

        void LogDeleteFailure(Exception ex)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(FileStreamResponseHelpers));
            logger.LogDebug(ex, "Could not delete live transcode file {Path}", outputPath);
        }
    }

    private static FileStreamResult TakeOwnership(Stream stream, string contentType)
    {
        try
        {
            return new FileStreamResult(stream, contentType);
        }
        catch (ArgumentException)
        {
            stream.Dispose();
            throw;
        }
        catch (FormatException)
        {
            stream.Dispose();
            throw;
        }
    }
}
