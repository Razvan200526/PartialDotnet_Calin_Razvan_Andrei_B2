using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class UnitOfWorkTests : ApiTestBase
{
    [Fact]
    public async Task RepositoryAdd_DoesNotPersistUntilUnitOfWorkCommits()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
        rooms.Add(Room.Create("Staged room", 10).Value!);
        Assert.Equal(0, await WithDbAsync(db => db.Rooms.CountAsync()));
        var saved = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.True(saved.IsSuccess);
        Assert.Equal(1, await WithDbAsync(db => db.Rooms.CountAsync()));
    }

    [Fact]
    public async Task StaleScheduleWrite_ReturnsConflictAndRollsBackBookingInsert()
    {
        var room = await CreateRoomAsync();
        await using var first = Factory.Services.CreateAsyncScope();
        await using var second = Factory.Services.CreateAsyncScope();
        var firstRoom = await first.ServiceProvider.GetRequiredService<IRoomRepository>().GetByIdAsync(room.Id);
        var secondRoom = await second.ServiceProvider.GetRequiredService<IRoomRepository>().GetByIdAsync(room.Id);
        var now = ApiFactory.Now.UtcDateTime;
        var firstBooking = firstRoom!.Book("ana", now, now.AddHours(1), 4, now);
        var secondBooking = secondRoom!.Book("bogdan", now, now.AddHours(1), 4, now);
        Assert.True(firstBooking.IsSuccess);
        Assert.True(secondBooking.IsSuccess);
        first.ServiceProvider.GetRequiredService<IBookingRepository>().Add(firstBooking.Value!);
        second.ServiceProvider.GetRequiredService<IBookingRepository>().Add(secondBooking.Value!);
        Assert.True((await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()).IsSuccess);
        var rejected = await second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, rejected.Error!.Type);
        Assert.Equal(1, await WithDbAsync(db => db.Bookings.CountAsync()));
        Assert.Equal(1, await WithDbAsync(db => db.Rooms.Select(r => r.ScheduleVersion).SingleAsync()));
    }

    [Fact]
    public async Task StaleCancellationWrite_ReturnsConflict()
    {
        var room = await CreateRoomAsync();
        var created = await CreateBookingAsync(room.Id);
        await using var first = Factory.Services.CreateAsyncScope();
        await using var second = Factory.Services.CreateAsyncScope();
        var firstBooking = await first.ServiceProvider.GetRequiredService<IBookingRepository>().GetByIdAsync(created.Id);
        var secondBooking = await second.ServiceProvider.GetRequiredService<IBookingRepository>().GetByIdAsync(created.Id);
        Assert.True(firstBooking!.Cancel(ApiFactory.Now.UtcDateTime).IsSuccess);
        Assert.True(secondBooking!.Cancel(ApiFactory.Now.UtcDateTime).IsSuccess);
        Assert.True((await first.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()).IsSuccess);
        var rejected = await second.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, rejected.Error!.Type);
        Assert.Equal(BookingStatus.Cancelled,
            await WithDbAsync(db => db.Bookings.Select(b => b.Status).SingleAsync()));
    }
}
