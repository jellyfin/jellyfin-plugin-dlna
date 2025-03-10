namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="SearchType" />.
/// </summary>
public enum SearchType
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Audio.
    /// </summary>
    Audio = 1,

    /// <summary>
    /// Image.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Video.
    /// </summary>
    Video = 3,

    /// <summary>
    /// Playlist.
    /// </summary>
    Playlist = 4,

    /// <summary>
    /// Music Album.
    /// </summary>
    MusicAlbum = 5
}
