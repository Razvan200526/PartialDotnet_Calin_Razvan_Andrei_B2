using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Features.Bookings.GetBooking;

public sealed record GetBookingResponse(
    Guid Id, Guid RoomId, string BookedBy, DateTime StartUtc, DateTime EndUtc,
    int Attendees, BookingStatus Status)
{
    public static GetBookingResponse From(Booking booking) => new(
        booking.Id, booking.RoomId, booking.BookedBy, booking.StartUtc, booking.EndUtc, booking.Attendees, booking.Status);
}
