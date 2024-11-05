using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Extensions;
using Jellyfin.Plugin.Dlna.Didl;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Service;

/// <summary>
/// Defines the <see cref="BaseControlHandler" />.
/// </summary>
public abstract class BaseControlHandler
{
    private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseControlHandler"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    protected BaseControlHandler(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/> instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Processes a control request asynchronously.
    /// </summary>
    /// <param name="request">The <see cref="ControlRequest"/>.</param>
    /// <returns>Task{ControlResponse}.</returns>
    public async Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
    {
        try
        {
            Logger.LogDebug("Control request. Headers: {@Headers}", request.Headers);

            var response = await ProcessControlRequestInternalAsync(request).ConfigureAwait(false);
            Logger.LogDebug("Control response. Headers: {@Headers}\n{Xml}", response.Headers, response.Xml);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing control request");

            return ControlErrorHandler.GetResponse(ex);
        }
    }

    private async Task<ControlResponse> ProcessControlRequestInternalAsync(ControlRequest request)
    {
        ControlRequestInfo requestInfo;

        using (var streamReader = new StreamReader(request.InputXml, Encoding.UTF8))
        {
            var readerSettings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                Async = true
            };

            using var reader = XmlReader.Create(streamReader, readerSettings);
            requestInfo = await ParseRequestAsync(reader).ConfigureAwait(false);
        }

        Logger.LogDebug("Received control request {LocalName}, params: {@Headers}", requestInfo.LocalName, requestInfo.Headers);

        return CreateControlResponse(requestInfo);
    }

    private ControlResponse CreateControlResponse(ControlRequestInfo requestInfo)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false
        };

        StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8);

        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartDocument(true);

            writer.WriteStartElement("SOAP-ENV", "Envelope", NsSoapEnv);
            writer.WriteAttributeString(string.Empty, "encodingStyle", NsSoapEnv, "http://schemas.xmlsoap.org/soap/encoding/");

            writer.WriteStartElement("SOAP-ENV", "Body", NsSoapEnv);
            writer.WriteStartElement("u", requestInfo.LocalName + "Response", requestInfo.NamespaceURI);

            WriteResult(requestInfo.LocalName, requestInfo.Headers, writer);

            writer.WriteFullEndElement();
            writer.WriteFullEndElement();

            writer.WriteFullEndElement();
            writer.WriteEndDocument();
        }

        var xml = builder.ToString().Replace("xmlns:m=", "xmlns:u=", StringComparison.Ordinal);

        var controlResponse = new ControlResponse(xml, true);

        controlResponse.Headers.Add("EXT", string.Empty);

        return controlResponse;
    }

    private async Task<ControlRequestInfo> ParseRequestAsync(XmlReader reader)
    {
        await reader.MoveToContentAsync().ConfigureAwait(false);
        await reader.ReadAsync().ConfigureAwait(false);

        // Loop through each element
        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (string.Equals(reader.LocalName, "Body", StringComparison.Ordinal))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.ReadAsync().ConfigureAwait(false);
                        continue;
                    }

                    using var subReader = reader.ReadSubtree();
                    return await ParseBodyTagAsync(subReader).ConfigureAwait(false);
                }

                await reader.SkipAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        throw new EndOfStreamException("Stream ended but no body tag found.");
    }

    private async Task<ControlRequestInfo> ParseBodyTagAsync(XmlReader reader)
    {
        string? namespaceURI = null, localName = null;

        await reader.MoveToContentAsync().ConfigureAwait(false);
        await reader.ReadAsync().ConfigureAwait(false);

        // Loop through each element
        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                localName = reader.LocalName;
                namespaceURI = reader.NamespaceURI;

                if (reader.IsEmptyElement)
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
                else
                {
                    var result = new ControlRequestInfo(localName, namespaceURI);
                    using var subReader = reader.ReadSubtree();
                    await ParseFirstBodyChildAsync(subReader, result.Headers).ConfigureAwait(false);
                    return result;
                }
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        if (localName is not null && namespaceURI is not null)
        {
            return new ControlRequestInfo(localName, namespaceURI);
        }

        throw new EndOfStreamException("Stream ended but no control found.");
    }

    private static async Task ParseFirstBodyChildAsync(XmlReader reader, Dictionary<string, string> headers)
    {
        await reader.MoveToContentAsync().ConfigureAwait(false);
        await reader.ReadAsync().ConfigureAwait(false);

        // Loop through each element
        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                // TODO: Should we be doing this here, or should it be handled earlier when decoding the request?
                headers[reader.LocalName.RemoveDiacritics()] = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Writes the result.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="methodParams">The method parameters.</param>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    protected abstract void WriteResult(string methodName, IReadOnlyDictionary<string, string> methodParams, XmlWriter xmlWriter);

    private sealed class ControlRequestInfo
    {
        public ControlRequestInfo(string localName, string namespaceUri)
        {
            LocalName = localName;
            NamespaceURI = namespaceUri;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string LocalName { get; set; }

        public string NamespaceURI { get; set; }

        public Dictionary<string, string> Headers { get; }
    }
}
