using System.Text;
using System.Xml;

namespace Jellyfin.Plugin.Dlna.Extensions;

/// <summary>
/// Extensions for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Removes all characters that are not valid inside an XML document.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The value without any invalid XML characters.</returns>
    public static string RemoveInvalidXmlChars(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Only allocate once an invalid character is actually encountered.
        StringBuilder? builder = null;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            // Surrogates are invalid on their own but valid as a pair.
            if (i + 1 < value.Length && XmlConvert.IsXmlSurrogatePair(value[i + 1], current))
            {
                builder?.Append(current).Append(value[i + 1]);
                i++;
                continue;
            }

            if (XmlConvert.IsXmlChar(current))
            {
                builder?.Append(current);
                continue;
            }

            builder ??= new StringBuilder(value.Length).Append(value, 0, i);
        }

        return builder?.ToString() ?? value;
    }
}
