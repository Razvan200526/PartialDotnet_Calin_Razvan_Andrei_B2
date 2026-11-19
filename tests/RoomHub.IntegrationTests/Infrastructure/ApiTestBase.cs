using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.IntegrationTests.Contracts;

namespace RoomHub.IntegrationTests.Infrastructure;

public abstract class ApiTestBase : IDisposable
{
    protected ApiFactory Factory { get; } = new();
    protected HttpClient Client { get; }

    protected ApiTestBase() => Client = Factory.CreateClient();

    protected async Task<RoomBody> CreateRoomAsync(string api = "/api")
    {
        using var response = await Client.PostAsJsonAsync($"{api}/rooms", new { name = "Study room", capacity = 10 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoomBody>())!;
    }

    protected static BookingInput Input(
        int startMinutes = 60, int endMinutes = 120, int attendees = 4, string? bookedBy = "ana") =>
        new(bookedBy, ApiFactory.Now.AddMinutes(startMinutes), ApiFactory.Now.AddMinutes(endMinutes), attendees);

    protected async Task<BookingCreatedBody> CreateBookingAsync(Guid roomId, string api = "/api")
    {
        using var response = await Client.PostAsJsonAsync($"{api}/rooms/{roomId}/bookings", Input());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookingCreatedBody>())!;
    }

    protected async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? title = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        if (title is not null)
            Assert.Equal(title, problem.Title);
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
