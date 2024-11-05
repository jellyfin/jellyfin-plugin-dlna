# nullable disable

using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlaylistItem" />.
/// </summary>
public class PlaylistItem
{
    /// <summary>
    /// Gets or sets the stream URL.
    /// </summary>
    public string StreamUrl { get; set; }

    /// <summary>
    /// Gets or sets the DIDL.
    /// </summary>
    public string Didl { get; set; }

    /// <summary>
    /// Gets or sets the stream info.
    /// </summary>
    public StreamInfo StreamInfo { get; set; }

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    public DlnaDeviceProfile Profile { get; set; }
}
