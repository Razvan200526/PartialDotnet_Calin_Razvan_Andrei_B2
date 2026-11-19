using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Rooms.GetRoom;

public sealed record GetRoomResponse(Guid Id, string Name, int Capacity)
{
    public static GetRoomResponse From(Room room) => new(room.Id, room.Name, room.Capacity);
}
