using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Common.Validation;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public sealed class CreateRoomRequestValidator : IRequestValidator<CreateRoomRequest>
{
    public ValueTask<Result> ValidateAsync(CreateRoomRequest request, HttpContext context, CancellationToken ct) =>
        ValueTask.FromResult(Room.Validate(request.Name, request.Capacity));
}
