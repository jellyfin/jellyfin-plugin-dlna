#pragma warning disable CS1591

using System;

namespace Jellyfin.Plugin.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public PlaybackStartEventArgs(UBaseObject mediaInfo)
        {
            MediaInfo = mediaInfo;
        }

        public UBaseObject MediaInfo { get; set; }
    }
}
