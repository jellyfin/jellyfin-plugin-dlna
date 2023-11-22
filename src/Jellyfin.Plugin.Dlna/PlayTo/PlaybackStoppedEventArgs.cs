#pragma warning disable CS1591

using System;

namespace Jellyfin.Plugin.Dlna.PlayTo;

public class PlaybackStoppedEventArgs : EventArgs
{
    public PlaybackStoppedEventArgs(UBaseObject mediaInfo)
    {
        MediaInfo = mediaInfo;
    }

    public UBaseObject MediaInfo { get; set; }
}
