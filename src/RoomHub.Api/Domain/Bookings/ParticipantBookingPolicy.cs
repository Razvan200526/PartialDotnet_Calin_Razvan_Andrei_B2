using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Bookings;

public static class ParticipantBookingPolicy
{
    public const int MaximumActiveBookingsPerDay = 2;

    public static Result ValidateDailyLimit(int activeBookingsOnDay) =>
        activeBookingsOnDay < MaximumActiveBookingsPerDay
            ? Result.Success()
            : Result.Failure(Error.Conflict(
                "DailyBookingLimitReached", "A participant cannot have more than two active bookings per UTC day."));
}
