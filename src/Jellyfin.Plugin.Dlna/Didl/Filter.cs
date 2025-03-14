using System;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Defines the <see cref="Filter" />.
/// </summary>
public class Filter
{
    private readonly string[] _fields;
    private readonly bool _all;

    /// <summary>
    /// Initializes a new instance of the <see cref="Filter"/> class.
    /// </summary>
    public Filter()
        : this("*")
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Filter"/> class.
    /// </summary>
    /// <param name="filter">The Filter.</param>
    public Filter(string filter)
    {
        _all = string.Equals(filter, "*", StringComparison.OrdinalIgnoreCase);
        _fields = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gets a value indicating whether the filter contains a field.
    /// </summary>
    /// <param name="field">The field to check.</param>
    /// <returns><c>true</c> if this filter contains the field; otherwise, <c>false</c>.</returns>
    public bool Contains(string field)
    {
        return _all || Array.Exists(_fields, x => x.Equals(field, StringComparison.OrdinalIgnoreCase));
    }
}
