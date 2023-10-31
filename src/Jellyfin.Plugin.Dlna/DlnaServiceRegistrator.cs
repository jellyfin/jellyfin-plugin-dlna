using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Jellyfin.Plugin.Dlna.ConnectionManager;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Jellyfin.Plugin.Dlna.Main;
using Jellyfin.Plugin.Dlna.MediaReceiverRegistrar;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Playback;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rssdp.Infrastructure;

namespace Jellyfin.Plugin.Dlna;

public class DlnaServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services)
    {
        // TODO
        /*services.AddHttpClient(NamedClient.Dlna, c =>
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
            });*/

        var appName = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).ProductName;
        var appFriendlyName = Environment.MachineName;
        var appVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString(3);
        services.AddHttpClient(NamedClient.Dlna, c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/{1} UPnP/1.0 {2}/{3}",
                        Environment.OSVersion.Platform,
                        Environment.OSVersion,
                        appName,
                        appVersion));

                c.DefaultRequestHeaders.Add("CPFN.UPNP.ORG", appFriendlyName); // Required for UPnP DeviceArchitecture v2.0
                c.DefaultRequestHeaders.Add("FriendlyName.DLNA.ORG", appFriendlyName); // REVIEW: where does this come from?
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
            });

        services.AddSingleton<IDlnaManager, DlnaManager>();
        services.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();
        services.AddSingleton<IContentDirectory, ContentDirectoryService>();
        services.AddSingleton<IConnectionManager, ConnectionManagerService>();
        services.AddSingleton<IMediaReceiverRegistrar, MediaReceiverRegistrarService>();

        services.AddSingleton<TranscodingJobHelper>();
        services.AddScoped<AudioHelper>();
        services.AddScoped<DynamicHlsHelper>();

        services.AddSingleton<ISsdpCommunicationsServer>(provider => new SsdpCommunicationsServer(
            provider.GetRequiredService<INetworkManager>(),
            provider.GetRequiredService<ILogger<SsdpCommunicationsServer>>())
        {
            IsShared = true
        });

        services.AddHostedService<DlnaHost>();
    }
}