using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public sealed record CreateRoomResponse(Guid Id, string Name, int Capacity)
{
    public static CreateRoomResponse From(Room room) => new(room.Id, room.Name, room.Capacity);
}
