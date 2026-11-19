namespace RoomHub.Api.Domain.Common;

public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
