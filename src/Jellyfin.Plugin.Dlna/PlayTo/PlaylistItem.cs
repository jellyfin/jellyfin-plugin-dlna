using System;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlaylistItem" />.
/// </summary>
public class PlaylistItem
{
    /// <summary>
    /// Gets or sets the user who authorized this playback item.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the stream URL.
    /// </summary>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DIDL.
    /// </summary>
    public string Didl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream info.
    /// </summary>
    public required StreamInfo StreamInfo { get; set; }

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    public required DlnaDeviceProfile Profile { get; set; }
}
