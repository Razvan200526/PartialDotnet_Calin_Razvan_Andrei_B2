using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Rooms;

public sealed class Room
{
    private readonly List<Booking> _bookings = [];

    private Room() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public int ScheduleVersion { get; private set; }
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    public static Result<Room> Create(string? name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Room>.Failure(Error.Validation("InvalidName", "Room name is required."));
        if (capacity <= 0)
            return Result<Room>.Failure(Error.Validation("InvalidCapacity", "Capacity must be positive."));

        return Result<Room>.Success(new Room
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Capacity = capacity
        });
    }

    public Result<Booking> Book(
        string? bookedBy, DateTime startUtc, DateTime endUtc,
        int attendees, DateTime nowUtc)
    {
        var result = Booking.Create(Id, bookedBy, startUtc, endUtc, attendees, Capacity, nowUtc);
        if (!result.IsSuccess)
            return result;

        if (_bookings.Any(existing => existing.Overlaps(startUtc, endUtc)))
            return Result<Booking>.Failure(Error.Conflict("RoomOccupied", "The room is occupied in this interval."));

        _bookings.Add(result.Value!);
        ScheduleVersion++;
        return result;
    }
}
