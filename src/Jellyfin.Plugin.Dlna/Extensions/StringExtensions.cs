using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Jellyfin.Plugin.Dlna.Extensions;

/// <summary>
/// Extensions for <see cref="string"/>.
/// </summary>
public static partial class StringExtensions
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

    /// <summary>
    /// Masks the credentials a stream url carries, so the url can be logged.
    /// </summary>
    /// <remarks>
    /// The url may be plain, XML escaped or embedded in DIDL metadata, so only the value of each
    /// credential parameter is replaced, up to the character that ends it in any of these forms.
    /// </remarks>
    /// <param name="value">The url, or text holding urls.</param>
    /// <returns>The value with each credential replaced by <c>***</c>.</returns>
    public static string? RedactCredentials(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return CredentialParameterRegex().Replace(value, "$1***");
    }

    [GeneratedRegex(@"\b((?:PlaybackToken|ApiKey|api_key)=)[^&""'<>\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CredentialParameterRegex();
}
