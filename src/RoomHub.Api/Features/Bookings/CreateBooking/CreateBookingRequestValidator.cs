using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Common.Validation;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed class CreateBookingRequestValidator(IRoomRepository rooms, TimeProvider timeProvider)
    : IRequestValidator<CreateBookingRequest>
{
    public async ValueTask<Result> ValidateAsync(CreateBookingRequest request, HttpContext context, CancellationToken ct)
    {
        var roomId = Guid.Parse(context.Request.RouteValues["roomId"]!.ToString()!);
        var room = await rooms.GetByIdAsync(roomId, ct);
        if (room is null)
            return Result.Failure(Error.NotFound("RoomNotFound", "The room does not exist."));

        return Booking.Validate(request.BookedBy, request.StartUtc.UtcDateTime, request.EndUtc.UtcDateTime,
            request.Attendees, room.Capacity, timeProvider.GetUtcNow().UtcDateTime);
    }
}
