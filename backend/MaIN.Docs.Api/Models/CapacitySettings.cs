namespace MaIN.Docs.Api.Models;

public record CapacitySettings(
    Tier1Settings Tier1,
    Tier2Settings Tier2,
    Tier3Settings Tier3)
{
    public CapacitySettings() : this(new Tier1Settings(), new Tier2Settings(), new Tier3Settings()) { }
}

public record Tier1Settings(long TokenLimit = 150_000, int CooldownMinutes = 180)
{
    public Tier1Settings() : this(150_000, 180) { }
}

public record Tier2Settings(string GeminiKey = "", long TokenLimit = 1_000_000, int CooldownMinutes = 120)
{
    public Tier2Settings() : this("", 1_000_000, 120) { }
}

public record Tier3Settings(string OllamaKey = "", string OllamaModel = "gemma4:31b-cloud")
{
    public Tier3Settings() : this("", "gemma4:31b-cloud") { }
}
