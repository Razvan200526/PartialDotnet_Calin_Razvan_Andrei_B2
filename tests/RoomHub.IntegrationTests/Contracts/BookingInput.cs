namespace RoomHub.IntegrationTests.Contracts;

public sealed record BookingInput(string? BookedBy, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int Attendees);
