using RoomHub.Api.Domain.Common;

namespace RoomHub.UnitTests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public Result SaveResult { get; set; } = Result.Success();
    public FakeUnitOfWorkTransaction Transaction { get; } = new();

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        Task.FromResult<IUnitOfWorkTransaction>(Transaction);

    public Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        LastToken = ct;
        return Task.FromResult(SaveResult);
    }
}
