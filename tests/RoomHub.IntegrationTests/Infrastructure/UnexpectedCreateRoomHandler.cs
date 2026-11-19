using RoomHub.Api.Domain.Common;
using RoomHub.Api.Features.Common;
using RoomHub.Api.Features.Rooms.CreateRoom;

namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class UnexpectedCreateRoomHandler : ICommandHandler<CreateRoomCommand, CreateRoomResponse>
{
    public Task<Result<CreateRoomResponse>> HandleAsync(CreateRoomCommand command, CancellationToken ct = default) =>
        throw new InvalidOperationException("Invalid route input must not reach this handler.");
}
