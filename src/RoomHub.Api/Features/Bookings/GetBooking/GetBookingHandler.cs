using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings.GetBooking;

public sealed class GetBookingHandler(IBookingRepository bookings) : IQueryHandler<GetBookingQuery, GetBookingResponse>
{
    public async Task<Result<GetBookingResponse>> HandleAsync(GetBookingQuery query, CancellationToken ct = default)
    {
        var booking = await bookings.GetByIdAsync(query.Id, ct);
        return booking is null
            ? Result<GetBookingResponse>.Failure(Error.NotFound("BookingNotFound", "The booking does not exist."))
            : Result<GetBookingResponse>.Success(GetBookingResponse.From(booking));
    }
}
