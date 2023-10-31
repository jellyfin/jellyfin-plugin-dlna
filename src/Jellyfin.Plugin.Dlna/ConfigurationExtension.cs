#pragma warning disable CS1591

using Jellyfin.Plugin.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Dlna
{
    public static class ConfigurationExtension
    {
        public static DlnaOptions GetDlnaConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<DlnaOptions>("dlna");
        }
    }
}
