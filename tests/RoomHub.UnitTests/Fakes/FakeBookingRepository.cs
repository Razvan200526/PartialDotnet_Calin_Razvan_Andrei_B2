using RoomHub.Api.Domain.Bookings;

namespace RoomHub.UnitTests.Fakes;

public sealed class FakeBookingRepository(Booking? booking = null) : IBookingRepository
{
    public Booking? Booking { get; private set; } = booking;
    public int AddCalls { get; private set; }
    public int ActiveBookingsOnDay { get; set; }
    public string? LastParticipant { get; private set; }
    public DateOnly LastDay { get; private set; }

    public Task<int> CountActiveForParticipantOnDayAsync(
        string bookedBy, DateOnly utcDay, CancellationToken ct = default)
    {
        LastParticipant = bookedBy;
        LastDay = utcDay;
        return Task.FromResult(ActiveBookingsOnDay);
    }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Booking?.Id == id ? Booking : null);

    public void Add(Booking value)
    {
        AddCalls++;
        Booking = value;
    }
}
