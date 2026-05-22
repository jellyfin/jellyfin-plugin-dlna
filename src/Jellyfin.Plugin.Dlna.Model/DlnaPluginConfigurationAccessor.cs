namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Shared DLNA plugin configuration for assemblies that cannot reference the main plugin assembly.
/// </summary>
public static class DlnaPluginConfigurationAccessor
{
    /// <summary>
    /// Gets or sets a value indicating whether to burn in subtitles for DLNA playback.
    /// </summary>
    public static bool EnableSubtitleBurnIn { get; set; }
}
