namespace RoomHub.Api.Features.Common.Validation;

public static class ValidationEndpointExtensions
{
    public static RouteHandlerBuilder ValidateRequest<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class => builder.AddEndpointFilter<RequestValidationFilter<TRequest>>();
}
