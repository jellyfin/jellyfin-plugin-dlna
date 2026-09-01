using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Defines the <see cref="ServerItem" />.
/// </summary>
internal sealed class ServerItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerItem"/> class.
    /// </summary>
    /// <param name="item">The underlying item.</param>
    /// <param name="stubType">The virtual folder type.</param>
    /// <param name="virtualFolderName">The displayed name of the virtual folder.</param>
    /// <param name="idSuffix">The optional suffix encoded in the DLNA object ID.</param>
    public ServerItem(BaseItem item, StubType? stubType, string? virtualFolderName = null, string? idSuffix = null)
    {
        Item = item;
        VirtualFolderName = virtualFolderName;
        IdSuffix = idSuffix;

        if (stubType.HasValue)
        {
            StubType = stubType;
        }
        else if (item is IItemByName and not Folder)
        {
            StubType = ContentDirectory.StubType.Folder;
        }
    }

    /// <summary>
    /// Gets the underlying base item.
    /// </summary>
    public BaseItem Item { get; }

    /// <summary>
    /// Gets the DLNA item type.
    /// </summary>
    public StubType? StubType { get; }

    /// <summary>
    /// Gets the display name for a virtual folder.
    /// </summary>
    public string? VirtualFolderName { get; }

    /// <summary>
    /// Gets the suffix appended to the virtual folder object id.
    /// </summary>
    public string? IdSuffix { get; }
}
