using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoomHub.IntegrationTests.Contracts;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class BookingDurationTests : ApiTestBase
{
    [Theory]
    [InlineData("/api", 9, 3, 0, HttpStatusCode.Created)]
    [InlineData("/api/v1", 9, 3, 0, HttpStatusCode.Created)]
    [InlineData("/api", 10, 4, 0, HttpStatusCode.Created)]
    [InlineData("/api/v1", 10, 4, 0, HttpStatusCode.Created)]
    [InlineData("/api", 10, 4, 1, HttpStatusCode.BadRequest)]
    [InlineData("/api/v1", 10, 4, 1, HttpStatusCode.BadRequest)]
    [InlineData("/api", 11, 2, 0, HttpStatusCode.Created)]
    [InlineData("/api/v1", 11, 2, 0, HttpStatusCode.Created)]
    [InlineData("/api", 11, 2, 1, HttpStatusCode.BadRequest)]
    [InlineData("/api/v1", 11, 2, 1, HttpStatusCode.BadRequest)]
    [InlineData("/api", 20, 3, 0, HttpStatusCode.BadRequest)]
    [InlineData("/api/v1", 20, 3, 0, HttpStatusCode.BadRequest)]
    public async Task DurationLimit_UsesRoomCapacityEvenForOneAttendee(
        string api, int capacity, int hours, int extraTicks, HttpStatusCode expected)
    {
        using var roomResponse = await Client.PostAsJsonAsync($"{api}/rooms",
            new { name = "Study room", capacity });
        Assert.Equal(HttpStatusCode.Created, roomResponse.StatusCode);
        var room = (await roomResponse.Content.ReadFromJsonAsync<RoomBody>())!;
        var input = new BookingInput("ana", ApiFactory.Now,
            ApiFactory.Now.AddHours(hours).AddTicks(extraTicks), 1);

        using var response = await Client.PostAsJsonAsync($"{api}/rooms/{room.Id}/bookings", input);

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.BadRequest)
        {
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "BookingTooLong");
            Assert.Equal(0, await WithDbAsync(db => db.Bookings.CountAsync()));
            Assert.Equal(0, await WithDbAsync(db => db.Rooms.Select(r => r.ScheduleVersion).SingleAsync()));
        }
        else
        {
            Assert.Equal(1, await WithDbAsync(db => db.Bookings.CountAsync()));
        }
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task MissingRoom_StillTakesPrecedenceOverDurationValidation(string api)
    {
        var input = new BookingInput("ana", ApiFactory.Now, ApiFactory.Now.AddHours(5), 1);
        using var response = await Client.PostAsJsonAsync($"{api}/rooms/{Guid.NewGuid()}/bookings", input);
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "RoomNotFound");
        Assert.Equal(0, await WithDbAsync(db => db.Bookings.CountAsync()));
    }
}
