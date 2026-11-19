using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public static class CreateRoomEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateRoomRequest request,
            ICommandHandler<CreateRoomCommand, CreateRoomResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CreateRoomCommand(request.Name, request.Capacity), ct);
            return result.ToHttpResult(room => Results.Created($"/api/rooms/{room.Id}", room));
        }).WithName("CreateRoom");
    }
}
