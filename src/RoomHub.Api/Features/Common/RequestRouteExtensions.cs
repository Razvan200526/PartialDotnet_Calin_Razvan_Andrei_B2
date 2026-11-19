namespace RoomHub.Api.Features.Common;

public static class RequestRouteExtensions
{
    public static string GetApiBasePath(this HttpRequest request)
    {
        var version = request.RouteValues["version"];
        return version is null ? $"{request.PathBase}/api" : $"{request.PathBase}/api/v{version}";
    }
}
