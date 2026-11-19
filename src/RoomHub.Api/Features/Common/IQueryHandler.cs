using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Common;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct = default);
}
