using System.Diagnostics;
using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

public static class BuildTools
{
    public record DraftFileArgs(string Path, string Content, string Language = "");
    public record TryBuildArgs();

    private static readonly List<ProposedFile> _drafts = [];
    private static Action<(string Path, string Content, string Language)>? _showCallback;

    /// <summary>True once at least one file has been shown via draft_file this request.</summary>
    public static bool HasShownFiles { get; private set; }

    public static void SetShowCallback(Action<(string Path, string Content, string Language)>? callback)
    {
        _showCallback = callback;
        _drafts.Clear();
        HasShownFiles = false;
    }

    /// <summary>
    /// Shows a file immediately in the UI. Re-calling with the same path replaces the file
    /// (for fix iterations after user feedback).
    /// </summary>
    public static Task<object> DraftFile(DraftFileArgs args)
    {
        var file = new ProposedFile(args.Path, args.Content, args.Language);
        var idx = _drafts.FindIndex(d => d.Path.Equals(args.Path, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _drafts[idx] = file;
        else _drafts.Add(file);
        _showCallback?.Invoke((file.Path, file.Content, file.Language));
        HasShownFiles = true;
        return Task.FromResult<object>(new { shown = true, path = args.Path, totalShown = _drafts.Count });
    }

    /// <summary>
    /// Compiles all drafted files. On success, promotes every draft to the UI and clears
    /// the queue. On failure, keeps drafts intact so the agent can update individual files
    /// via draft_file and retry.
    /// </summary>
    public static async Task<object> TryBuild(TryBuildArgs _)
    {
        if (_drafts.Count == 0)
            return new { success = false, error = "No files drafted yet — call draft_file for every file first, then try_build." };

        var tempDir = Path.Combine(Path.GetTempPath(), $"main-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            foreach (var f in _drafts)
            {
                var fullPath = Path.Combine(tempDir, f.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, f.Content);
            }

            var csproj = Directory.GetFiles(tempDir, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (csproj is null)
                return new { success = false, error = "No .csproj found in drafts — add it via draft_file." };

            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("dotnet", "build")
            {
                WorkingDirectory = Path.GetDirectoryName(csproj)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            // If dotnet is not on PATH (e.g. aspnet runtime image), degrade gracefully:
            // promote drafts to the UI anyway so the user can still download the code.
            bool started;
            try { started = proc.Start(); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
                foreach (var f in _drafts)
                    _showCallback?.Invoke((f.Path, f.Content, f.Language));
                var shown = _drafts.Count;
                _drafts.Clear();
                HasShownFiles = true;
                return new { success = true, filesShown = shown, note = "dotnet SDK not available — build skipped, files shown unverified." };
            }
            if (!started)
                return new { success = false, error = "Failed to start dotnet process." };

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask  = proc.StandardError.ReadToEndAsync();
            await Task.WhenAny(proc.WaitForExitAsync(), Task.Delay(TimeSpan.FromMinutes(3)));

            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
                return new { success = false, error = "Build timed out after 3 minutes." };
            }

            var stdout = await stdoutTask;
            var stderr  = await stderrTask;

            if (proc.ExitCode == 0)
            {
                foreach (var f in _drafts)
                    _showCallback?.Invoke((f.Path, f.Content, f.Language));
                var shown = _drafts.Count;
                _drafts.Clear();
                HasShownFiles = true;
                return new { success = true, filesShown = shown };
            }

            // Filter to actionable error lines only (warnings excluded)
            var allOutput = stdout + "\n" + stderr;
            var errors = allOutput
                .Split('\n')
                .Where(l => l.Contains("): error "))
                .Take(30)
                .ToList();

            return new
            {
                success = false,
                errors = errors.Count > 0
                    ? string.Join("\n", errors)
                    : allOutput.Trim()[..Math.Min(2000, allOutput.Trim().Length)]
            };
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
