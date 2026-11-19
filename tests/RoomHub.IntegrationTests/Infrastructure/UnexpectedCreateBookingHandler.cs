using RoomHub.Api.Domain.Common;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Common;

namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class UnexpectedCreateBookingHandler : ICommandHandler<CreateBookingCommand, CreateBookingResponse>
{
    public Task<Result<CreateBookingResponse>> HandleAsync(CreateBookingCommand command, CancellationToken ct = default) =>
        throw new InvalidOperationException("Invalid route input must not reach this handler.");
}
