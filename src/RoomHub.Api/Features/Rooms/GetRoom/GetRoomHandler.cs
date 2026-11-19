using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Rooms.GetRoom;

public sealed class GetRoomHandler(IRoomRepository rooms) : IQueryHandler<GetRoomQuery, GetRoomResponse>
{
    public async Task<Result<GetRoomResponse>> HandleAsync(GetRoomQuery query, CancellationToken ct = default)
    {
        var room = await rooms.GetByIdAsync(query.Id, ct);
        return room is null
            ? Result<GetRoomResponse>.Failure(Error.NotFound("RoomNotFound", "The room does not exist."))
            : Result<GetRoomResponse>.Success(GetRoomResponse.From(room));
    }
}
