namespace RoomHub.Api.Domain.Rooms;

public interface IRoomRepository
{
    // Include the schedule so Room can enforce availability itself.
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Room room);
}
