using RoomHub.Api.Features.Common;
using RoomHub.Api.Features.Common.Validation;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public static class CreateRoomEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateRoomRequest request, HttpRequest httpRequest,
            ICommandHandler<CreateRoomCommand, CreateRoomResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CreateRoomCommand(request.Name, request.Capacity), ct);
            return result.ToHttpResult(room =>
                Results.Created($"{httpRequest.GetApiBasePath()}/rooms/{room.Id}", room));
        }).ValidateRequest<CreateRoomRequest>();
    }
}
