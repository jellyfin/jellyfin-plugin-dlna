using System;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Resolves and formats dates for DLNA DIDL metadata (live TV EPG, channels).
/// </summary>
public static class DlnaItemDisplayDate
{
    /// <summary>
    /// Returns whether a live TV date is safe to expose to DLNA clients (avoids Unix epoch display).
    /// </summary>
    /// <param name="date">The date to validate.</param>
    /// <returns>True if the date is valid for live TV metadata; otherwise false.</returns>
    public static bool IsValidDlnaDate(DateTime date)
        => date.Year >= 1980;

    /// <summary>
    /// Formats a UTC instant as a calendar date, without converting to local time.
    /// </summary>
    /// <param name="utcDate">The UTC (or unspecified) date to format.</param>
    /// <returns>A <c>yyyy-MM-dd</c> string.</returns>
    public static string FormatDlnaDate(DateTime utcDate)
        => AsUtc(utcDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a UTC instant for UPnP schedule fields, including the UTC designator.
    /// </summary>
    /// <param name="utcDate">The UTC (or unspecified) date to format.</param>
    /// <returns>A <c>yyyy-MM-ddTHH:mm:ssZ</c> string.</returns>
    public static string FormatDlnaDateTime(DateTime utcDate)
        => AsUtc(utcDate).ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a UTC instant as a local clock time, without a timezone designator.
    /// Samsung live TV info bars parse this and ignore a trailing Z.
    /// </summary>
    /// <param name="utcDate">The UTC (or unspecified) date to format.</param>
    /// <returns>A <c>yyyy-MM-ddTHH:mm:ss</c> string in the server's local time.</returns>
    public static string FormatDlnaLocalDateTime(DateTime utcDate)
        => AsUtc(utcDate).ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Display date for a <see cref="MediaBrowser.Controller.LiveTv.LiveTvProgram"/> (air start).
    /// </summary>
    /// <param name="startDate">The programme start date.</param>
    /// <returns>The start date when valid; otherwise null.</returns>
    public static DateTime? GetProgramDisplayDateUtc(DateTime startDate)
        => IsValidDlnaDate(startDate) ? startDate : null;

    /// <summary>
    /// Display date for a live TV channel. A channel with no guide data has no date.
    /// </summary>
    /// <param name="currentProgramStartUtc">The current EPG programme start, if any.</param>
    /// <returns>The programme start when it is valid; otherwise null.</returns>
    public static DateTime? GetChannelDisplayDateUtc(DateTime? currentProgramStartUtc)
    {
        if (currentProgramStartUtc.HasValue && IsValidDlnaDate(currentProgramStartUtc.Value))
        {
            return currentProgramStartUtc.Value;
        }

        return null;
    }

    private static DateTime AsUtc(DateTime date)
        => date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : DateTime.SpecifyKind(date, DateTimeKind.Utc);
}
