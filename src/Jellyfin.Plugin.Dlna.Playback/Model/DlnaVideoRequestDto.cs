using MediaBrowser.Controller.Streaming;

namespace Jellyfin.Plugin.Dlna.Playback.Model;

/// <summary>
/// The video request dto.
/// </summary>
public class DlnaVideoRequestDto : VideoRequestDto
{
    /// <summary>
    /// Gets or sets the device profile.
    /// </summary>
    public string? DeviceProfileId { get; set; }
}
