using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Service;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.MediaReceiverRegistrar;

/// <summary>
/// Defines the <see cref="MediaReceiverRegistrarService" />.
/// </summary>
public class MediaReceiverRegistrarService : BaseService, IMediaReceiverRegistrar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaReceiverRegistrarService"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{MediaReceiverRegistrarService}"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
    public MediaReceiverRegistrarService(
        ILogger<MediaReceiverRegistrarService> logger,
        IHttpClientFactory httpClientFactory)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public string GetServiceXml()
    {
        return MediaReceiverRegistrarXmlBuilder.GetXml();
    }

    /// <inheritdoc />
    public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
    {
        return new ControlHandler(Logger)
            .ProcessControlRequestAsync(request);
    }
}
