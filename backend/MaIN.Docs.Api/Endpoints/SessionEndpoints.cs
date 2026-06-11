using MaIN.Docs.Api.Extensions;
using MaIN.Docs.Api.Models;
using MaIN.Docs.Api.Services;

namespace MaIN.Docs.Api.Endpoints;

public static class SessionEndpoints
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/session", HandleCreateSession)
           .RequireRateLimiting("session");
    }

    private static async Task<IResult> HandleCreateSession(
        HttpContext context,
        SessionRequest request,
        SessionTokenService tokenService,
        TurnstileService turnstile,
        IConfiguration config,
        CancellationToken ct)
    {
        var origin = context.Request.Headers.Origin.ToString();
        var allowedOrigins = config.GetAllowedOrigins();
        if (string.IsNullOrEmpty(origin) || !allowedOrigins.Contains(origin))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        if (string.IsNullOrEmpty(request.TurnstileToken))
            return Results.BadRequest("turnstileToken is required.");

        var verified = await turnstile.VerifyAsync(request.TurnstileToken, context.Connection.RemoteIpAddress?.ToString(), ct);
        if (!verified)
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var (token, expiresAt) = tokenService.IssueToken(TokenLifetime);
        return Results.Ok(new SessionResponse(token, expiresAt));
    }
}
