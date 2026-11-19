using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.UnitTests.Domain;

public sealed class BookingCancellationTests
{
    private static readonly DateTime Now = new(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(-1, 0, true)]
    [InlineData(0, -1, true)]
    [InlineData(0, 0, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(60, 0, false)]
    [InlineData(61, 0, false)]
    [InlineData(121, 0, false)]
    public void Cancellation_RequiresAtLeastSixtyMinutesBeforeStart(
        int elapsedMinutes, int extraTicks, bool allowed)
    {
        var booking = Room.Create("Study room", 10).Value!
            .Book("ana", Now.AddHours(1), Now.AddHours(2), 4, Now).Value!;

        var result = booking.Cancel(Now.AddMinutes(elapsedMinutes).AddTicks(extraTicks));

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? BookingStatus.Cancelled : BookingStatus.Active, booking.Status);
        if (!allowed)
        {
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
            Assert.Equal("CancellationWindowClosed", result.Error.Code);
        }
    }

    [Fact]
    public void RepeatedCancellation_KeepsItsExistingErrorAfterTheWindowCloses()
    {
        var booking = Room.Create("Study room", 10).Value!
            .Book("ana", Now.AddHours(1), Now.AddHours(2), 4, Now).Value!;
        Assert.True(booking.Cancel(Now).IsSuccess);

        var repeated = booking.Cancel(Now.AddHours(3));

        Assert.Equal("BookingAlreadyCancelled", repeated.Error!.Code);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
}
