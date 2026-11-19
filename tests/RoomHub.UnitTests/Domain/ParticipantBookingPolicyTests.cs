using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;

namespace RoomHub.UnitTests.Domain;

public sealed class ParticipantBookingPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void ThirdActiveBookingOnDay_IsAConflict(int activeCount, bool allowed)
    {
        var result = ParticipantBookingPolicy.ValidateDailyLimit(activeCount);
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed)
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
