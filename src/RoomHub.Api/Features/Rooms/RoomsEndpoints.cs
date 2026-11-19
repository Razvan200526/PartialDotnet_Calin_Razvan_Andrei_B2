namespace RoomHub.Api.Features.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/rooms").WithTags("Rooms");
        CreateRoom.CreateRoomEndpoint.Map(group);
        GetRoom.GetRoomEndpoint.Map(group);
        Bookings.CreateBooking.CreateBookingEndpoint.Map(group);
        return endpoints;
    }
}
