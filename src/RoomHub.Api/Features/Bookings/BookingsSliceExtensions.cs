using RoomHub.Api.Features.Bookings.CancelBooking;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Common;

namespace RoomHub.Api.Features.Bookings;

public static class BookingsSliceExtensions
{
    public static IServiceCollection AddBookingsSlice(this IServiceCollection services) => services
        .AddScoped<ICommandHandler<CreateBookingCommand, CreateBookingResponse>, CreateBookingHandler>()
        .AddScoped<IQueryHandler<GetBookingQuery, GetBookingResponse>, GetBookingHandler>()
        .AddScoped<ICommandHandler<CancelBookingCommand, Unit>, CancelBookingHandler>();
}
