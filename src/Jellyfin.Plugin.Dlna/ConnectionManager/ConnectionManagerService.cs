#pragma warning disable CS1591

using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Service;
using Microsoft.Extensions.Logging;
using IDlnaManager = Jellyfin.Plugin.Dlna.Model.IDlnaManager;

namespace Jellyfin.Plugin.Dlna.ConnectionManager;

/// <summary>
/// Defines the <see cref="ConnectionManagerService" />.
/// </summary>
public class ConnectionManagerService : BaseService, IConnectionManager
{
    private readonly IDlnaManager _dlna;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManagerService"/> class.
    /// </summary>
    /// <param name="dlna">The <see cref="IDlnaManager"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
    /// <param name="logger">The <see cref="ILogger{ConnectionManagerService}"/> for use with the <see cref="ConnectionManagerService"/> instance..</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for use with the <see cref="ConnectionManagerService"/> instance..</param>
    public ConnectionManagerService(
        IDlnaManager dlna,
        ILogger<ConnectionManagerService> logger,
        IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
        _dlna = dlna;
    }

    /// <inheritdoc />
    public string GetServiceXml()
    {
        return ConnectionManagerXmlBuilder.GetXml();
    }

    /// <inheritdoc />
    public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
    {
        var profile = _dlna.GetProfile(request.Headers) ?? _dlna.GetDefaultProfile();

        return new ControlHandler(Logger, profile).ProcessControlRequestAsync(request);
    }
}
