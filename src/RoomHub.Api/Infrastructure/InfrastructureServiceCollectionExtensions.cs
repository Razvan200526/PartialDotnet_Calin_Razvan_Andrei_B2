using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.Api.Infrastructure.Persistence.Repositories;

namespace RoomHub.Api.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connection = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("Default") ?? "Data Source=roomhub.db");

        if (!string.IsNullOrEmpty(connection.DataSource) && connection.DataSource != ":memory:"
            && !Path.IsPathRooted(connection.DataSource))
        {
            connection.DataSource = Path.GetFullPath(connection.DataSource, environment.ContentRootPath);
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection.ToString()));
        services.AddScoped<IRoomRepository, EfRoomRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
