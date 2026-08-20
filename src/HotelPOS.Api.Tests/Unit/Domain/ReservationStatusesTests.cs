using HotelPOS.Domain.Common.Constants;
using Xunit;

namespace HotelPOS.Tests;

public class ReservationStatusesTests
{
    [Fact]
    public void NextStatuses_Reserved_AllowsCheckedInCancelledOrNoShow()
    {
        var next = ReservationStatuses.NextStatuses[ReservationStatuses.Reserved];

        Assert.Equal(new[] { ReservationStatuses.CheckedIn, ReservationStatuses.Cancelled, ReservationStatuses.NoShow }, next);
    }

    [Fact]
    public void NextStatuses_CheckedIn_AllowsCompletedOrCancelled()
    {
        var next = ReservationStatuses.NextStatuses[ReservationStatuses.CheckedIn];

        Assert.Equal(new[] { ReservationStatuses.Completed, ReservationStatuses.Cancelled }, next);
    }

    [Fact]
    public void NextStatuses_Completed_IsTerminal_HasNoNextStatuses()
    {
        var next = ReservationStatuses.NextStatuses[ReservationStatuses.Completed];

        Assert.Empty(next);
    }

    [Fact]
    public void NextStatuses_Cancelled_IsTerminal_HasNoNextStatuses()
    {
        var next = ReservationStatuses.NextStatuses[ReservationStatuses.Cancelled];

        Assert.Empty(next);
    }

    [Fact]
    public void NextStatuses_NoShow_IsTerminal_HasNoNextStatuses()
    {
        var next = ReservationStatuses.NextStatuses[ReservationStatuses.NoShow];

        Assert.Empty(next);
    }

    [Fact]
    public void NextStatuses_ContainsEntryForEveryDefinedStatus()
    {
        foreach (var status in ReservationStatuses.All)
        {
            Assert.True(ReservationStatuses.NextStatuses.ContainsKey(status));
        }
    }
}
