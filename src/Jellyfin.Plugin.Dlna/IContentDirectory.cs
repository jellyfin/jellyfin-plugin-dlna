namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="IContentDirectory" /> interface.
/// </summary>
public interface IContentDirectory : IDlnaEventManager, IUpnpService
{
}
