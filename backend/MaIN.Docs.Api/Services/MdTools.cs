using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MaIN.Docs.Api.Services;

public static class MdTools
{
    private static string _directory = "";
    private static ILogger _logger = NullLogger.Instance;
    private static Action<string, int>? _readCapture;
    private static HashSet<string>? _readPaths;

    public static void Initialize(string directory, ILogger logger)
    {
        _directory = directory;
        _logger    = logger;
    }

    // Invoked with (path, contentLength) so the orchestrator can fold tool-result size into the token estimate.
    public static void SetReadCapture(Action<string, int>? capture) => _readCapture = capture;

    // Tracks paths already read during this turn so a repeat read_md_file call for the same
    // file doesn't re-spend tokens/iteration budget on content the model already has.
    public static void SetReadDedup(HashSet<string>? readPaths) => _readPaths = readPaths;

    public record ListDocsArgs;
    public record SearchArgs(string Query);
    public record ReadArgs(string Path);

    public static Task<object> ListDocs(ListDocsArgs _)
    {
        _logger.LogInformation("[tool:list_docs] dir={Dir}", _directory);

        if (!Directory.Exists(_directory))
        {
            _logger.LogWarning("[tool:list_docs] Directory not found: {Dir}", _directory);
            return Task.FromResult<object>(new { error = $"Directory not found: {_directory}" });
        }

        var files = Directory.GetFiles(_directory, "*.md", SearchOption.AllDirectories)
            .Select(f => new
            {
                name     = Path.GetFileNameWithoutExtension(f),
                fileName = Path.GetFileName(f),
                path     = f,
                sizeKb   = Math.Round(new FileInfo(f).Length / 1024.0, 1)
            })
            .OrderBy(f => f.name)
            .ToList();

        _logger.LogInformation("[tool:list_docs] Found {Count} files", files.Count);
        return Task.FromResult<object>(new { totalFiles = files.Count, files });
    }

    public static async Task<object> Search(SearchArgs args)
    {
        _logger.LogInformation("[tool:search_md_files] query={Query}", args.Query);

        if (!Directory.Exists(_directory))
        {
            _logger.LogWarning("[tool:search_md_files] Directory not found: {Dir}", _directory);
            return new { error = $"Directory not found: {_directory}" };
        }

        var files   = Directory.GetFiles(_directory, "*.md", SearchOption.AllDirectories);
        var results = new List<object>();

        foreach (var file in files)
        {
            var lines   = await File.ReadAllLinesAsync(file);
            var matches = new List<object>();

            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(args.Query, StringComparison.OrdinalIgnoreCase)) continue;
                var start = Math.Max(0, i - 1);
                var end   = Math.Min(lines.Length - 1, i + 1);
                matches.Add(new { lineNumber = i + 1, snippet = string.Join('\n', lines[start..(end + 1)]) });
            }

            if (matches.Count > 0)
                results.Add(new { file = Path.GetFileName(file), path = file, matchCount = matches.Count, matches });
        }

        _logger.LogInformation("[tool:search_md_files] {Matching}/{Total} files matched", results.Count, files.Length);
        return new { query = args.Query, totalFiles = files.Length, matchingFiles = results.Count, results };
    }

    public static async Task<object> Read(ReadArgs args)
    {
        _logger.LogInformation("[tool:read_md_file] path={Path}", args.Path);

        if (!args.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[tool:read_md_file] Rejected non-.md path: {Path}", args.Path);
            return new { error = "Only .md files are supported" };
        }

        if (!File.Exists(args.Path))
        {
            _logger.LogWarning("[tool:read_md_file] File not found: {Path}", args.Path);
            return new { error = $"File not found: {args.Path}" };
        }

        if (_readPaths is not null && !_readPaths.Add(args.Path))
        {
            _logger.LogInformation("[tool:read_md_file] Skipped duplicate read of {File} — already read this turn", Path.GetFileName(args.Path));
            return new
            {
                path     = args.Path,
                fileName = Path.GetFileName(args.Path),
                note     = "Already read earlier in this turn — its full content is in an earlier tool result above. Do not request it again."
            };
        }

        var content = await File.ReadAllTextAsync(args.Path);
        _logger.LogInformation("[tool:read_md_file] Read {Bytes} chars from {File}", content.Length, Path.GetFileName(args.Path));
        _readCapture?.Invoke(args.Path, content.Length);

        return new
        {
            path         = args.Path,
            fileName     = Path.GetFileName(args.Path),
            content,
            lastModified = File.GetLastWriteTime(args.Path).ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
