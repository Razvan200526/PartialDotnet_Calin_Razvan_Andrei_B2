using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.UnitTests.Fakes;

namespace RoomHub.UnitTests.Features;

public sealed class DailyBookingLimitHandlerTests
{
    [Fact]
    public async Task DailyLimit_IsCheckedInHandlerAndDoesNotStageOrCommitThirdBooking()
    {
        var now = new DateTimeOffset(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);
        var room = Room.Create("Room", 10).Value!;
        var bookings = new FakeBookingRepository { ActiveBookingsOnDay = 2 };
        var unit = new FakeUnitOfWork();
        var handler = new CreateBookingHandler(new FakeRoomRepository(room), bookings, unit, new FixedTimeProvider(now));
        var result = await handler.HandleAsync(new CreateBookingCommand(
            room.Id, "  ana  ", now.AddHours(1).UtcDateTime, now.AddHours(2).UtcDateTime, 4));

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("DailyBookingLimitReached", result.Error.Code);
        Assert.Equal("ana", bookings.LastParticipant);
        Assert.Equal(new DateOnly(2030, 1, 10), bookings.LastDay);
        Assert.Equal(0, bookings.AddCalls);
        Assert.Equal(0, unit.SaveCalls);
        Assert.Equal(0, unit.Transaction.CommitCalls);
        Assert.True(unit.Transaction.IsDisposed);
        Assert.Empty(room.Bookings);
    }
}
