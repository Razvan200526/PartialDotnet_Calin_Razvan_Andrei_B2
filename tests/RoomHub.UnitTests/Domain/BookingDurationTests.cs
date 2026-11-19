using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.UnitTests.Domain;

public sealed class BookingDurationTests
{
    private static readonly DateTime Now = new(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(9, 3, 0, true)]
    [InlineData(9, 4, 0, true)]
    [InlineData(9, 4, 1, false)]
    [InlineData(10, 4, -1, true)]
    [InlineData(10, 4, 0, true)]
    [InlineData(10, 4, 1, false)]
    [InlineData(11, 2, -1, true)]
    [InlineData(11, 2, 0, true)]
    [InlineData(11, 2, 1, false)]
    [InlineData(11, 4, 0, false)]
    [InlineData(20, 2, 0, true)]
    [InlineData(20, 3, 0, false)]
    public void MaximumDuration_DependsOnRoomCapacity(
        int capacity, int hours, int extraTicks, bool allowed)
    {
        var room = Room.Create("Study room", capacity).Value!;
        var result = room.Book("ana", Now, Now.AddHours(hours).AddTicks(extraTicks), 1, Now);

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 1 : 0, room.Bookings.Count);
        Assert.Equal(allowed ? 1 : 0, room.ScheduleVersion);
        if (!allowed)
        {
            Assert.Equal(ErrorType.Validation, result.Error!.Type);
            Assert.Equal("BookingTooLong", result.Error.Code);
        }
    }

    [Theory]
    [InlineData(10, 5, "A booking cannot last more than 4 hours.")]
    [InlineData(11, 3, "A booking cannot last more than 2 hours.")]
    public void RejectedDuration_ReportsTheApplicableLimit(int capacity, int hours, string message)
    {
        var room = Room.Create("Study room", capacity).Value!;
        var result = room.Book("ana", Now, Now.AddHours(hours), 1, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(message, result.Error!.Message);
    }
}
