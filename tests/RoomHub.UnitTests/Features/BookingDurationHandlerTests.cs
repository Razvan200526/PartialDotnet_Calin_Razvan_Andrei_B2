using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.UnitTests.Fakes;

namespace RoomHub.UnitTests.Features;

public sealed class BookingDurationHandlerTests
{
    [Fact]
    public async Task LargeRoomDuration_IsValidatedBeforeDailyQuotaWithoutStagingOrCommitting()
    {
        var now = new DateTimeOffset(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);
        var room = Room.Create("Large study room", 11).Value!;
        var bookings = new FakeBookingRepository { ActiveBookingsOnDay = 2 };
        var unit = new FakeUnitOfWork();
        var handler = new CreateBookingHandler(new FakeRoomRepository(room), bookings, unit, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(new CreateBookingCommand(
            room.Id, "ana", now.UtcDateTime, now.AddHours(3).UtcDateTime, 1));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal("BookingTooLong", result.Error.Code);
        Assert.Null(bookings.LastParticipant);
        Assert.Equal(0, bookings.AddCalls);
        Assert.Equal(0, unit.SaveCalls);
        Assert.Equal(0, unit.Transaction.CommitCalls);
        Assert.True(unit.Transaction.IsDisposed);
        Assert.Empty(room.Bookings);
    }
}
