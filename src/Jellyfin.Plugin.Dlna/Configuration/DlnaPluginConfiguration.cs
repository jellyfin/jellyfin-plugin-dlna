using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Dlna.Configuration;

public class DlnaPluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether gets or sets a value to indicate the status of the dlna playTo subsystem.
    /// </summary>
    public bool EnablePlayTo { get; set; } = true;

    /// <summary>
    /// Gets or sets the ssdp client discovery interval time (in seconds).
    /// This is the time after which the server will send a ssdp search request.
    /// </summary>
    public int ClientDiscoveryIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to blast alive messages.
    /// </summary>
    public bool BlastAliveMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the frequency at which ssdp alive notifications are transmitted.
    /// </summary>
    public int AliveMessageIntervalSeconds { get; set; }  = 180;

    /// <summary>
    /// gets or sets a value indicating whether to send only matched host.
    /// </summary>
    public bool SendOnlyMatchedHost { get; set; } = true;

    /// <summary>
    /// Gets or sets the default user account that the dlna server uses.
    /// </summary>
    public string? DefaultUserId { get; set; }
}
