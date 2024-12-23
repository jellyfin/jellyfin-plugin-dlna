#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna;

public class EventSubscriptionResponse
{
    public EventSubscriptionResponse(string content, string contentType)
    {
        Content = content;
        ContentType = contentType;
        Headers = new Dictionary<string, string>();
    }

    public string Content { get; set; }

    public string ContentType { get; set; }

    public Dictionary<string, string> Headers { get; }

    public override string ToString() 
    {
        if (ContentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
        {
            return Content.Trim() + "\r\n" + string.Join(Environment.NewLine, Headers);
        }

        return Content;
    }
}
