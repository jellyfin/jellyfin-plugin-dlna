#nullable disable

#pragma warning disable CS1591

using System.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna;

public class ControlRequest
{
    public ControlRequest(IHeaderDictionary headers)
    {
        Headers = headers;
    }

    public IHeaderDictionary Headers { get; }

    public Stream InputXml { get; set; }

    public string TargetServerUuId { get; set; }

    public string RequestedUrl { get; set; }
}
