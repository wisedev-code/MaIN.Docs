using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaIN.Docs.Api.Services;

public record GitHubIssue(
    int Number,
    string Title,
    string Body,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    IReadOnlyList<GitHubLabel> Labels,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public record GitHubLabel(string Name);

public record GitHubContentEntry(
    string Name,
    string Path,
    string Type,
    string? Content,
    string? Sha,
    string? Encoding);

public record GitHubBranch(string Name, GitHubBranchCommit Commit);
public record GitHubBranchCommit(string Sha);

public record GitHubPr(
    int Number,
    string Title,
    string Body,
    string State,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    GitHubPrRef Head,
    [property: JsonPropertyName("base")] GitHubPrRef Base_);

public record GitHubPrRef(string Ref, string Sha);

public record GitHubPrFile(
    string Filename,
    string Status,
    int Additions,
    int Deletions,
    string? Patch);

public record GitHubPrCreated(
    int Number,
    [property: JsonPropertyName("html_url")] string HtmlUrl);

public record PrReviewComment(string Path, int Line, string Body);

public class GitHubService(HttpClient http, IConfiguration config, ILogger<GitHubService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private string Repo => config["GITHUB_REPO"] ?? "wisedev-code/MaIN.NET";

    public async Task<List<GitHubIssue>> ListIssuesAsync(string? label = null)
    {
        var url = $"/repos/{Repo}/issues?state=open&per_page=30";
        if (label is not null) url += $"&labels={Uri.EscapeDataString(label)}";
        var json = await http.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<GitHubIssue>>(json, JsonOpts) ?? [];
    }

    public async Task<GitHubIssue> GetIssueAsync(int number)
    {
        var json = await http.GetStringAsync($"/repos/{Repo}/issues/{number}");
        return JsonSerializer.Deserialize<GitHubIssue>(json, JsonOpts)!;
    }

    public async Task<GitHubIssue> CreateIssueAsync(string title, string body, List<string> labels)
    {
        var payload = JsonSerializer.Serialize(new { title, body, labels });
        var response = await http.PostAsync($"/repos/{Repo}/issues",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubIssue>(json, JsonOpts)!;
    }

    public async Task CloseWithCommentAsync(int number, string comment)
    {
        var commentPayload = JsonSerializer.Serialize(new { body = comment });
        await http.PostAsync($"/repos/{Repo}/issues/{number}/comments",
            new StringContent(commentPayload, Encoding.UTF8, "application/json"));

        var closePayload = JsonSerializer.Serialize(new { state = "closed" });
        await http.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/repos/{Repo}/issues/{number}")
        {
            Content = new StringContent(closePayload, Encoding.UTF8, "application/json")
        });

        logger.LogInformation("Auto-closed GitHub issue #{Number}", number);
    }

    public async Task<string> ListRepoFilesAsync(string path = "")
    {
        var url = string.IsNullOrEmpty(path)
            ? $"/repos/{Repo}/contents"
            : $"/repos/{Repo}/contents/{path.TrimStart('/')}";

        var json = await http.GetStringAsync(url);
        var entries = JsonSerializer.Deserialize<List<GitHubContentEntry>>(json, JsonOpts) ?? [];

        var sb = new StringBuilder();
        foreach (var e in entries)
            sb.AppendLine($"{(e.Type == "dir" ? "[dir]" : "[file]")} {e.Path}");
        return sb.ToString();
    }

    public async Task<string> ReadRepoFileAsync(string path, string? branch = null)
    {
        var url = $"/repos/{Repo}/contents/{path.TrimStart('/')}";
        if (branch is not null) url += $"?ref={Uri.EscapeDataString(branch)}";
        var json = await http.GetStringAsync(url);
        var entry = JsonSerializer.Deserialize<GitHubContentEntry>(json, JsonOpts)!;

        if (entry.Encoding == "base64" && entry.Content is not null)
            return Encoding.UTF8.GetString(Convert.FromBase64String(entry.Content.Replace("\n", "")));

        return entry.Content ?? "(empty)";
    }

    public async Task<string> CreateBranchAsync(string branchName, string baseBranch)
    {
        var refJson = await http.GetStringAsync($"/repos/{Repo}/git/ref/heads/{Uri.EscapeDataString(baseBranch)}");
        using var refDoc = JsonDocument.Parse(refJson);
        var sha = refDoc.RootElement.GetProperty("object").GetProperty("sha").GetString()!;

        var payload = JsonSerializer.Serialize(new { @ref = $"refs/heads/{branchName}", sha });
        var response = await http.PostAsync(
            $"/repos/{Repo}/git/refs",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Created branch {Branch} from {Base} ({Sha})", branchName, baseBranch, sha[..7]);
        return branchName;
    }

    public async Task<string> ListBranchesAsync()
    {
        var json = await http.GetStringAsync($"/repos/{Repo}/branches?per_page=100");
        var branches = JsonSerializer.Deserialize<List<GitHubBranch>>(json, JsonOpts) ?? [];
        return string.Join("\n", branches.Select(b => $"{b.Name} (sha: {b.Commit.Sha[..7]})"));
    }

    public async Task<string> ListPullRequestsAsync()
    {
        var json = await http.GetStringAsync($"/repos/{Repo}/pulls?state=open&per_page=30");
        var prs = JsonSerializer.Deserialize<List<GitHubPr>>(json, JsonOpts) ?? [];
        if (prs.Count == 0) return "No open pull requests.";
        return string.Join("\n", prs.Select(p => $"#{p.Number}: {p.Title} ({p.Head.Ref} → {p.Base_.Ref})"));
    }

    public async Task<GitHubPr> GetPullRequestAsync(int number)
    {
        var json = await http.GetStringAsync($"/repos/{Repo}/pulls/{number}");
        return JsonSerializer.Deserialize<GitHubPr>(json, JsonOpts)!;
    }

    public async Task<string> GetPrFilesAsync(int number)
    {
        var json = await http.GetStringAsync($"/repos/{Repo}/pulls/{number}/files?per_page=100");
        var files = JsonSerializer.Deserialize<List<GitHubPrFile>>(json, JsonOpts) ?? [];
        var sb = new StringBuilder();
        foreach (var f in files)
        {
            sb.AppendLine($"--- {f.Filename} ({f.Status}, +{f.Additions}/-{f.Deletions}) ---");
            if (!string.IsNullOrEmpty(f.Patch)) sb.AppendLine(f.Patch);
        }
        return sb.ToString();
    }

    public async Task<string> SubmitPrReviewAsync(
        int prNumber, string headSha, string reviewEvent, string body, List<PrReviewComment> comments)
    {
        // Try with inline comments first; fall back to body-only if GitHub rejects line numbers
        // (lines must be part of the diff context — not all file lines are valid targets)
        var withComments = JsonSerializer.Serialize(new
        {
            commit_id = headSha,
            body,
            @event = reviewEvent,
            comments = comments.Select(c => new
            {
                path = c.Path,
                line = c.Line,
                side = "RIGHT",
                body = c.Body
            })
        });

        var response = await http.PostAsync(
            $"/repos/{Repo}/pulls/{prNumber}/reviews",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("GitHub PR review failed {Status}: {Body}", (int)response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"GitHub returned {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var url = doc.RootElement.GetProperty("html_url").GetString() ?? string.Empty;
        logger.LogInformation("Submitted PR review for #{PrNumber}, event={Event}", prNumber, reviewEvent);
        return url;
    }

    public async Task EnsureBranchExistsAsync(string branch)
    {
        var response = await http.GetAsync($"/repos/{Repo}/branches/{Uri.EscapeDataString(branch)}");
        if (response.IsSuccessStatusCode) return;

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Branch {Branch} not found, creating from main...", branch);
            
            // Get SHA of main branch
            var mainBranchJson = await http.GetStringAsync($"/repos/{Repo}/branches/main");
            var mainBranch = JsonSerializer.Deserialize<GitHubBranch>(mainBranchJson, JsonOpts)!;
            
            var payload = JsonSerializer.Serialize(new 
            { 
                @ref = $"refs/heads/{branch}", 
                sha = mainBranch.Commit.Sha 
            });
            
            var createResponse = await http.PostAsync(
                $"/repos/{Repo}/git/refs",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            
            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create branch {branch}: {error}");
            }
            
            logger.LogInformation("Created branch {Branch} from main", branch);
        }
    }

    public async Task PushFileAsync(string filePath, string content, string commitMessage, string branch)
    {
        await EnsureBranchExistsAsync(branch);

        var cleanPath = filePath.TrimStart('/');
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        // Fetch existing SHA if file exists (required for updates)
        string? sha = null;
        try
        {
            var existingJson = await http.GetStringAsync(
                $"/repos/{Repo}/contents/{cleanPath}?ref={Uri.EscapeDataString(branch)}");
            var existing = JsonSerializer.Deserialize<GitHubContentEntry>(existingJson, JsonOpts);
            sha = existing?.Sha;
        }
        catch { /* file doesn't exist yet — create mode */ }

        object requestBody = sha is not null
            ? new { message = commitMessage, content = base64Content, branch, sha }
            : new { message = commitMessage, content = base64Content, branch };

        var payload = JsonSerializer.Serialize(requestBody);
        var response = await http.PutAsync(
            $"/repos/{Repo}/contents/{cleanPath}",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Pushed file {Path} to branch {Branch}", filePath, branch);
    }

    public async Task<GitHubPrCreated> CreatePullRequestAsync(
        string title, string body, string head, string base_)
    {
        var payload = JsonSerializer.Serialize(new { title, body, head, @base = base_ });
        var response = await http.PostAsync(
            $"/repos/{Repo}/pulls",
            new StringContent(payload, Encoding.UTF8, "application/json"));
            
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"GitHub API Error ({response.StatusCode}): {errorJson}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubPrCreated>(json, JsonOpts)!;
    }
}
