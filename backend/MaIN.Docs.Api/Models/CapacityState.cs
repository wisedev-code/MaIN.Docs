namespace MaIN.Docs.Api.Models;

public record CapacityState(
    int Tier,
    long TokensInCurrentWindow,
    DateTime? Tier1ExhaustedAt,
    DateTime? Tier2ExhaustedAt,
    int OllamaKeyIndex = 0);
