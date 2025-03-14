using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="EventSubscriptionResponse" />.
/// </summary>
public class EventSubscriptionResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSubscriptionResponse"/> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="contentType">The content type.</param>
    public EventSubscriptionResponse(string content, string contentType)
    {
        Content = content;
        ContentType = contentType;
        Headers = [];
    }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets the headers dictionary.
    /// </summary>
    public Dictionary<string, string> Headers { get; }
}
