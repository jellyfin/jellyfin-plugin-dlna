using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Jellyfin.Plugin.Dlna.ConnectionManager;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Main;
using Jellyfin.Plugin.Dlna.MediaReceiverRegistrar;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rssdp.Infrastructure;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="DlnaServiceRegistrator" />.
/// </summary>
public class DlnaServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient(NamedClient.Dlna, c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/{1} UPnP/1.0 {2}/{3}",
                        Environment.OSVersion.Platform,
                        Environment.OSVersion,
                        applicationHost.Name,
                        applicationHost.ApplicationVersionString));

                c.DefaultRequestHeaders.Add("CPFN.UPNP.ORG", applicationHost.FriendlyName); // Required for UPnP DeviceArchitecture v2.0
                c.DefaultRequestHeaders.Add("FriendlyName.DLNA.ORG", applicationHost.FriendlyName); // REVIEW: where does this come from?
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
            });

        serviceCollection.AddSingleton<IDlnaManager, DlnaManager>();
        serviceCollection.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();
        serviceCollection.AddSingleton<IContentDirectory, ContentDirectoryService>();
        serviceCollection.AddSingleton<IConnectionManager, ConnectionManagerService>();
        serviceCollection.AddSingleton<IMediaReceiverRegistrar, MediaReceiverRegistrarService>();

        serviceCollection.AddScoped<AudioHelper>();
        serviceCollection.AddScoped<DynamicHlsHelper>();

        serviceCollection.AddSingleton<ISsdpCommunicationsServer>(provider => new SsdpCommunicationsServer(
            provider.GetRequiredService<INetworkManager>(),
            provider.GetRequiredService<ILogger<SsdpCommunicationsServer>>())
        {
            IsShared = true
        });

        serviceCollection.AddHostedService<DlnaHost>();
    }
}
