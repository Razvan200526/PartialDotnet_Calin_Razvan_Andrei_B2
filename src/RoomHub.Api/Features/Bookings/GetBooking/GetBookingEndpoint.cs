using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings.GetBooking;

public static class GetBookingEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id, IQueryHandler<GetBookingQuery, GetBookingResponse> handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetBookingQuery(id), ct);
            return result.ToHttpResult(booking => Results.Ok(booking));
        }).WithName("GetBooking");
    }
}
