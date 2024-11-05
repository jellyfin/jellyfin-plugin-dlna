using System;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Service;
using MediaBrowser.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.ConnectionManager;

/// <summary>
/// Defines the <see cref="ControlHandler" />.
/// </summary>
public class ControlHandler : BaseControlHandler
{
    private readonly DlnaDeviceProfile _profile;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlHandler"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="profile">The <see cref="DlnaDeviceProfile"/>.</param>
    public ControlHandler(ILogger logger, DlnaDeviceProfile profile)
        : base(logger)
    {
        _profile = profile;
    }

    /// <inheritdoc />
    protected override void WriteResult(string methodName, IReadOnlyDictionary<string, string> methodParams, XmlWriter xmlWriter)
    {
        if (string.Equals(methodName, "GetProtocolInfo", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetProtocolInfo(xmlWriter);
            return;
        }

        throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
    }

    /// <summary>
    /// Builds the response to the GetProtocolInfo request.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private void HandleGetProtocolInfo(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString("Source", _profile.ProtocolInfo);
        xmlWriter.WriteElementString("Sink", string.Empty);
    }
}
