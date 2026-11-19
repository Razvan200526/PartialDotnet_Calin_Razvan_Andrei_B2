namespace RoomHub.Api.Domain.Common;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
