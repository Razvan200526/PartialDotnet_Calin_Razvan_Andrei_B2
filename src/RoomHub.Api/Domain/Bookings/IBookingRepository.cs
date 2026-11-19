namespace RoomHub.Api.Domain.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> CountActiveForParticipantOnDayAsync(string bookedBy, DateOnly utcDay, CancellationToken ct = default);
    void Add(Booking booking);
}
