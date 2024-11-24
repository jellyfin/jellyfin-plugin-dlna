using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="SortCriteria" />.
/// </summary>
public class SortCriteria
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortCriteria"/> class.
    /// </summary>
    /// <param name="sortOrder">The sort order.</param>
    public SortCriteria(string sortOrder)
    {
        if (Enum.TryParse<SortOrder>(sortOrder, true, out var sortOrderValue))
        {
            SortOrder = sortOrderValue;
        }
        else
        {
            SortOrder = SortOrder.Ascending;
        }
    }

    /// <summary>
    /// Gets the sort order.
    /// </summary>
    public SortOrder SortOrder { get; }
}
