using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Infrastructure.Persistence.Repositories;

public sealed class EfBookingRepository(AppDbContext db) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Bookings.SingleOrDefaultAsync(booking => booking.Id == id, ct);

    public void Add(Booking booking) => db.Bookings.Add(booking);

    public Task<int> CountActiveForParticipantOnDayAsync(
        string bookedBy, DateOnly utcDay, CancellationToken ct = default)
    {
        var day = utcDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return db.Bookings.CountAsync(booking =>
            booking.BookedBy == bookedBy && booking.Status == BookingStatus.Active
            && booking.StartUtc.Date == day, ct);
    }
}
