using System;
using System.Globalization;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Resolves and formats dates for DLNA DIDL metadata (live TV EPG, channels).
/// </summary>
public static class DlnaItemDisplayDate
{
    /// <summary>
    /// Returns whether a date is safe to expose to DLNA clients (avoids Unix epoch display).
    /// </summary>
    public static bool IsValidDlnaDate(DateTime date)
    {
        if (date == default || date.Year < 1980)
        {
            return false;
        }

        return !(date.Year == 1970 && date.Month == 1 && date.Day <= 2);
    }

    /// <summary>
    /// Formats a UTC instant for Dublin Core / UPnP datetime fields (local wall time).
    /// </summary>
    public static string FormatDlnaDateTime(DateTime utcDate, TimeZoneInfo? timeZone = null)
    {
        var zone = timeZone ?? TimeZoneInfo.Local;
        var local = utcDate.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(utcDate, zone)
            : TimeZoneInfo.ConvertTime(utcDate, zone);
        return local.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Display date for a <see cref="MediaBrowser.Controller.LiveTv.LiveTvProgram"/> (air start).
    /// </summary>
    public static DateTime? GetProgramDisplayDateUtc(DateTime startDate)
        => IsValidDlnaDate(startDate) ? startDate : null;

    /// <summary>
    /// Display date for a live TV channel (current programme start, or <paramref name="utcNow"/> if no EPG).
    /// </summary>
    public static DateTime? GetChannelDisplayDateUtc(DateTime? currentProgramStartUtc, DateTime utcNow)
    {
        if (currentProgramStartUtc.HasValue && IsValidDlnaDate(currentProgramStartUtc.Value))
        {
            return currentProgramStartUtc.Value;
        }

        return utcNow;
    }

    /// <summary>
    /// Display date from library premiere metadata when valid.
    /// </summary>
    public static DateTime? GetPremiereDisplayDateUtc(DateTime? premiereDate)
        => premiereDate.HasValue && IsValidDlnaDate(premiereDate.Value) ? premiereDate.Value : null;
}
