#pragma warning disable CS1591

namespace Jellyfin.Plugin.Dlna
{
    public interface IMediaReceiverRegistrar : IDlnaEventManager, IUpnpService
    {
    }
}
