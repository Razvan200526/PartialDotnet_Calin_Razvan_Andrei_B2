namespace RoomHub.IntegrationTests.Infrastructure;

public sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset current = utcNow;

    public override DateTimeOffset GetUtcNow() => current;

    public void Advance(TimeSpan duration) => current = current.Add(duration);
}
