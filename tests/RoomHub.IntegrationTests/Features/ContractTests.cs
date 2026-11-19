using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoomHub.IntegrationTests.Contracts;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class ContractTests : ApiTestBase
{
    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task FullLifecycle_PreservesContractAndLocationPaths(string api)
    {
        using var createRoom = await Client.PostAsJsonAsync($"{api}/rooms", new { name = "  Study room  ", capacity = 10 });
        Assert.Equal(HttpStatusCode.Created, createRoom.StatusCode);
        var room = (await createRoom.Content.ReadFromJsonAsync<RoomBody>())!;
        Assert.Equal($"{api}/rooms/{room.Id}", createRoom.Headers.Location!.OriginalString);
        using var roomJson = JsonDocument.Parse(await createRoom.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "capacity", "id", "name" },
            roomJson.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("Study room", room.Name);
        using var getRoom = await Client.GetAsync(createRoom.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getRoom.StatusCode);
        Assert.Equal(room, await getRoom.Content.ReadFromJsonAsync<RoomBody>());

        using var createBooking = await Client.PostAsJsonAsync($"{api}/rooms/{room.Id}/bookings",
            Input(bookedBy: "  ana  "));
        Assert.Equal(HttpStatusCode.Created, createBooking.StatusCode);
        var booking = (await createBooking.Content.ReadFromJsonAsync<BookingCreatedBody>())!;
        Assert.Equal($"{api}/bookings/{booking.Id}", createBooking.Headers.Location!.OriginalString);
        Assert.Equal("Active", booking.Status);
        Assert.Equal(room.Id, booking.RoomId);
        using var createdJson = JsonDocument.Parse(await createBooking.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "id", "roomId", "status" },
            createdJson.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());

        using var getBooking = await Client.GetAsync(createBooking.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getBooking.StatusCode);
        using var json = JsonDocument.Parse(await getBooking.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "attendees", "bookedBy", "endUtc", "id", "roomId", "startUtc", "status" },
            json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.EndsWith("Z", json.RootElement.GetProperty("startUtc").GetString());
        var detail = (await getBooking.Content.ReadFromJsonAsync<BookingBody>())!;
        Assert.Equal("ana", detail.BookedBy);
        Assert.Equal(ApiFactory.Now.AddHours(1).UtcDateTime, detail.StartUtc);
        Assert.Equal(ApiFactory.Now.AddHours(2).UtcDateTime, detail.EndUtc);
        Assert.Equal(4, detail.Attendees);

        using var cancel = await Client.PostAsync($"{api}/bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Empty(await cancel.Content.ReadAsStringAsync());
        var cancelled = await Client.GetFromJsonAsync<BookingBody>($"{api}/bookings/{booking.Id}");
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    [Theory]
    [InlineData("/api/rooms/")]
    [InlineData("/api/bookings/")]
    [InlineData("/api/v1/rooms/")]
    [InlineData("/api/v1/bookings/")]
    public async Task MissingResources_ReturnProblemDetails(string route)
    {
        using var response = await Client.GetAsync(route + Guid.NewGuid());
        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("", 10)]
    [InlineData(" ", 10)]
    [InlineData("Room", 0)]
    [InlineData("Room", -1)]
    public async Task InvalidRoom_IsRejectedWithoutPersistence(string? name, int capacity)
    {
        using var response = await Client.PostAsJsonAsync("/api/rooms", new { name, capacity });
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(0, await WithDbAsync(db => db.Rooms.CountAsync()));
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task MissingRoom_PrecedesAllBookingBusinessValidation(string api)
    {
        using var response = await Client.PostAsJsonAsync($"{api}/rooms/{Guid.NewGuid()}/bookings", new { });
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "RoomNotFound");
        Assert.Equal(0, await WithDbAsync(db => db.Bookings.CountAsync()));
    }

    [Theory]
    [InlineData(null, 60, 120, 1)]
    [InlineData("", 60, 120, 1)]
    [InlineData("  ", 60, 120, 1)]
    [InlineData("ana", 60, 60, 1)]
    [InlineData("ana", 120, 60, 1)]
    [InlineData("ana", 60, 120, 0)]
    [InlineData("ana", 60, 120, -1)]
    [InlineData("ana", 60, 120, 11)]
    [InlineData("ana", -1, 60, 1)]
    [InlineData("ana", 60, 301, 1)]
    public async Task InvalidBooking_IsRejectedWithoutPersistence(
        string? bookedBy, int start, int end, int attendees)
    {
        var room = await CreateRoomAsync();
        using var response = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings",
            Input(start, end, attendees, bookedBy));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(0, await WithDbAsync(db => db.Bookings.CountAsync()));
        Assert.Equal(0, await WithDbAsync(db => db.Rooms.Select(r => r.ScheduleVersion).SingleAsync()));
    }

    [Fact]
    public async Task ExactTimeDurationAndCapacityBoundaries_AreAllowed()
    {
        var room = await CreateRoomAsync();
        using var response = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", Input(0, 240, 10));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task OffsetTimestamps_AreNormalizedToUtc()
    {
        var room = await CreateRoomAsync();
        var body = Input() with
        {
            StartUtc = ApiFactory.Now.AddHours(1).ToOffset(TimeSpan.FromHours(2)),
            EndUtc = ApiFactory.Now.AddHours(2).ToOffset(TimeSpan.FromHours(2))
        };
        using var response = await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await Client.GetFromJsonAsync<BookingBody>(response.Headers.Location);
        Assert.Equal(ApiFactory.Now.AddHours(1).UtcDateTime, detail!.StartUtc);
        Assert.Equal(DateTimeKind.Utc, detail.StartUtc.Kind);
    }

    [Theory]
    [InlineData("/api/rooms")]
    [InlineData("/api/v1/rooms")]
    public async Task MalformedJson_ReturnsProblemDetails(string route)
    {
        using var body = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync(route, body);
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnsupportedVersion_DoesNotReachTheHandler()
    {
        using var response = await Client.PostAsJsonAsync("/api/v2/rooms", new { name = "Room", capacity = 10 });
        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal(0, await WithDbAsync(db => db.Rooms.CountAsync()));
    }

    [Fact]
    public async Task InvalidGuidRoute_ReturnsProblemDetails()
    {
        using var response = await Client.GetAsync("/api/v1/rooms/not-a-guid");
        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
