#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Core of the AVTransport service. It defines the conceptually top-
/// level state of the transport, for example, whether it is playing, recording, etc.
/// </summary>
public enum TransportState
{
    /// <summary>
    /// Stopped state.
    /// </summary>
    STOPPED,

    /// <summary>
    /// Playing state.
    /// </summary>
    PLAYING,

    /// <summary>
    /// Transitioning state.
    /// </summary>
    TRANSITIONING,

    /// <summary>
    /// Paused state.
    /// </summary>
    PAUSED_PLAYBACK
}
