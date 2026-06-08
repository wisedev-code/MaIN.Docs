namespace MaIN.Docs.Api.Services;

public static class IssueTools
{
    public record ListIssuesArgs;
    public record GetIssueArgs(int Number);
    public record ListRepoFilesArgs(string Path = "");
    public record ReadRepoFileArgs(string Path);
    public record ProposeIssueArgs(string Title, string Body, List<string> AdditionalLabels);
    public record CreateIssueArgs(string Title, string Body, List<string> AdditionalLabels);

    private static GitHubService _svc = null!;
    private static Action<(string Title, string Body)>? _proposalCapture;
    private static Action<string>? _urlCapture;

    public static void Init(GitHubService svc) => _svc = svc;
    public static void SetProposalCapture(Action<(string Title, string Body)>? capture) => _proposalCapture = capture;
    public static void SetUrlCapture(Action<string>? capture) => _urlCapture = capture;

    public static async Task<object> ListIssues(ListIssuesArgs _)
    {
        var issues = await _svc.ListIssuesAsync();
        return issues.Select(i => new
        {
            number = i.Number,
            title  = i.Title,
            url    = i.HtmlUrl,
            labels = i.Labels.Select(l => l.Name).ToList(),
        }).ToList();
    }

    public static async Task<object> GetIssue(GetIssueArgs args)
    {
        var issue = await _svc.GetIssueAsync(args.Number);
        return new
        {
            number = issue.Number,
            title  = issue.Title,
            body   = issue.Body,
            url    = issue.HtmlUrl,
            labels = issue.Labels.Select(l => l.Name).ToList(),
        };
    }

    public static async Task<object> ListRepoFiles(ListRepoFilesArgs args)
    {
        var listing = await _svc.ListRepoFilesAsync(args.Path);
        return new { path = args.Path, files = listing };
    }

    public static async Task<object> ReadRepoFile(ReadRepoFileArgs args)
    {
        var content = await _svc.ReadRepoFileAsync(args.Path);
        return new { path = args.Path, content };
    }

    public static Task<object> Propose(ProposeIssueArgs args)
    {
        _proposalCapture?.Invoke((args.Title, args.Body));
        return Task.FromResult<object>(new { proposed = true, title = args.Title });
    }

    public static async Task<object> Create(CreateIssueArgs args)
    {
        var notice = "> ⚠️ AI-proposed via MaIN.Docs. Auto-closed in 3 days unless a contributor engages.\n\n";
        var labels = new List<string>(args.AdditionalLabels) { "proposal" };
        var issue = await _svc.CreateIssueAsync(args.Title, notice + args.Body, labels);
        _urlCapture?.Invoke(issue.HtmlUrl);
        return new { number = issue.Number, url = issue.HtmlUrl, title = issue.Title };
    }
}
