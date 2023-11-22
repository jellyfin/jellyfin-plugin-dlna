using System;
using Jellyfin.Plugin.Dlna.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// DLNA plugin for Jellyfin.
/// </summary>
public class DlnaPlugin : BasePlugin<DlnaPluginConfiguration>
{
    public static DlnaPlugin Instance { get; private set; } = null!;

    public DlnaPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override Guid Id => new("33EBA9CD-7DA1-4720-967F-DD7DAE7B74A1");

    /// <inheritdoc />
    public override string Name => "DLNA";

    /// <inheritdoc />
    public override string Description => "Use Jellyfin as a DLNA server.";
}