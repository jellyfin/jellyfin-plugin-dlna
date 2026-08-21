using System.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="ControlRequest" />.
/// </summary>
public class ControlRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlRequest"/> class.
    /// </summary>
    /// <param name="headers">Instance of the <see cref="IHeaderDictionary"/> interface.</param>
    public ControlRequest(IHeaderDictionary headers)
    {
        Headers = headers;
    }

    /// <summary>
    /// Gets the <see cref="IHeaderDictionary"/> instance.
    /// </summary>
    public IHeaderDictionary Headers { get; }

    /// <summary>
    /// Gets or sets the <see cref="Stream"/>.
    /// </summary>
    public required Stream InputXml { get; set; }

    /// <summary>
    /// Gets or sets the target server UUID.
    /// </summary>
    public required string TargetServerUuId { get; set; }

    /// <summary>
    /// Gets or sets the request URL.
    /// </summary>
    public required string RequestedUrl { get; set; }
}
