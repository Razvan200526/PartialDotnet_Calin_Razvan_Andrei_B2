using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Rooms.GetRoom;

public static class GetRoomEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id, IQueryHandler<GetRoomQuery, GetRoomResponse> handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetRoomQuery(id), ct);
            return result.ToHttpResult(room => Results.Ok(room));
        });
    }
}
