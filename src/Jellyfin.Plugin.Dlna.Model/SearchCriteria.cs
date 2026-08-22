using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="SearchCriteria" />.
/// </summary>
public class SearchCriteria
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCriteria"/> class.
    /// </summary>
    /// <param name="search">The search string.</param>
    public SearchCriteria(string search)
    {
        ArgumentException.ThrowIfNullOrEmpty(search);

        SearchType = SearchType.Unknown;

        foreach (var (property, op, value) in ParseRelations(search))
        {
            if (string.Equals("upnp:class", property, StringComparison.OrdinalIgnoreCase)
                && (string.Equals("=", op, StringComparison.Ordinal) || string.Equals("derivedfrom", op, StringComparison.OrdinalIgnoreCase)))
            {
                SearchType = GetSearchType(value);
                continue;
            }

            // Only the operators that widen or match a name are of use, a negated or ordering
            // comparison on a name can not be turned into a library query
            if (!string.Equals("contains", op, StringComparison.OrdinalIgnoreCase)
                && !string.Equals("=", op, StringComparison.Ordinal))
            {
                continue;
            }

            if (value.Length == 0)
            {
                continue;
            }

            if (string.Equals("dc:title", property, StringComparison.OrdinalIgnoreCase))
            {
                NameContains ??= value;
            }
            else if (string.Equals("upnp:artist", property, StringComparison.OrdinalIgnoreCase)
                     || string.Equals("upnp:album", property, StringComparison.OrdinalIgnoreCase)
                     || string.Equals("upnp:albumArtist", property, StringComparison.OrdinalIgnoreCase)
                     || string.Equals("dc:creator", property, StringComparison.OrdinalIgnoreCase))
            {
                SearchTerm ??= value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the search type.
    /// </summary>
    public SearchType SearchType { get; set; }

    /// <summary>
    /// Gets or sets the title the results have to contain, if any.
    /// </summary>
    public string? NameContains { get; set; }

    /// <summary>
    /// Gets or sets the term the results have to match, if any.
    /// </summary>
    public string? SearchTerm { get; set; }

    private static SearchType GetSearchType(string upnpClass)
    {
        if (string.Equals("object.item.imageItem", upnpClass, StringComparison.OrdinalIgnoreCase)
            || string.Equals("object.item.imageItem.photo", upnpClass, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Image;
        }

        if (string.Equals("object.item.videoItem", upnpClass, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Video;
        }

        if (string.Equals("object.item.audioItem", upnpClass, StringComparison.OrdinalIgnoreCase)
            || string.Equals("object.item.audioItem.musicTrack", upnpClass, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Audio;
        }

        if (string.Equals("object.container.playlistContainer", upnpClass, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Playlist;
        }

        if (string.Equals("object.container.album.musicAlbum", upnpClass, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.MusicAlbum;
        }

        return SearchType.Unknown;
    }

    /// <summary>
    /// Splits a UPnP search criteria string into its relational expressions.
    /// </summary>
    /// <param name="search">The search string.</param>
    /// <returns>The property, operator and unquoted value of every relational expression.</returns>
    /// <remarks>
    /// Values are read as the quoted strings they are, so that a value containing a logical
    /// operator or a parenthesis does not tear the expression it belongs to apart.
    /// </remarks>
    private static IEnumerable<(string Property, string Operator, string Value)> ParseRelations(string search)
    {
        var index = 0;
        while (index < search.Length)
        {
            var property = ReadToken(search, ref index);
            if (property.Length == 0)
            {
                break;
            }

            // A logical operator between two expressions, the next token starts an expression
            if (string.Equals("and", property, StringComparison.OrdinalIgnoreCase)
                || string.Equals("or", property, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var op = ReadToken(search, ref index);
            if (op.Length == 0)
            {
                break;
            }

            var value = ReadValue(search, ref index);

            yield return (property, op, value);
        }
    }

    /// <summary>
    /// Reads the next unquoted token, skipping over whitespace and grouping parentheses.
    /// </summary>
    /// <param name="search">The search string.</param>
    /// <param name="index">The position to read from, left behind the token.</param>
    /// <returns>The token, empty when the end of the string was reached.</returns>
    private static string ReadToken(string search, ref int index)
    {
        while (index < search.Length && (char.IsWhiteSpace(search[index]) || search[index] is '(' or ')'))
        {
            index++;
        }

        var start = index;
        while (index < search.Length && !char.IsWhiteSpace(search[index]) && search[index] is not ('(' or ')'))
        {
            index++;
        }

        return search[start..index];
    }

    /// <summary>
    /// Reads the value of a relational expression, unescaping it when it is quoted.
    /// </summary>
    /// <param name="search">The search string.</param>
    /// <param name="index">The position to read from, left behind the value.</param>
    /// <returns>The value.</returns>
    private static string ReadValue(string search, ref int index)
    {
        while (index < search.Length && char.IsWhiteSpace(search[index]))
        {
            index++;
        }

        if (index >= search.Length || search[index] != '"')
        {
            return ReadToken(search, ref index);
        }

        index++;
        var value = new StringBuilder();
        while (index < search.Length && search[index] != '"')
        {
            if (search[index] == '\\' && index + 1 < search.Length)
            {
                index++;
            }

            value.Append(search[index]);
            index++;
        }

        // Step over the closing quote, if the criteria is not truncated
        if (index < search.Length)
        {
            index++;
        }

        return value.ToString();
    }
}
