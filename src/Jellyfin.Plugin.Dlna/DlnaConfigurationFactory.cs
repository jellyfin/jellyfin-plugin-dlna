#pragma warning disable CS1591

using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Dlna
{
    public class DlnaConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                    Key = "dlna",
                    ConfigurationType = typeof(DlnaOptions)
                }
            };
        }
    }
}
