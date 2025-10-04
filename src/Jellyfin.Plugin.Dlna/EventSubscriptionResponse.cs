using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="EventSubscriptionResponse" />.
/// </summary>
public class EventSubscriptionResponse
{
    /// <summary>
    /// Gets the headers dictionary.
    /// </summary>
    public Dictionary<string, string> Headers { get; } = [];
}
