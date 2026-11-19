using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.UnitTests.Domain;

public sealed class RoomTests
{
    private static readonly DateTime Now = new(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null, 10)]
    [InlineData("", 10)]
    [InlineData("   ", 10)]
    [InlineData("Room", 0)]
    [InlineData("Room", -1)]
    public void InvalidRoom_ReturnsValidation(string? name, int capacity)
    {
        var result = Room.Create(name, capacity);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ValidRoom_TrimsNameAndHasNoPublicSetters()
    {
        var room = Room.Create("  Study room  ", 10).Value!;
        Assert.Equal("Study room", room.Name);
        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.All(typeof(Room).GetProperties(), property => Assert.False(property.SetMethod?.IsPublic ?? false));
        Assert.All(typeof(Booking).GetProperties(), property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    [Theory]
    [InlineData(null, 60, 120, 1)]
    [InlineData(" ", 60, 120, 1)]
    [InlineData("ana", 60, 60, 1)]
    [InlineData("ana", 120, 60, 1)]
    [InlineData("ana", 60, 120, 0)]
    [InlineData("ana", 60, 120, -1)]
    [InlineData("ana", 60, 120, 11)]
    [InlineData("ana", -1, 60, 1)]
    [InlineData("ana", 60, 301, 1)]
    public void InvalidBooking_ReturnsValidationWithoutChangingSchedule(
        string? bookedBy, int startMinutes, int endMinutes, int attendees)
    {
        var room = Room.Create("Study room", 10).Value!;
        var result = room.Book(bookedBy, Now.AddMinutes(startMinutes), Now.AddMinutes(endMinutes), attendees, Now);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(room.Bookings);
        Assert.Equal(0, room.ScheduleVersion);
    }

    [Fact]
    public void BookingAtNow_ForExactlyFourHoursAndFullCapacity_IsAllowed()
    {
        var room = Room.Create("Study room", 10).Value!;
        var result = room.Book("  ana  ", Now, Now.AddHours(4), 10, Now);
        Assert.True(result.IsSuccess);
        Assert.Equal("ana", result.Value!.BookedBy);
        Assert.Equal(BookingStatus.Active, result.Value.Status);
        Assert.Single(room.Bookings);
    }

    [Theory]
    [InlineData(0, 60, true)]
    [InlineData(120, 180, true)]
    [InlineData(60, 120, false)]
    [InlineData(30, 90, false)]
    [InlineData(90, 150, false)]
    [InlineData(0, 180, false)]
    [InlineData(75, 105, false)]
    public void ActiveIntervals_OverlapOnlyWhenTheirInteriorsIntersect(int startMinutes, int endMinutes, bool allowed)
    {
        var room = Room.Create("Study room", 10).Value!;
        Assert.True(room.Book("ana", Now.AddHours(1), Now.AddHours(2), 4, Now).IsSuccess);
        var result = room.Book("bogdan", Now.AddMinutes(startMinutes), Now.AddMinutes(endMinutes), 4, Now);
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 2 : 1, room.Bookings.Count);
        if (!allowed)
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public void Cancellation_FreesIntervalAndRejectsRepeatedCancellation()
    {
        var room = Room.Create("Study room", 10).Value!;
        var booking = room.Book("ana", Now, Now.AddHours(1), 4, Now).Value!;
        Assert.True(booking.Cancel().IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(ErrorType.Conflict, booking.Cancel().Error!.Type);
        Assert.True(room.Book("bogdan", Now, Now.AddHours(1), 4, Now).IsSuccess);
    }
}
