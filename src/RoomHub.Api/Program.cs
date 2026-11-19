using RoomHub.Api.Extensions;
using RoomHub.Api.Features.Bookings;
using RoomHub.Api.Features.Rooms;
using RoomHub.Api.Infrastructure;
using RoomHub.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddRoomsSlice();
builder.Services.AddBookingsSlice();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseApi();
await app.MigrateDatabaseAsync();
app.Run();

public partial class Program;
