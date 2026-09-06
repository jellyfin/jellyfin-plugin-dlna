using System;
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
        var now = new DateTime(2024, 5, 1, 12, 30, 0, DateTimeKind.Utc);

        Assert.Equal(programStart, DlnaItemDisplayDate.GetChannelDisplayDateUtc(programStart, now));
    }

    [Fact]
    public void GetChannelDisplayDateUtc_FallsBackToUtcNowWithoutEpg()
    {
        var now = new DateTime(2024, 5, 1, 12, 30, 0, DateTimeKind.Utc);

        Assert.Equal(now, DlnaItemDisplayDate.GetChannelDisplayDateUtc(null, now));
        Assert.Equal(now, DlnaItemDisplayDate.GetChannelDisplayDateUtc(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), now));
    }

    [Fact]
    public void FormatDlnaDateTime_UsesUtcWallTimeWhenTimeZoneSpecified()
    {
        var utc = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
        Assert.Equal("2024-06-15T14:30:45", DlnaItemDisplayDate.FormatDlnaDateTime(utc, TimeZoneInfo.Utc));
    }
}
