namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// DLNA item types.
/// </summary>
public enum StubType
{
    /// <summary>
    /// Folder stub.
    /// </summary>
    Folder = 0,

    /// <summary>
    /// Latest stub.
    /// </summary>
    Latest = 2,

    /// <summary>
    /// Playlists stub.
    /// </summary>
    Playlists = 3,

    /// <summary>
    /// Albums stub.
    /// </summary>
    Albums = 4,

    /// <summary>
    /// AlbumArtists stub.
    /// </summary>
    AlbumArtists = 5,

    /// <summary>
    /// Artists stub.
    /// </summary>
    Artists = 6,

    /// <summary>
    /// Songs stub.
    /// </summary>
    Songs = 7,

    /// <summary>
    /// Genres stub.
    /// </summary>
    Genres = 8,

    /// <summary>
    /// FavoriteSongs stub.
    /// </summary>
    FavoriteSongs = 9,

    /// <summary>
    /// FavoriteArtists stub.
    /// </summary>
    FavoriteArtists = 10,

    /// <summary>
    /// FavoriteAlbums stub.
    /// </summary>
    FavoriteAlbums = 11,

    /// <summary>
    /// ContinueWatching stub.
    /// </summary>
    ContinueWatching = 12,

    /// <summary>
    /// Movies stub.
    /// </summary>
    Movies = 13,

    /// <summary>
    /// Collections stub.
    /// </summary>
    Collections = 14,

    /// <summary>
    /// Favorites stub.
    /// </summary>
    Favorites = 15,

    /// <summary>
    /// NextUp stub.
    /// </summary>
    NextUp = 16,

    /// <summary>
    /// Series stub.
    /// </summary>
    Series = 17,

    /// <summary>
    /// FavoriteSeries stub.
    /// </summary>
    FavoriteSeries = 18,

    /// <summary>
    /// FavoriteEpisodes stub.
    /// </summary>
    FavoriteEpisodes = 19
}
