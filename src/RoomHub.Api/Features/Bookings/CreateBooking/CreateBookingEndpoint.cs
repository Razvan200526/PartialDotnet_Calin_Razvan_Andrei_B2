using RoomHub.Api.Features.Common;
using RoomHub.Api.Features.Common.Validation;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public static class CreateBookingEndpoint
{
    public static void Map(RouteGroupBuilder rooms)
    {
        rooms.MapPost("/{roomId:guid}/bookings", async (
            Guid roomId, CreateBookingRequest request, HttpRequest httpRequest,
            ICommandHandler<CreateBookingCommand, CreateBookingResponse> handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CreateBookingCommand(
                roomId, request.BookedBy, request.StartUtc.UtcDateTime, request.EndUtc.UtcDateTime, request.Attendees), ct);
            return result.ToHttpResult(booking =>
                Results.Created($"{httpRequest.GetApiBasePath()}/bookings/{booking.Id}", booking));
        }).ValidateRequest<CreateBookingRequest>();
    }
}
