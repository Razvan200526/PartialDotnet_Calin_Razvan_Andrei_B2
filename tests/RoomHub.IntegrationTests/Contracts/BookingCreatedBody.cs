namespace RoomHub.IntegrationTests.Contracts;

public sealed record BookingCreatedBody(Guid Id, Guid RoomId, string Status);
