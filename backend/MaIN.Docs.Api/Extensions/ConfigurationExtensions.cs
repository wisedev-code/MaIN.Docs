namespace MaIN.Docs.Api.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Reads the "AllowedOrigins" array, dropping empty entries left by unset
    /// indexed env vars (e.g. AllowedOrigins__1= with no value).
    /// </summary>
    public static string[] GetAllowedOrigins(this IConfiguration config) =>
        config.GetSection("AllowedOrigins").Get<string[]>()
            ?.Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray()
        ?? ["http://localhost:4200"];
}
