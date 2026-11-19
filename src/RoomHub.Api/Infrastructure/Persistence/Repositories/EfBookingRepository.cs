using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Infrastructure.Persistence.Repositories;

public sealed class EfBookingRepository(AppDbContext db) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Bookings.SingleOrDefaultAsync(booking => booking.Id == id, ct);

    public void Add(Booking booking) => db.Bookings.Add(booking);
}
