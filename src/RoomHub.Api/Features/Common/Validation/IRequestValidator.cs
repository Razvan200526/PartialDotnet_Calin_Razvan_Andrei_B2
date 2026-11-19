using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Common.Validation;

public interface IRequestValidator<in TRequest> where TRequest : class
{
    ValueTask<Result> ValidateAsync(TRequest request, HttpContext context, CancellationToken ct);
}
