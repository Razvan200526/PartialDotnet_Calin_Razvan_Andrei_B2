using RoomHub.Api.Domain.Rooms;

namespace RoomHub.UnitTests.Fakes;

public sealed class FakeRoomRepository(Room? room = null) : IRoomRepository
{
    public Room? Room { get; private set; } = room;
    public int AddCalls { get; private set; }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Room?.Id == id ? Room : null);

    public void Add(Room value)
    {
        AddCalls++;
        Room = value;
    }
}
