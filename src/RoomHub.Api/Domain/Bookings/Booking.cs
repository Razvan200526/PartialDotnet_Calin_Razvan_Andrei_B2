using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Bookings;

public sealed class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public string BookedBy { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int Attendees { get; private set; }
    public BookingStatus Status { get; private set; }

    internal static Result<Booking> Create(
        Guid roomId, string? bookedBy, DateTime startUtc, DateTime endUtc,
        int attendees, int capacity, DateTime nowUtc)
    {
        var validation = Validate(bookedBy, startUtc, endUtc, attendees, capacity, nowUtc);
        if (!validation.IsSuccess)
            return Result<Booking>.Failure(validation.Error!);

        return Result<Booking>.Success(new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            BookedBy = bookedBy!.Trim(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            Attendees = attendees,
            Status = BookingStatus.Active
        });
    }

    public static Result Validate(
        string? bookedBy, DateTime startUtc, DateTime endUtc,
        int attendees, int capacity, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(bookedBy))
            return Result.Failure(Error.Validation("InvalidBookedBy", "BookedBy is required."));
        if (endUtc <= startUtc)
            return Result.Failure(Error.Validation("InvalidRange", "The end must be after the start."));
        if (attendees <= 0)
            return Result.Failure(Error.Validation("InvalidAttendees", "Attendees must be positive."));
        if (attendees > capacity)
            return Result.Failure(Error.Validation("AttendeesExceedCapacity", "The room does not have enough seats."));
        if (startUtc < nowUtc)
            return Result.Failure(Error.Validation("BookingInPast", "A booking cannot start in the past."));
        var maximumDurationHours = capacity > 10 ? 2 : 4;
        if (endUtc - startUtc > TimeSpan.FromHours(maximumDurationHours))
            return Result.Failure(Error.Validation(
                "BookingTooLong", $"A booking cannot last more than {maximumDurationHours} hours."));

        return Result.Success();
    }

    internal bool Overlaps(DateTime startUtc, DateTime endUtc) =>
        Status == BookingStatus.Active && StartUtc < endUtc && startUtc < EndUtc;

    public Result Cancel(DateTime nowUtc)
    {
        if (Status == BookingStatus.Cancelled)
            return Result.Failure(Error.Conflict("BookingAlreadyCancelled", "The booking is already cancelled."));

        if (StartUtc - nowUtc < TimeSpan.FromMinutes(60))
            return Result.Failure(Error.Conflict(
                "CancellationWindowClosed", "A booking can only be cancelled at least 60 minutes before it starts."));

        Status = BookingStatus.Cancelled;
        return Result.Success();
    }
}
