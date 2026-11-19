using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.IntegrationTests.Contracts;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class CancellationWindowTests
{
    [Theory]
    [InlineData("/api", -1L, HttpStatusCode.NoContent)]
    [InlineData("/api/v1", -1L, HttpStatusCode.NoContent)]
    [InlineData("/api", 0L, HttpStatusCode.NoContent)]
    [InlineData("/api/v1", 0L, HttpStatusCode.NoContent)]
    [InlineData("/api", 1L, HttpStatusCode.Conflict)]
    [InlineData("/api/v1", 1L, HttpStatusCode.Conflict)]
    [InlineData("/api", TimeSpan.TicksPerMinute, HttpStatusCode.Conflict)]
    [InlineData("/api/v1", TimeSpan.TicksPerMinute, HttpStatusCode.Conflict)]
    [InlineData("/api", TimeSpan.TicksPerHour, HttpStatusCode.Conflict)]
    [InlineData("/api/v1", TimeSpan.TicksPerHour, HttpStatusCode.Conflict)]
    [InlineData("/api", TimeSpan.TicksPerHour * 2, HttpStatusCode.Conflict)]
    [InlineData("/api/v1", TimeSpan.TicksPerHour * 2, HttpStatusCode.Conflict)]
    public async Task CancellationWindow_PreservesBoundaryAndPersistedStatus(
        string api, long elapsedTicks, HttpStatusCode expected)
    {
        var clock = new MutableTimeProvider(ApiFactory.Now);
        using var factory = new ApiFactory(timeProvider: clock);
        using var client = factory.CreateClient();
        var room = await CreateRoomAsync(client, api);
        using var created = await client.PostAsJsonAsync($"{api}/rooms/{room.Id}/bookings", Input());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = (await created.Content.ReadFromJsonAsync<BookingCreatedBody>())!;
        clock.Advance(TimeSpan.FromTicks(elapsedTicks));

        using var response = await client.PostAsync($"{api}/bookings/{booking.Id}/cancel", null);

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Conflict)
            await AssertConflictAsync(response, "CancellationWindowClosed");
        else
            Assert.Empty(await response.Content.ReadAsStringAsync());

        var detail = await client.GetFromJsonAsync<BookingBody>($"{api}/bookings/{booking.Id}");
        Assert.Equal(expected == HttpStatusCode.NoContent ? "Cancelled" : "Active", detail!.Status);
        await using var scope = factory.Services.CreateAsyncScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Bookings.AsNoTracking().SingleAsync();
        Assert.Equal(expected == HttpStatusCode.NoContent ? BookingStatus.Cancelled : BookingStatus.Active,
            persisted.Status);
    }

    [Fact]
    public async Task RejectedCancellation_StillOccupiesTheRoomAndCountsTowardDailyAllowance()
    {
        var clock = new MutableTimeProvider(ApiFactory.Now);
        using var factory = new ApiFactory(timeProvider: clock);
        using var client = factory.CreateClient();
        var first = await CreateRoomAsync(client, "/api");
        var second = await CreateRoomAsync(client, "/api");
        var third = await CreateRoomAsync(client, "/api");
        using var firstResponse = await client.PostAsJsonAsync($"/api/rooms/{first.Id}/bookings", Input());
        using var secondResponse = await client.PostAsJsonAsync($"/api/rooms/{second.Id}/bookings", Input());
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var booking = (await firstResponse.Content.ReadFromJsonAsync<BookingCreatedBody>())!;
        clock.Advance(TimeSpan.FromMinutes(1));

        using var cancel = await client.PostAsync($"/api/bookings/{booking.Id}/cancel", null);
        await AssertConflictAsync(cancel, "CancellationWindowClosed");

        using var overlap = await client.PostAsJsonAsync($"/api/rooms/{first.Id}/bookings",
            Input() with { BookedBy = "bogdan" });
        await AssertConflictAsync(overlap, "RoomOccupied");
        using var thirdBooking = await client.PostAsJsonAsync($"/api/rooms/{third.Id}/bookings", Input());
        await AssertConflictAsync(thirdBooking, "DailyBookingLimitReached");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.Bookings.CountAsync(b => b.Status == BookingStatus.Active));
        Assert.Equal(0, await db.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled));
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task MissingBooking_Remains404AfterTimeAdvances(string api)
    {
        var clock = new MutableTimeProvider(ApiFactory.Now);
        using var factory = new ApiFactory(timeProvider: clock);
        using var client = factory.CreateClient();
        clock.Advance(TimeSpan.FromDays(1));

        using var response = await client.PostAsync($"{api}/bookings/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("BookingNotFound", problem!.Title);
    }

    private static BookingInput Input() =>
        new("ana", ApiFactory.Now.AddHours(1), ApiFactory.Now.AddHours(2), 4);

    private static async Task<RoomBody> CreateRoomAsync(HttpClient client, string api)
    {
        using var response = await client.PostAsJsonAsync($"{api}/rooms", new { name = "Study room", capacity = 10 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoomBody>())!;
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response, string title)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal(title, problem.Title);
    }
}
