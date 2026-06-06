namespace MaIN.Docs.Api.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string _expected = config["ApiKey"] ?? "";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Options)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var provided) || provided != _expected)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }
}
