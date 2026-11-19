using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Common;

public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default);
}

public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
