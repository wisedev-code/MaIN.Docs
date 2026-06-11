namespace MaIN.Docs.Api.Models;

public record SessionRequest(string TurnstileToken);

public record SessionResponse(string Token, DateTimeOffset ExpiresAt);
