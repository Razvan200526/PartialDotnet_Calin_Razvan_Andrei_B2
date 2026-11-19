using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoomHub.IntegrationTests.Contracts;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class DailyBookingLimitTests : ApiTestBase
{
    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task ThirdActiveBooking_AcrossDifferentRooms_Returns409(string api)
    {
        var first = await CreateRoomAsync(api);
        var second = await CreateRoomAsync(api);
        var third = await CreateRoomAsync(api);
        await CreateBookingAsync(first.Id, api);
        await CreateBookingAsync(second.Id, api);
        using var rejected = await Client.PostAsJsonAsync($"{api}/rooms/{third.Id}/bookings", Input());
        await AssertProblemAsync(rejected, HttpStatusCode.Conflict, "DailyBookingLimitReached");
        Assert.Equal(2, await WithDbAsync(db => db.Bookings.CountAsync()));
        Assert.Equal(0, await WithDbAsync(db => db.Rooms.Where(r => r.Id == third.Id)
            .Select(r => r.ScheduleVersion).SingleAsync()));
    }

    [Fact]
    public async Task ThirdNonoverlappingBooking_InSameRoom_IsAlsoRejected()
    {
        var room = await CreateRoomAsync();
        await CreateBookingAsync(room.Id);
        using var second = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", Input(120, 180));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var third = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", Input(180, 240));
        await AssertProblemAsync(third, HttpStatusCode.Conflict, "DailyBookingLimitReached");
    }

    [Fact]
    public async Task Cancellation_FreesTheParticipantsDailyAllowance()
    {
        var room = await CreateRoomAsync();
        var secondRoom = await CreateRoomAsync();
        var first = await CreateBookingAsync(room.Id);
        await CreateBookingAsync(secondRoom.Id);
        using var cancel = await Client.PostAsync($"/api/bookings/{first.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        await CreateBookingAsync(room.Id);
        Assert.Equal(2, await WithDbAsync(db => db.Bookings.CountAsync(b => b.Status == RoomHub.Api.Domain.Bookings.BookingStatus.Active)));
    }

    [Fact]
    public async Task DifferentParticipant_HasAnIndependentAllowance()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        await CreateBookingAsync(second.Id);
        using var other = await Client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", Input(bookedBy: "bogdan"));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task TrimmedParticipantNames_ShareTheSameAllowance()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        using var spaced = await Client.PostAsJsonAsync($"/api/rooms/{second.Id}/bookings", Input(bookedBy: "  ana  "));
        Assert.Equal(HttpStatusCode.Created, spaced.StatusCode);
        using var rejected = await Client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", Input(bookedBy: "ana "));
        await AssertProblemAsync(rejected, HttpStatusCode.Conflict, "DailyBookingLimitReached");
    }

    [Fact]
    public async Task OnlyUtcStartDateCounts_EvenWhenBookingCrossesMidnight()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        var crossingMidnight = new BookingInput("ana",
            ApiFactory.Now.AddHours(13.5), ApiFactory.Now.AddHours(14.5), 4);
        using var one = await Client.PostAsJsonAsync($"/api/rooms/{first.Id}/bookings", crossingMidnight);
        using var two = await Client.PostAsJsonAsync($"/api/rooms/{second.Id}/bookings", crossingMidnight);
        Assert.Equal(HttpStatusCode.Created, one.StatusCode);
        Assert.Equal(HttpStatusCode.Created, two.StatusCode);
        var nextDay = crossingMidnight with
        {
            StartUtc = ApiFactory.Now.AddHours(14.5),
            EndUtc = ApiFactory.Now.AddHours(15.5)
        };
        using var allowed = await Client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", nextDay);
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
    }

    [Fact]
    public async Task TimestampOffset_DoesNotChangeTheUtcDayUsedForTheLimit()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        await CreateBookingAsync(second.Id);
        var localNextDay = new BookingInput("ana",
            ApiFactory.Now.AddHours(13).ToOffset(TimeSpan.FromHours(2)),
            ApiFactory.Now.AddHours(14).ToOffset(TimeSpan.FromHours(2)), 4);
        using var rejected = await Client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", localNextDay);
        await AssertProblemAsync(rejected, HttpStatusCode.Conflict, "DailyBookingLimitReached");

        var localPreviousDay = new BookingInput("ana",
            ApiFactory.Now.AddHours(15).ToOffset(TimeSpan.FromHours(-2)),
            ApiFactory.Now.AddHours(16).ToOffset(TimeSpan.FromHours(-2)), 4);
        using var allowed = await Client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", localPreviousDay);
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
    }

    [Fact]
    public async Task AnotherApplicationInstance_UsesThePersistedGlobalAllowance()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        await CreateBookingAsync(second.Id);
        using var another = new ApiFactory(Factory.DatabasePath);
        using var client = another.CreateClient();
        using var rejected = await client.PostAsJsonAsync($"/api/v1/rooms/{third.Id}/bookings", Input());
        await AssertProblemAsync(rejected, HttpStatusCode.Conflict, "DailyBookingLimitReached");
    }

    [Fact]
    public async Task ConcurrentRequests_ForDifferentRooms_CannotExceedTheDailyLimit()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();
        var third = await CreateRoomAsync();
        await CreateBookingAsync(first.Id);
        using var another = new ApiFactory(Factory.DatabasePath);
        using var otherClient = another.CreateClient();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var one = Task.Run(async () =>
        {
            await start.Task;
            return await Client.PostAsJsonAsync($"/api/rooms/{second.Id}/bookings", Input());
        });
        var two = Task.Run(async () =>
        {
            await start.Task;
            return await otherClient.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", Input());
        });
        start.SetResult();
        var responses = await Task.WhenAll(one, two);
        try
        {
            Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
                responses.Select(r => r.StatusCode).Order().ToArray());
            Assert.Equal(2, await WithDbAsync(db => db.Bookings.CountAsync()));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }
}
