using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaIN.Docs.Api.Services;

public class SessionTokenService
{
    // Only used when SessionSecret is unset in Development, so `dotnet run` works
    // with no env var. Production must set SessionSecret (enforced at startup).
    private const string DevFallbackSecret = "dev-only-insecure-session-secret-do-not-use-in-prod";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly byte[] _secret;

    public SessionTokenService(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["SessionSecret"];
        var secret = string.IsNullOrEmpty(configured) && env.IsDevelopment()
            ? DevFallbackSecret
            : configured ?? "";
        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public (string Token, DateTimeOffset ExpiresAt) IssueToken(TimeSpan lifetime)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new SessionTokenPayload(expiresAt.ToUnixTimeSeconds()), JsonOptions);
        var signature = HMACSHA256.HashData(_secret, payloadBytes);

        var token = $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
        return (token, expiresAt);
    }

    public bool TryValidate(string token, out DateTimeOffset expiresAt)
    {
        expiresAt = default;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            var payloadBytes = Base64UrlDecode(parts[0]);
            var providedSignature = Base64UrlDecode(parts[1]);

            var expectedSignature = HMACSHA256.HashData(_secret, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
                return false;

            var payload = JsonSerializer.Deserialize<SessionTokenPayload>(payloadBytes, JsonOptions);
            if (payload is null)
                return false;

            expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Exp);
            return expiresAt > DateTimeOffset.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    private record SessionTokenPayload([property: JsonPropertyName("exp")] long Exp);
}
