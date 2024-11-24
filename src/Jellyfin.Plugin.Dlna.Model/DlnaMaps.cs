using System.Globalization;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="DlnaMaps" />.
/// </summary>
public static class DlnaMaps
{
    /// <summary>
    /// Takes DLNA flags and stringifies them.
    /// </summary>
    /// <param name="flags">The <see cref="DlnaFlags"/>.</param>
    /// <returns>The flags string.</returns>
    public static string FlagsToString(DlnaFlags flags)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:X8}{1:D24}", (ulong)flags, 0);
    }

        /// <summary>
        /// Gets the org operation value.
        /// </summary>
        /// <param name="hasKnownRuntime">Value indicating whether the stream has a known runtime.</param>
        /// <param name="isDirectStream">Value indicating whether the stream is a direct stream.</param>
        /// <param name="profileTranscodeSeekInfo">The <see cref="TranscodeSeekInfo"/>.</param>
        /// <returns>System.String.</returns>
    public static string GetOrgOpValue(bool hasKnownRuntime, bool isDirectStream, TranscodeSeekInfo profileTranscodeSeekInfo)
    {
        if (hasKnownRuntime)
        {
            string orgOp = string.Empty;

            // Time-based seeking currently only possible when transcoding
            orgOp += isDirectStream ? "0" : "1";

            // Byte-based seeking only possible when not transcoding
            orgOp += isDirectStream || profileTranscodeSeekInfo == TranscodeSeekInfo.Bytes ? "1" : "0";

            return orgOp;
        }

        // No seeking is available if we don't know the content runtime
        return "00";
    }

    /// <summary>
    /// Gets the image org operation value.
    /// </summary>
    public static string GetImageOrgOpValue()
    {
        string orgOp = string.Empty;

        // Time-based seeking currently only possible when transcoding
        orgOp += "0";

        // Byte-based seeking only possible when not transcoding
        orgOp += "0";

        return orgOp;
    }
}
