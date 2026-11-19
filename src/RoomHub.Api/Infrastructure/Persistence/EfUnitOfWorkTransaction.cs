using Microsoft.EntityFrameworkCore.Storage;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Infrastructure.Persistence;

public sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
