using System;
using System.Globalization;
using Jellyfin.Plugin.Dlna.Didl;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaItemDisplayDateTests
{
    [Theory]
    [InlineData(1970, 1, 1)]
    [InlineData(1970, 1, 2)]
    [InlineData(1979, 12, 31)]
    public void IsValidDlnaDate_RejectsEpochAndPre1980(int year, int month, int day)
    {
        Assert.False(DlnaItemDisplayDate.IsValidDlnaDate(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData(1980, 1, 1)]
    [InlineData(2024, 6, 15, 20, 30, 0)]
    public void IsValidDlnaDate_AcceptsReasonableDates(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        Assert.True(DlnaItemDisplayDate.IsValidDlnaDate(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc)));
    }

    [Fact]
    public void IsValidDlnaDate_RejectsDefault()
    {
        Assert.False(DlnaItemDisplayDate.IsValidDlnaDate(default));
    }

    [Fact]
    public void GetProgramDisplayDateUtc_UsesStartDateWhenValid()
    {
        var start = new DateTime(2024, 3, 10, 18, 0, 0, DateTimeKind.Utc);
        Assert.Equal(start, DlnaItemDisplayDate.GetProgramDisplayDateUtc(start));
    }

    [Fact]
    public void GetProgramDisplayDateUtc_ReturnsNullForEpoch()
    {
        Assert.Null(DlnaItemDisplayDate.GetProgramDisplayDateUtc(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void GetChannelDisplayDateUtc_PrefersCurrentProgramStart()
    {
        var programStart = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(programStart, DlnaItemDisplayDate.GetChannelDisplayDateUtc(programStart));
    }

    [Fact]
    public void GetChannelDisplayDateUtc_ReturnsNullWithoutEpg()
    {
        Assert.Null(DlnaItemDisplayDate.GetChannelDisplayDateUtc(null));
        Assert.Null(DlnaItemDisplayDate.GetChannelDisplayDateUtc(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void FormatDlnaDate_KeepsUtcCalendarDay()
    {
        var utc = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("2024-06-15", DlnaItemDisplayDate.FormatDlnaDate(utc));
    }

    [Fact]
    public void FormatDlnaDate_FormatsPre1980CalendarDate()
    {
        var premiere = new DateTime(1968, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("1968-04-03", DlnaItemDisplayDate.FormatDlnaDate(premiere));
    }

    [Fact]
    public void FormatDlnaDateTime_UsesUtcWithZone()
    {
        var utc = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
        Assert.Equal("2024-06-15T14:30:45Z", DlnaItemDisplayDate.FormatDlnaDateTime(utc));
    }

    [Fact]
    public void FormatDlnaLocalDateTime_UsesLocalClockWithoutZone()
    {
        var utc = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
        var expected = utc.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        var formatted = DlnaItemDisplayDate.FormatDlnaLocalDateTime(utc);

        Assert.Equal(expected, formatted);
        Assert.DoesNotContain("Z", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatDlnaLocalDateTime_KeepsALocalInstantOnTheSameClock()
    {
        var local = new DateTime(2024, 6, 15, 17, 40, 0, DateTimeKind.Local);

        Assert.Equal("2024-06-15T17:40:00", DlnaItemDisplayDate.FormatDlnaLocalDateTime(local));
    }

    [Fact]
    public void GetProgramDisplayDateUtc_ReturnsNullForPre1980()
    {
        Assert.Null(DlnaItemDisplayDate.GetProgramDisplayDateUtc(new DateTime(1968, 4, 3, 0, 0, 0, DateTimeKind.Utc)));
    }
}
