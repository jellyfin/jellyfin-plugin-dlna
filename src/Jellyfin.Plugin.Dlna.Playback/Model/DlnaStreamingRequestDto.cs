using MediaBrowser.Controller.Streaming;

namespace Jellyfin.Plugin.Dlna.Playback.Model;

/// <summary>
/// The audio streaming request dto.
/// </summary>
public class DlnaStreamingRequestDto : StreamingRequestDto
{
    /// <summary>
    /// Gets or sets the device profile.
    /// </summary>
    public string? DeviceProfileId { get; set; }
}
