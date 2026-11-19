using RoomHub.Api.Domain.Bookings;

namespace RoomHub.UnitTests.Fakes;

public sealed class FakeBookingRepository(Booking? booking = null) : IBookingRepository
{
    public Booking? Booking { get; private set; } = booking;
    public int AddCalls { get; private set; }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Booking?.Id == id ? Booking : null);

    public void Add(Booking value)
    {
        AddCalls++;
        Booking = value;
    }
}
