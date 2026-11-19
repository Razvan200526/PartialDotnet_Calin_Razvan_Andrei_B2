using RoomHub.Api.Domain.Common;

namespace RoomHub.UnitTests.Fakes;

public sealed class FakeUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    public int CommitCalls { get; private set; }
    public bool IsDisposed { get; private set; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        CommitCalls++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
