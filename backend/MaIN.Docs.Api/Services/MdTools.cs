using System.Text.Json;

namespace MaIN.Docs.Api.Services;

public static class MdTools
{
    public static Func<string, Task<string>> SearchIn(string directory) =>
        async argsJson =>
        {
            var args = JsonSerializer.Deserialize<SearchArgs>(argsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            if (!Directory.Exists(directory))
                return JsonSerializer.Serialize(new { error = $"Directory not found: {directory}" });

            var files = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);
            var results = new List<object>();

            foreach (var file in files)
            {
                var lines = await File.ReadAllLinesAsync(file);
                var matches = new List<object>();

                for (var i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains(args.Query, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var start = Math.Max(0, i - 1);
                    var end = Math.Min(lines.Length - 1, i + 1);
                    matches.Add(new { lineNumber = i + 1, snippet = string.Join('\n', lines[start..(end + 1)]) });
                }

                if (matches.Count > 0)
                    results.Add(new { file = Path.GetFileName(file), path = file, matchCount = matches.Count, matches });
            }

            return JsonSerializer.Serialize(new
            {
                query = args.Query,
                totalFiles = files.Length,
                matchingFiles = results.Count,
                results
            });
        };

    public static Func<string, Task<string>> Read() =>
        async argsJson =>
        {
            var args = JsonSerializer.Deserialize<ReadArgs>(argsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            if (!args.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Serialize(new { error = "Only .md files are supported" });

            if (!File.Exists(args.Path))
                return JsonSerializer.Serialize(new { error = $"File not found: {args.Path}" });

            var content = await File.ReadAllTextAsync(args.Path);
            return JsonSerializer.Serialize(new
            {
                path = args.Path,
                fileName = Path.GetFileName(args.Path),
                content,
                lastModified = File.GetLastWriteTime(args.Path).ToString("yyyy-MM-dd HH:mm:ss")
            });
        };

    private record SearchArgs(string Query);
    private record ReadArgs(string Path);
}
