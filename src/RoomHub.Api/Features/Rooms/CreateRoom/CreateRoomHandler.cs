using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public sealed class CreateRoomHandler(IRoomRepository rooms, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRoomCommand, CreateRoomResponse>
{
    public async Task<Result<CreateRoomResponse>> HandleAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        var result = Room.Create(command.Name, command.Capacity);
        if (!result.IsSuccess)
            return Result<CreateRoomResponse>.Failure(result.Error!);

        rooms.Add(result.Value!);
        var saved = await unitOfWork.SaveChangesAsync(ct);
        return saved.IsSuccess
            ? Result<CreateRoomResponse>.Success(CreateRoomResponse.From(result.Value!))
            : Result<CreateRoomResponse>.Failure(saved.Error!);
    }
}
