#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.Dlna.Model
{
    public class SortCriteria
    {
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

        public SortOrder SortOrder { get; }
    }
}