using RoomHub.Api.Features.Common;
using RoomHub.Api.Features.Common.Validation;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;

namespace RoomHub.Api.Features.Rooms;

public static class RoomsSliceExtensions
{
    public static IServiceCollection AddRoomsSlice(this IServiceCollection services) => services
        .AddScoped<IRequestValidator<CreateRoomRequest>, CreateRoomRequestValidator>()
        .AddScoped<ICommandHandler<CreateRoomCommand, CreateRoomResponse>, CreateRoomHandler>()
        .AddScoped<IQueryHandler<GetRoomQuery, GetRoomResponse>, GetRoomHandler>();
}
