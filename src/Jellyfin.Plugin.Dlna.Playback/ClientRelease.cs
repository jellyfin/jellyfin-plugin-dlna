namespace Jellyfin.Plugin.Dlna.Playback;

/// <summary>
/// The result of <see cref="DlnaLiveSessionTracker.Detach"/>.
/// </summary>
/// <param name="RemainingClients">Clients still attached.</param>
/// <param name="WentIdle">True when this detach left zero clients.</param>
public readonly record struct ClientRelease(int RemainingClients, bool WentIdle);
