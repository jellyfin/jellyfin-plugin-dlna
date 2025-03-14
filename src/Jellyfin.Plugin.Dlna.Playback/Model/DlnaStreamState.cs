using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;

namespace Jellyfin.Plugin.Dlna.Playback.Model;

/// <summary>
/// Defines the <see cref="DlnaStreamState" />.
/// </summary>
public class DlnaStreamState : StreamState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaStreamState"/> class.
    /// </summary>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="transcodingType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    public DlnaStreamState(
        IMediaSourceManager mediaSourceManager,
        TranscodingJobType transcodingType,
        ITranscodeManager transcodeManager)
        : base(mediaSourceManager, transcodingType, transcodeManager)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable dlna headers.
    /// </summary>
    public bool EnableDlnaHeaders { get; set; }

    /// <summary>
    /// Gets or sets the device profile.
    /// </summary>
    public DlnaDeviceProfile? DeviceProfile { get; set; }
}
