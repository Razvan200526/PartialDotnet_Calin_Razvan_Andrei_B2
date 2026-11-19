using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class BookingScheduleTests : ApiTestBase
{
    [Theory]
    [InlineData(60, 120)]
    [InlineData(30, 90)]
    [InlineData(90, 150)]
    [InlineData(0, 180)]
    [InlineData(75, 105)]
    public async Task ActiveOverlap_ReturnsConflictAndDoesNotInsert(int start, int end)
    {
        var room = await CreateRoomAsync();
        await CreateBookingAsync(room.Id);
        using var response = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", Input(start, end));
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "RoomOccupied");
        Assert.Equal(1, await WithDbAsync(db => db.Bookings.CountAsync()));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(120, 180)]
    public async Task TouchingIntervals_AreAllowed(int start, int end)
    {
        var room = await CreateRoomAsync();
        await CreateBookingAsync(room.Id);
        using var response = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", Input(start, end));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SameInterval_InDifferentRooms_IsAllowed()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        await CreateBookingAsync(second.Id);
        Assert.Equal(2, await WithDbAsync(db => db.Bookings.CountAsync()));
    }

    [Fact]
    public async Task Cancellation_FreesIntervalAndRepeatedCancellationConflicts()
    {
        var room = await CreateRoomAsync();
        var booking = await CreateBookingAsync(room.Id);
        using var first = await Client.PostAsync($"/api/bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        using var second = await Client.PostAsync($"/api/bookings/{booking.Id}/cancel", null);
        await AssertProblemAsync(second, HttpStatusCode.Conflict, "BookingAlreadyCancelled");
        await CreateBookingAsync(room.Id);
    }

    [Fact]
    public async Task MissingCancellation_Returns404()
    {
        using var response = await Client.PostAsync($"/api/bookings/{Guid.NewGuid()}/cancel", null);
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "BookingNotFound");
    }
}
