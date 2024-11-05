using System;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlaybackProgressEventArgs" />.
/// </summary>
public class PlaybackProgressEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackProgressEventArgs"/> class.
    /// </summary>
    /// <param name="mediaInfo">The media info <see cref="UBaseObject"/>.</param>
    public PlaybackProgressEventArgs(UBaseObject mediaInfo)
    {
        MediaInfo = mediaInfo;
    }

    /// <summary>
    /// Gets or sets the media info.
    /// </summary>
    public UBaseObject MediaInfo { get; set; }
}
