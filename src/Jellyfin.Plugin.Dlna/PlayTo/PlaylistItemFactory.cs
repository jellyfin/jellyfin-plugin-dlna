using System.IO;
using System.Linq;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="PlaylistItemFactory" />.
/// </summary>
public static class PlaylistItemFactory
{
    /// <summary>
    /// Creates a new playlist item.
    /// </summary>
    /// <param name="item">The <see cref="Photo"/>.</param>
    /// <param name="profile">The <see cref="DlnaDeviceProfile"/>.</param>
    public static PlaylistItem Create(Photo item, DlnaDeviceProfile profile)
    {
        var playlistItem = new PlaylistItem
        {
            StreamInfo = new StreamInfo
            {
                ItemId = item.Id,
                MediaType = DlnaProfileType.Photo,
                DeviceProfile = profile
            },

            Profile = profile
        };

        var directPlay = profile.DirectPlayProfiles
            .FirstOrDefault(i => i.Type == DlnaProfileType.Photo && IsSupported(i, item));

        if (directPlay is not null)
        {
            playlistItem.StreamInfo.PlayMethod = PlayMethod.DirectStream;
            playlistItem.StreamInfo.Container = Path.GetExtension(item.Path);

            return playlistItem;
        }

        var transcodingProfile = profile.TranscodingProfiles
            .FirstOrDefault(i => i.Type == DlnaProfileType.Photo);

        if (transcodingProfile is not null)
        {
            playlistItem.StreamInfo.PlayMethod = PlayMethod.Transcode;
            playlistItem.StreamInfo.Container = "." + transcodingProfile.Container.TrimStart('.');
        }

        return playlistItem;
    }

    private static bool IsSupported(DirectPlayProfile profile, Photo item)
    {
        var mediaPath = item.Path;

        if (profile.Container.Length > 0)
        {
            // Check container type
            var mediaContainer = (Path.GetExtension(mediaPath) ?? string.Empty).TrimStart('.');

            if (!profile.SupportsContainer(mediaContainer))
            {
                return false;
            }
        }

        return true;
    }
}
