using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Plugin.Dlna.Common;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Http client for Dlna PlayTo function.
/// </summary>
public partial class DlnaHttpClient
{
    private const int MaxLoggedContentLength = 1024;

    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaHttpClient"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    public DlnaHttpClient(ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [GeneratedRegex("(&(?![a-z]*;))")]
    private static partial Regex EscapeAmpersandRegex();

    /// <summary>
    /// Logs what a device answered to a request it refused.
    /// </summary>
    /// <remarks>
    /// The status code alone does not say why a device rejected a command: the reason is in the SOAP fault
    /// it returns, which would otherwise be dropped with the response.
    /// </remarks>
    /// <param name="request">The <see cref="HttpRequestMessage"/> that was refused.</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/> of the device.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task LogErrorResponseAsync(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content;
        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                "Device answered {StatusCode} to {Method} {Uri}, the response could not be read: {Message}",
                (int)response.StatusCode,
                request.Method,
                request.RequestUri,
                ex.Message);

            return;
        }

        var (errorCode, errorDescription) = GetUPnPError(content);
        if (errorCode is not null)
        {
            _logger.LogWarning(
                "Device answered {StatusCode} to {Method} {Uri} with UPnP error {ErrorCode}: {ErrorDescription}",
                (int)response.StatusCode,
                request.Method,
                request.RequestUri,
                errorCode,
                errorDescription ?? "no description");

            return;
        }

        _logger.LogWarning(
            "Device answered {StatusCode} to {Method} {Uri}: {Content}",
            (int)response.StatusCode,
            request.Method,
            request.RequestUri,
            content.Length > MaxLoggedContentLength ? content[..MaxLoggedContentLength] + "..." : content);
    }

    /// <summary>
    /// Gets the UPnP error a device reported in a SOAP fault.
    /// </summary>
    /// <param name="content">The response content.</param>
    /// <returns>The error code and description, both <c>null</c> if the content carries no UPnP error.</returns>
    private static (string? ErrorCode, string? ErrorDescription) GetUPnPError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (null, null);
        }

        try
        {
            var document = XDocument.Parse(content);

            // The fault is wrapped in SOAP envelope and detail elements, so go by name only.
            var errorCode = document.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "errorCode", StringComparison.Ordinal))?.Value;
            var errorDescription = document.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "errorDescription", StringComparison.Ordinal))?.Value;

            return (errorCode, errorDescription);
        }
        catch (XmlException)
        {
            return (null, null);
        }
    }

    private async Task<XDocument?> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(NamedClient.Dlna);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            await LogErrorResponseAsync(request, response, cancellationToken).ConfigureAwait(false);
        }

        response.EnsureSuccessStatusCode();
        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            try
            {
                return await XDocument.LoadAsync(
                    stream,
                    LoadOptions.None,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (XmlException)
            {
                // try correcting the Xml response with common errors
                stream.Position = 0;
                using StreamReader sr = new(stream);
                var xmlString = await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                // find and replace unescaped ampersands (&)
                xmlString = EscapeAmpersandRegex().Replace(xmlString, "&amp;");

                try
                {
                    // retry reading Xml
                    using var xmlReader = new StringReader(xmlString);
                    return await XDocument.LoadAsync(
                        xmlReader,
                        LoadOptions.None,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (XmlException ex)
                {
                    _logger.LogError(ex, "Failed to parse response");
                    _logger.LogDebug("Malformed response: {Content}\n", xmlString);

                    return null;
                }
            }
        }
    }

    /// <summary>
    /// Gets data of a URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous data fetching operation.</returns>
    public async Task<XDocument?> GetDataAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Have to await here instead of returning the Task directly, otherwise request would be disposed too soon
        return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends command async.
    /// </summary>
    /// <param name="controlUrl">The absolute control URL of the service.</param>
    /// <param name="service">The <see cref="DeviceService"/>.</param>
    /// <param name="command">The command.</param>
    /// <param name="postData">The POST data.</param>
    /// <param name="header">The header.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous send operation.</returns>
    public async Task<XDocument?> SendCommandAsync(
        string controlUrl,
        DeviceService service,
        string command,
        string postData,
        string? header = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, controlUrl)
        {
            Content = new StringContent(postData, Encoding.UTF8, MediaTypeNames.Text.Xml)
        };

        request.Headers.TryAddWithoutValidation(
            "SOAPACTION",
            string.Format(
                CultureInfo.InvariantCulture,
                "\"{0}#{1}\"",
                service.ServiceType,
                command));
        request.Headers.Pragma.ParseAdd("no-cache");

        if (!string.IsNullOrEmpty(header))
        {
            request.Headers.TryAddWithoutValidation("contentFeatures.dlna.org", header);
        }

        // Have to await here instead of returning the Task directly, otherwise request would be disposed too soon
        return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
