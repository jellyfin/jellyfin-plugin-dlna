using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Extensions;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Dlna.Api;

/// <summary>
/// Dlna Server Controller.
/// </summary>
[ApiController]
[Route("Dlna")]
[Authorize(Policy = Policies.AnonymousLanAccessPolicy)]
public class DlnaServerController : ControllerBase
{
    private static readonly string[] _relativePathUserAgents = ["Bigscreen"];

    private readonly IDlnaManager _dlnaManager;
    private readonly IContentDirectory _contentDirectory;
    private readonly IConnectionManager _connectionManager;
    private readonly IMediaReceiverRegistrar _mediaReceiverRegistrar;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaServerController"/> class.
    /// </summary>
    /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
    /// <param name="contentDirectory">Instance of the <see cref="IContentDirectory"/> interface.</param>
    /// <param name="connectionManager">Instance of the <see cref="IConnectionManager"/> interface.</param>
    /// <param name="mediaReceiverRegistrar">Instance of the <see cref="IMediaReceiverRegistrar"/> interface.</param>
    public DlnaServerController(
        IDlnaManager dlnaManager,
        IContentDirectory contentDirectory,
        IConnectionManager connectionManager,
        IMediaReceiverRegistrar mediaReceiverRegistrar)
    {
        _dlnaManager = dlnaManager;
        _contentDirectory = contentDirectory;
        _connectionManager = connectionManager;
        _mediaReceiverRegistrar = mediaReceiverRegistrar;
    }

    /// <summary>
    /// Get Description Xml.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Description xml returned.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>An <see cref="OkResult"/> containing the description xml.</returns>
    [HttpGet("{serverId}/description")]
    [HttpGet("{serverId}/description.xml", Name = "GetDescriptionXml_2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public ActionResult<string> GetDescriptionXml([FromRoute, Required] string serverId)
    {
        var useRelativePath = false;
        string? userAgent = Request.Headers.UserAgent;
        if (userAgent is not null)
        {
            var firstIndexOfSlash = userAgent.IndexOf('/', StringComparison.OrdinalIgnoreCase);
            if (firstIndexOfSlash > 0)
            {
                userAgent = userAgent[..firstIndexOfSlash];
            }

            useRelativePath = _relativePathUserAgents.Contains(userAgent, StringComparison.Ordinal);
        }

        var url = useRelativePath ? GetRelativePath() : GetAbsoluteUri();
        var serverAddress = url[..url.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase)];
        var xml = _dlnaManager.GetServerDescriptionXml(Request.Headers, serverId, serverAddress);
        return Ok(xml);
    }

    /// <summary>
    /// Gets Dlna content directory xml.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Dlna content directory returned.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>An <see cref="OkResult"/> containing the dlna content directory xml.</returns>
    [HttpGet("{serverId}/ContentDirectory")]
    [HttpGet("{serverId}/ContentDirectory/ContentDirectory", Name = "GetContentDirectory_2")]
    [HttpGet("{serverId}/ContentDirectory/ContentDirectory.xml", Name = "GetContentDirectory_3")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public ActionResult<string> GetContentDirectory([FromRoute, Required] string serverId)
    {
        return Ok(_contentDirectory.GetServiceXml());
    }

    /// <summary>
    /// Gets Dlna media receiver registrar xml.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Dlna media receiver registrar xml returned.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Dlna media receiver registrar xml.</returns>
    [HttpGet("{serverId}/MediaReceiverRegistrar")]
    [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar", Name = "GetMediaReceiverRegistrar_2")]
    [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar.xml", Name = "GetMediaReceiverRegistrar_3")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public ActionResult<string> GetMediaReceiverRegistrar([FromRoute, Required] string serverId)
    {
        return Ok(_mediaReceiverRegistrar.GetServiceXml());
    }

    /// <summary>
    /// Gets Dlna media receiver registrar xml.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Dlna media receiver registrar xml returned.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Dlna media receiver registrar xml.</returns>
    [HttpGet("{serverId}/ConnectionManager")]
    [HttpGet("{serverId}/ConnectionManager/ConnectionManager", Name = "GetConnectionManager_2")]
    [HttpGet("{serverId}/ConnectionManager/ConnectionManager.xml", Name = "GetConnectionManager_3")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public ActionResult<string> GetConnectionManager([FromRoute, Required] string serverId)
    {
        return Ok(_connectionManager.GetServiceXml());
    }

    /// <summary>
    /// Process a content directory control request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Control response.</returns>
    [HttpPost("{serverId}/ContentDirectory/Control")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public async Task<ActionResult<ControlResponse>> ProcessContentDirectoryControlRequest([FromRoute, Required] string serverId)
    {
        return await ProcessControlRequestInternalAsync(serverId, Request.Body, _contentDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Process a connection manager control request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Control response.</returns>
    [HttpPost("{serverId}/ConnectionManager/Control")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public async Task<ActionResult<ControlResponse>> ProcessConnectionManagerControlRequest([FromRoute, Required] string serverId)
    {
        return await ProcessControlRequestInternalAsync(serverId, Request.Body, _connectionManager).ConfigureAwait(false);
    }

    /// <summary>
    /// Process a media receiver registrar control request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Control response.</returns>
    [HttpPost("{serverId}/MediaReceiverRegistrar/Control")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [Produces(MediaTypeNames.Text.Xml)]
    public async Task<ActionResult<ControlResponse>> ProcessMediaReceiverRegistrarControlRequest([FromRoute, Required] string serverId)
    {
        return await ProcessControlRequestInternalAsync(serverId, Request.Body, _mediaReceiverRegistrar).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes an event subscription request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Event subscription response.</returns>
    [HttpSubscribe("{serverId}/MediaReceiverRegistrar/Events")]
    [HttpUnsubscribe("{serverId}/MediaReceiverRegistrar/Events")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult ProcessMediaReceiverRegistrarEventRequest(string serverId)
    {
        SetResponse(ProcessEventRequest(_mediaReceiverRegistrar));
        return new EmptyResult();
    }

    /// <summary>
    /// Processes an event subscription request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Event subscription response.</returns>
    [HttpSubscribe("{serverId}/ContentDirectory/Events")]
    [HttpUnsubscribe("{serverId}/ContentDirectory/Events")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult ProcessContentDirectoryEventRequest(string serverId)
    {
        SetResponse(ProcessEventRequest(_contentDirectory));
        return new EmptyResult();
    }

    /// <summary>
    /// Processes an event subscription request.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Event subscription response.</returns>
    [HttpSubscribe("{serverId}/ConnectionManager/Events")]
    [HttpUnsubscribe("{serverId}/ConnectionManager/Events")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult ProcessConnectionManagerEventRequest(string serverId)
    {
        SetResponse(ProcessEventRequest(_connectionManager));
        return new EmptyResult();
    }

    /// <summary>
    /// Gets a server icon.
    /// </summary>
    /// <param name="serverId">Server UUID.</param>
    /// <param name="fileName">The icon filename.</param>
    /// <response code="200">Request processed.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="503">DLNA is disabled.</response>
    /// <returns>Icon stream.</returns>
    [HttpGet("{serverId}/icons/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult GetIconId([FromRoute, Required] string serverId, [FromRoute, Required] string fileName)
    {
        return GetIconInternal(fileName);
    }

    /// <summary>
    /// Gets a server icon.
    /// </summary>
    /// <param name="fileName">The icon filename.</param>
    /// <returns>Icon stream.</returns>
    /// <response code="200">Request processed.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="503">DLNA is disabled.</response>
    [HttpGet("icons/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult GetIcon([FromRoute, Required] string fileName)
    {
        return GetIconInternal(fileName);
    }

    private ActionResult GetIconInternal(string fileName)
    {
        var iconStream = _dlnaManager.GetIcon(fileName);
        if (iconStream is null)
        {
            return NotFound();
        }

        return File(iconStream, MimeTypes.GetMimeType(fileName));
    }

    private string GetAbsoluteUri()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
    }

    private string GetRelativePath()
    {
        return $"{Request.PathBase}{Request.Path}";
    }

    private Task<ControlResponse> ProcessControlRequestInternalAsync(string id, Stream requestStream, IUpnpService service)
    {
        return service.ProcessControlRequestAsync(new ControlRequest(Request.Headers)
        {
            InputXml = requestStream,
            TargetServerUuId = id,
            RequestedUrl = GetAbsoluteUri()
        });
    }

    private EventSubscriptionResponse ProcessEventRequest(IDlnaEventManager dlnaEventManager)
    {
        var subscriptionId = Request.Headers["SID"];
        if (string.Equals(Request.Method, "subscribe", StringComparison.OrdinalIgnoreCase))
        {
            var notificationType = Request.Headers["NT"];
            var callback = Request.Headers["CALLBACK"];
            var timeoutString = Request.Headers["TIMEOUT"];

            if (string.IsNullOrEmpty(notificationType))
            {
                return dlnaEventManager.RenewEventSubscription(
                    subscriptionId,
                    notificationType,
                    timeoutString,
                    callback);
            }

            return dlnaEventManager.CreateEventSubscription(notificationType, timeoutString, callback);
        }

        return dlnaEventManager.CancelEventSubscription(subscriptionId);
    }

    private void SetResponse(EventSubscriptionResponse eventSubscriptionResponse)
    {
        Response.Headers.Server = HttpUtility.UrlEncode(_dlnaManager.GetServerName());
        Response.ContentLength = 0;
        foreach (var header in eventSubscriptionResponse.Headers)
        {
            Response.Headers[header.Key] = HttpUtility.UrlEncode(header.Value);
        }
    }
}
