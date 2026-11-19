using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings.CancelBooking;
using RoomHub.UnitTests.Fakes;

namespace RoomHub.UnitTests.Features;

public sealed class CancelBookingHandlerTests
{
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task Cancellation_UsesInjectedTimeAndSavesOnlyWhenAllowed(long elapsedTicks, bool allowed)
    {
        var createdAt = new DateTimeOffset(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);
        var booking = Room.Create("Room", 10).Value!.Book("ana",
            createdAt.AddHours(1).UtcDateTime, createdAt.AddHours(2).UtcDateTime, 4, createdAt.UtcDateTime).Value!;
        var unit = new FakeUnitOfWork();
        var handler = new CancelBookingHandler(new FakeBookingRepository(booking), unit,
            new FixedTimeProvider(createdAt.AddTicks(elapsedTicks)));
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(new CancelBookingCommand(booking.Id), cancellation.Token);

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 1 : 0, unit.SaveCalls);
        Assert.Equal(allowed ? BookingStatus.Cancelled : BookingStatus.Active, booking.Status);
        if (allowed)
            Assert.Equal(cancellation.Token, unit.LastToken);
        else
        {
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
            Assert.Equal("CancellationWindowClosed", result.Error.Code);
        }
    }
}
