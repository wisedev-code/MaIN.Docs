using MaIN.Docs.Api.Services;

namespace MaIN.Docs.Api.Middleware;

public class SessionTokenMiddleware(RequestDelegate next, SessionTokenService tokenService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Options)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/api/session", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.Ordinal) ||
            !tokenService.TryValidate(header[prefix.Length..], out _))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }
}
