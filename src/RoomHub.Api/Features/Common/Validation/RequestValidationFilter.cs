using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Common.Validation;

public sealed class RequestValidationFilter<TRequest>(IRequestValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().SingleOrDefault();
        if (request is null)
            return Result.Failure(Error.Validation("InvalidRequest", "A request body is required.")).ToHttpResult();

        var result = await validator.ValidateAsync(request, context.HttpContext, context.HttpContext.RequestAborted);
        return result.IsSuccess ? await next(context) : result.ToHttpResult();
    }
}
