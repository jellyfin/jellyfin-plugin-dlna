using System;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="MediaChangedEventArgs" />.
/// </summary>
public class MediaChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldMediaInfo">The old media info <see cref="UBaseObject"/>.</param>
    /// <param name="newMediaInfo">The new media info <see cref="UBaseObject"/>.</param>
    public MediaChangedEventArgs(UBaseObject oldMediaInfo, UBaseObject newMediaInfo)
    {
        OldMediaInfo = oldMediaInfo;
        NewMediaInfo = newMediaInfo;
    }

    /// <summary>
    /// Gets or sets the old media info.
    /// </summary>
    public UBaseObject OldMediaInfo { get; set; }

    /// <summary>
    /// Gets or sets the new media info.
    /// </summary>
    public UBaseObject NewMediaInfo { get; set; }
}
