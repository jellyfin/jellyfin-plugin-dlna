namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="IConnectionManager" /> interface.
/// </summary>
public interface IConnectionManager : IDlnaEventManager, IUpnpService
{
}
