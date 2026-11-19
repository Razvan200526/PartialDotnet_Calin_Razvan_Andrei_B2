using Asp.Versioning;
using RoomHub.Api.Features.Bookings;
using RoomHub.Api.Features.Rooms;

namespace RoomHub.Api.Extensions;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapGet("/", () => "RoomHub API is running.");

        var api = app.NewVersionedApi("RoomHub");
        var v1 = api.MapGroup("/api/v{version:apiVersion}").HasApiVersion(new ApiVersion(1, 0));
        v1.MapRoomsEndpoints();
        v1.MapBookingsEndpoints();

        // The assignment's original routes remain aliases of v1.
        var original = api.MapGroup("/api").HasApiVersion(new ApiVersion(1, 0));
        original.MapRoomsEndpoints();
        original.MapBookingsEndpoints();
        return app;
    }
}
