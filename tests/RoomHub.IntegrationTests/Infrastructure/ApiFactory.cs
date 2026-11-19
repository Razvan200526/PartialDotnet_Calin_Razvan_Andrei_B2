using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static DateTimeOffset Now { get; } = new(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);

    private readonly string? ownedDirectory;
    public string DatabasePath { get; }

    public ApiFactory(string? databasePath = null)
    {
        if (databasePath is null)
        {
            ownedDirectory = Directory.CreateTempSubdirectory("roomhub-tests-").FullName;
            databasePath = Path.Combine(ownedDirectory, "roomhub.db");
        }
        DatabasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath};Pooling=False");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && ownedDirectory is not null && Directory.Exists(ownedDirectory))
            Directory.Delete(ownedDirectory, recursive: true);
    }
}
