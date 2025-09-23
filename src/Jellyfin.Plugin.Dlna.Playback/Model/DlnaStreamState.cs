using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using System;

namespace Jellyfin.Plugin.Dlna.Playback.Model;

public class DlnaStreamState : StreamState
{
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

    public int TargetStreamCount
    {
        get
        {
            if (BaseRequest.Static)
            {
                return MediaSource.MediaStreams.Count;
            }

            return Math.Min(MediaSource.MediaStreams.Count, 1);
        }
    }
}
