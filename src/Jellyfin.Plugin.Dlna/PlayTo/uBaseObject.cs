#nullable disable

using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="UBaseObject" />.
/// </summary>
public class UBaseObject
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the parent id.
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the second text.
    /// </summary>
    public string SecondText { get; set; }

    /// <summary>
    /// Gets or sets the icon URL.
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the meta data.
    /// </summary>
    public string MetaData { get; set; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the protocol info.
    /// </summary>
    public IReadOnlyList<string> ProtocolInfo { get; set; }

    /// <summary>
    /// Gets or sets the UPnP class.
    /// </summary>
    public string UpnpClass { get; set; }

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public string MediaType
    {
        get
        {
            var classType = UpnpClass ?? string.Empty;

            if (classType.Contains("Audio", StringComparison.Ordinal))
            {
                return "Audio";
            }

            if (classType.Contains("Video", StringComparison.Ordinal))
            {
                return "Video";
            }

            if (classType.Contains("image", StringComparison.Ordinal))
            {
                return "Photo";
            }

            return null;
        }
    }

    /// <inheritdoc />
    public bool Equals(UBaseObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return string.Equals(Id, obj.Id, StringComparison.Ordinal);
    }
}
