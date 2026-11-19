using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Infrastructure.Persistence.Repositories;

public sealed class EfRoomRepository(AppDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Rooms.Include(room => room.Bookings).SingleOrDefaultAsync(room => room.Id == id, ct);

    public void Add(Room room) => db.Rooms.Add(room);
}
