namespace MaIN.Docs.Api.Models;

public record ModelSettings(
    Tier1ModelSettings Tier1,
    Tier2ModelSettings Tier2)
{
    public ModelSettings() : this(new Tier1ModelSettings(), new Tier2ModelSettings()) { }
}

public record Tier1ModelSettings(
    string Chatty = "gemini-3.1-flash-lite",
    string Code = "gemini-3.5-flash",
    string Review = "gemini-3.5-flash",
    string Design = "gemini-3.1-pro-preview",
    string Forge = "gemini-3.1-pro-preview")
{
    public Tier1ModelSettings() : this("gemini-3.1-flash-lite", "gemini-3.5-flash", "gemini-3.5-flash", "gemini-3.1-pro-preview", "gemini-3.1-pro-preview") { }
}

public record Tier2ModelSettings(
    string Chatty = "gemini-3.1-flash-lite",
    string Code = "gemini-3.1-flash-lite",
    string Review = "gemini-3.1-flash-lite",
    string Design = "gemini-3.1-flash-lite",
    string Forge = "gemini-3.1-flash-lite")
{
    public Tier2ModelSettings() : this("gemini-3.1-flash-lite", "gemini-3.1-flash-lite", "gemini-3.1-flash-lite", "gemini-3.1-flash-lite", "gemini-3.1-flash-lite") { }
}
