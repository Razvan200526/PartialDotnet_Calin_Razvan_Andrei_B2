namespace RoomHub.Api.Features.Bookings;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bookings").WithTags("Bookings");
        GetBooking.GetBookingEndpoint.Map(group);
        CancelBooking.CancelBookingEndpoint.Map(group);
        return endpoints;
    }
}
