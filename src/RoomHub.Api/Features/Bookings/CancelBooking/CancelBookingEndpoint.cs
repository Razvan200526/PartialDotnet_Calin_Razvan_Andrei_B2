using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings.CancelBooking;

public static class CancelBookingEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, ICommandHandler<CancelBookingCommand, Unit> handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CancelBookingCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });
    }
}
