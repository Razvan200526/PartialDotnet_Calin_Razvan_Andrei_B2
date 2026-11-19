namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingRequest(
    string? BookedBy, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int Attendees);
