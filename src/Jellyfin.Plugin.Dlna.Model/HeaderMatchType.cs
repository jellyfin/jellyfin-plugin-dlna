namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="HeaderMatchType" />.
/// </summary>
public enum HeaderMatchType
{
    /// <summary>
    /// Equals.
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Regex.
    /// </summary>
    Regex = 1,

    /// <summary>
    /// Substring.
    /// </summary>
    Substring = 2
}
