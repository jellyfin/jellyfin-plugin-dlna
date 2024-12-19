using System;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlaybackStartEventArgs" />.
/// </summary>
public class PlaybackStartEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartEventArgs"/> class.
    /// </summary>
    /// <param name="mediaInfo">The media info <see cref="UBaseObject"/>.</param>
    public PlaybackStartEventArgs(UBaseObject mediaInfo)
    {
        MediaInfo = mediaInfo;
    }

    /// <summary>
    /// Gets or sets the media info.
    /// </summary>
    public UBaseObject MediaInfo { get; set; }
}
