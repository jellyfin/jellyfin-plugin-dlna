using System;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Shared DLNA plugin configuration for assemblies that cannot reference the main plugin assembly.
/// </summary>
public static class DlnaPluginConfigurationAccessor
{
    /// <summary>
    /// Gets or sets the default user configured under DLNA plugin settings.
    /// </summary>
    public static Guid? DefaultUserId { get; set; }
}
