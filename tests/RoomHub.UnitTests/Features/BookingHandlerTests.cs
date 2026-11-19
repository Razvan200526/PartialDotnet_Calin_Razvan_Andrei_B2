using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings.CancelBooking;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.UnitTests.Fakes;

namespace RoomHub.UnitTests.Features;

public sealed class BookingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MissingRoom_TakesPrecedenceOverInvalidFieldsAndNeverSaves()
    {
        var bookings = new FakeBookingRepository();
        var unit = new FakeUnitOfWork();
        var handler = new CreateBookingHandler(new FakeRoomRepository(), bookings, unit, new FixedTimeProvider(Now));
        var result = await handler.HandleAsync(new CreateBookingCommand(Guid.NewGuid(), null, default, default, -1));
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Equal(0, bookings.AddCalls);
        Assert.Equal(0, unit.SaveCalls);
    }

    [Fact]
    public async Task InvalidBooking_IsNotAddedOrSaved()
    {
        var room = Room.Create("Study room", 10).Value!;
        var bookings = new FakeBookingRepository();
        var unit = new FakeUnitOfWork();
        var handler = new CreateBookingHandler(new FakeRoomRepository(room), bookings, unit, new FixedTimeProvider(Now));
        var result = await handler.HandleAsync(new CreateBookingCommand(
            room.Id, "ana", Now.AddMinutes(-1).UtcDateTime, Now.AddHours(1).UtcDateTime, 1));
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(0, bookings.AddCalls);
        Assert.Equal(0, unit.SaveCalls);
    }

    [Fact]
    public async Task ValidBooking_UsesInjectedTimeAndCommitsOnceWithCancellationToken()
    {
        var room = Room.Create("Study room", 10).Value!;
        var bookings = new FakeBookingRepository();
        var unit = new FakeUnitOfWork();
        using var cancellation = new CancellationTokenSource();
        var handler = new CreateBookingHandler(new FakeRoomRepository(room), bookings, unit, new FixedTimeProvider(Now));
        var result = await handler.HandleAsync(new CreateBookingCommand(
            room.Id, "ana", Now.UtcDateTime, Now.AddHours(4).UtcDateTime, 10), cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, bookings.AddCalls);
        Assert.Equal(1, unit.SaveCalls);
        Assert.Equal(cancellation.Token, unit.LastToken);
    }

    [Fact]
    public async Task FailedCommit_ReturnsConflictRatherThanCreatedResponse()
    {
        var room = Room.Create("Study room", 10).Value!;
        var unit = new FakeUnitOfWork { SaveResult = Result.Failure(Error.Conflict("ConcurrentChange", "Refresh.")) };
        var handler = new CreateBookingHandler(new FakeRoomRepository(room), new FakeBookingRepository(),
            unit, new FixedTimeProvider(Now));
        var result = await handler.HandleAsync(new CreateBookingCommand(
            room.Id, "ana", Now.UtcDateTime, Now.AddHours(1).UtcDateTime, 1));
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CancelMissingOrAlreadyCancelled_DoesNotCommit()
    {
        var unit = new FakeUnitOfWork();
        var missing = await new CancelBookingHandler(new FakeBookingRepository(), unit)
            .HandleAsync(new CancelBookingCommand(Guid.NewGuid()));
        Assert.Equal(ErrorType.NotFound, missing.Error!.Type);
        var booking = Room.Create("Room", 10).Value!
            .Book("ana", Now.UtcDateTime, Now.AddHours(1).UtcDateTime, 1, Now.UtcDateTime).Value!;
        booking.Cancel();
        var cancelled = await new CancelBookingHandler(new FakeBookingRepository(booking), unit)
            .HandleAsync(new CancelBookingCommand(booking.Id));
        Assert.Equal(ErrorType.Conflict, cancelled.Error!.Type);
        Assert.Equal(0, unit.SaveCalls);
    }
}
