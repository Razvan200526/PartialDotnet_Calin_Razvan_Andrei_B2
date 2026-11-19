using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Common;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.IntegrationTests.Infrastructure;

namespace RoomHub.IntegrationTests.Features;

public sealed class RouteValidationTests : ApiTestBase
{
    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task InvalidRoom_IsRejectedBeforeHandlerDispatch(string api)
    {
        using var overridden = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICommandHandler<CreateRoomCommand, CreateRoomResponse>>();
            services.AddScoped<ICommandHandler<CreateRoomCommand, CreateRoomResponse>, UnexpectedCreateRoomHandler>();
        }));
        using var client = overridden.CreateClient();
        using var response = await client.PostAsJsonAsync($"{api}/rooms", new { name = "", capacity = 0 });
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "InvalidName");
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task BookingValidationAndMissingRoomPrecedence_RunBeforeHandlerDispatch(string api)
    {
        var room = await CreateRoomAsync(api);
        using var overridden = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICommandHandler<CreateBookingCommand, CreateBookingResponse>>();
            services.AddScoped<ICommandHandler<CreateBookingCommand, CreateBookingResponse>, UnexpectedCreateBookingHandler>();
        }));
        using var client = overridden.CreateClient();
        using var invalid = await client.PostAsJsonAsync($"{api}/rooms/{room.Id}/bookings", Input(bookedBy: ""));
        await AssertProblemAsync(invalid, HttpStatusCode.BadRequest, "InvalidBookedBy");
        using var missing = await client.PostAsJsonAsync($"{api}/rooms/{Guid.NewGuid()}/bookings", new { });
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "RoomNotFound");
    }
}
