using System.Data;
using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        // SQLite uses BEGIN IMMEDIATE for Serializable: reserve the write before reading the quota.
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure(Error.Conflict(
                "ConcurrentChange", "The room or booking changed during this request. Refresh and try again."));
        }
    }
}
