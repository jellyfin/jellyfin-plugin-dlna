#nullable disable

#pragma warning disable CS1591

using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.PlayTo;

public class PlaylistItem
{
    public string StreamUrl { get; set; }

    public string Didl { get; set; }

    public StreamInfo StreamInfo { get; set; }

    public DlnaDeviceProfile Profile { get; set; }
}
