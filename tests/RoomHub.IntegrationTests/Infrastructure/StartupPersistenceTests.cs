using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.IntegrationTests.Contracts;

namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class StartupPersistenceTests
{
    [Fact]
    public async Task Startup_CreatesMigratedDatabaseWithMatchingModel()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.True(File.Exists(factory.DatabasePath));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task RecreatingApplication_PreservesRoomsBookingsAvailabilityAndCancellation()
    {
        var directory = Directory.CreateTempSubdirectory("roomhub-restart-");
        var database = Path.Combine(directory.FullName, "roomhub.db");
        Guid roomId;
        Guid bookingId;
        var input = new BookingInput("ana", ApiFactory.Now.AddHours(1), ApiFactory.Now.AddHours(2), 4);
        try
        {
            using (var first = new ApiFactory(database))
            using (var client = first.CreateClient())
            {
                using var roomResponse = await client.PostAsJsonAsync("/api/rooms", new { name = "Room", capacity = 10 });
                Assert.Equal(HttpStatusCode.Created, roomResponse.StatusCode);
                roomId = (await roomResponse.Content.ReadFromJsonAsync<RoomBody>())!.Id;
                using var bookingResponse = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", input);
                Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
                bookingId = (await bookingResponse.Content.ReadFromJsonAsync<BookingCreatedBody>())!.Id;
            }
            using (var second = new ApiFactory(database))
            using (var client = second.CreateClient())
            {
                var room = await client.GetFromJsonAsync<RoomBody>($"/api/rooms/{roomId}");
                Assert.Equal("Room", room!.Name);
                var booking = await client.GetFromJsonAsync<BookingBody>($"/api/bookings/{bookingId}");
                Assert.Equal("Active", booking!.Status);
                Assert.Equal(DateTimeKind.Utc, booking.StartUtc.Kind);
                Assert.Equal(input.StartUtc.UtcDateTime, booking.StartUtc);
                using var overlap = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", input);
                Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
                using var cancel = await client.PostAsync($"/api/bookings/{bookingId}/cancel", null);
                Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
            }
            using (var third = new ApiFactory(database))
            using (var client = third.CreateClient())
            {
                var booking = await client.GetFromJsonAsync<BookingBody>($"/api/v1/bookings/{bookingId}");
                Assert.Equal("Cancelled", booking!.Status);
                using var replacement = await client.PostAsJsonAsync($"/api/v1/rooms/{roomId}/bookings", input);
                Assert.Equal(HttpStatusCode.Created, replacement.StatusCode);
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
