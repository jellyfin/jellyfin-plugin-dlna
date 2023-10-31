#pragma warning disable CS1591

using System;

namespace Jellyfin.Plugin.Dlna.PlayTo
{
    public class PlaybackProgressEventArgs : EventArgs
    {
        public PlaybackProgressEventArgs(UBaseObject mediaInfo)
        {
            MediaInfo = mediaInfo;
        }

        public UBaseObject MediaInfo { get; set; }
    }
}
