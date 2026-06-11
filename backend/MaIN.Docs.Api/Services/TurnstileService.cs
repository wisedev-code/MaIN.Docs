using System.Text.Json.Serialization;

namespace MaIN.Docs.Api.Services;

public class TurnstileService(HttpClient httpClient, IConfiguration config, ILogger<TurnstileService> logger)
{
    private readonly string _secretKey = config["Turnstile:SecretKey"] ?? "";

    // Verifies a Turnstile response token with Cloudflare's siteverify endpoint.
    // If no secret key is configured (local dev without a Cloudflare account),
    // verification is skipped entirely and this always returns true.
    public async Task<bool> VerifyAsync(string turnstileToken, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_secretKey))
            return true;

        var fields = new Dictionary<string, string>
        {
            ["secret"] = _secretKey,
            ["response"] = turnstileToken
        };
        if (!string.IsNullOrEmpty(remoteIp))
            fields["remoteip"] = remoteIp;

        try
        {
            using var response = await httpClient.PostAsync(
                "turnstile/v0/siteverify",
                new FormUrlEncodedContent(fields),
                ct);

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(ct);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Turnstile] Verification request failed — failing closed");
            return false;
        }
    }

    private record TurnstileVerifyResponse([property: JsonPropertyName("success")] bool Success);
}
