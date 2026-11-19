using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingResponse(Guid Id, Guid RoomId, BookingStatus Status)
{
    public static CreateBookingResponse From(Booking booking) => new(booking.Id, booking.RoomId, booking.Status);
}
