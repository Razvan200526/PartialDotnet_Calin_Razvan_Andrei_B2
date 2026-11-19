namespace RoomHub.IntegrationTests.Contracts;

public sealed record BookingBody(
    Guid Id, Guid RoomId, string BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees, string Status);
