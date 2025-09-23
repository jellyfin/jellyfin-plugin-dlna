namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="IMediaReceiverRegistrar" /> interface.
/// </summary>
public interface IMediaReceiverRegistrar : IDlnaEventManager, IUpnpService
{
}
