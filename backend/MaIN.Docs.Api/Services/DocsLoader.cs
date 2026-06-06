namespace MaIN.Docs.Api.Services;

public class DocsLoader(IConfiguration config, ILogger<DocsLoader> logger)
{
    public string DocsPath { get; } = ResolveDocsPath(config, logger);

    private static string ResolveDocsPath(IConfiguration config, ILogger logger)
    {
        var configured = config["DocsContentPath"];
        var path = !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs-content"));

        if (!Directory.Exists(path))
            logger.LogWarning("docs-content directory not found at {Path}", path);

        return path;
    }
}
