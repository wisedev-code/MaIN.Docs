using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

public static class PrTools
{
    // Args records
    public record ListBranchesArgs;
    public record ListPrsArgs;
    public record GetPrArgs(int Number);
    public record GetPrFilesArgs(int Number);
    public record ReadBranchFileArgs(string Path, string Branch);
    public record ProposePrReviewArgs(
        int PrNumber, string HeadSha, string Verdict, string Summary,
        List<ReviewCommentArg> Comments);
    public record ReviewCommentArg(string FilePath, int Line, string Body);
    public record SubmitPrReviewArgs(
        int PrNumber, string HeadSha, string Verdict, string Summary,
        List<ReviewCommentArg> Comments);
    public record CreatePrReviewArgs(
        int PrNumber, string HeadSha, string Verdict, string Summary,
        List<ReviewCommentArg> Comments);
    public record ProposeCodeChangeArgs(
        string Branch, string FilePath, string Content,
        string CommitMessage, string Rationale);
    public record PushFileArgs(
        string Branch, string FilePath, string Content, string CommitMessage);
    public record ProposePrArgs(
        string Title, string Body, string HeadBranch, string BaseBranch);
    public record CreatePrArgs(
        string Title, string Body, string HeadBranch, string BaseBranch);

    private static GitHubService _svc = null!;
    private static Action<PrReviewProposal>? _reviewCapture;
    private static Action<ReviewPosted>? _reviewPostedCapture;
    private static Action<CodeChangeProposal>? _codeChangeCapture;
    private static Action<PrProposal>? _prCapture;
    private static Action<string>? _prUrlCapture;

    public static void Init(GitHubService svc) => _svc = svc;
    public static void SetReviewCapture(Action<PrReviewProposal>? capture) => _reviewCapture = capture;
    public static void SetReviewPostedCapture(Action<ReviewPosted>? capture) => _reviewPostedCapture = capture;
    public static void SetCodeChangeCapture(Action<CodeChangeProposal>? capture) => _codeChangeCapture = capture;
    public static void SetPrCapture(Action<PrProposal>? capture) => _prCapture = capture;
    public static void SetPrUrlCapture(Action<string>? capture) => _prUrlCapture = capture;

    public static async Task<object> ListBranches(ListBranchesArgs _)
    {
        var result = await _svc.ListBranchesAsync();
        return new { branches = result };
    }

    public static async Task<object> ListPullRequests(ListPrsArgs _)
    {
        var result = await _svc.ListPullRequestsAsync();
        return new { pullRequests = result };
    }

    public static async Task<object> GetPullRequest(GetPrArgs args)
    {
        var pr = await _svc.GetPullRequestAsync(args.Number);
        return new
        {
            number = pr.Number,
            title = pr.Title,
            body = pr.Body,
            state = pr.State,
            url = pr.HtmlUrl,
            headBranch = pr.Head.Ref,
            headSha = pr.Head.Sha,
            baseBranch = pr.Base_.Ref,
        };
    }

    public static async Task<object> GetPrFiles(GetPrFilesArgs args)
    {
        var result = await _svc.GetPrFilesAsync(args.Number);
        return new { files = result };
    }

    public static async Task<object> ReadBranchFile(ReadBranchFileArgs args)
    {
        var content = await _svc.ReadRepoFileAsync(args.Path, args.Branch);
        return new { path = args.Path, branch = args.Branch, content };
    }

    public static Task<object> ProposePrReview(ProposePrReviewArgs args)
    {
        var proposal = new PrReviewProposal(
            args.PrNumber,
            args.Verdict,
            args.Summary,
            args.Comments.Count);
        _reviewCapture?.Invoke(proposal);
        return Task.FromResult<object>(new
        {
            proposed = true,
            prNumber = args.PrNumber,
            verdict = args.Verdict,
            commentCount = args.Comments.Count
        });
    }

    public static async Task<object> SubmitPrReview(SubmitPrReviewArgs args)
    {
        var comments = args.Comments
            .Select(c => new PrReviewComment(c.FilePath, c.Line, c.Body))
            .ToList();
        var url = await _svc.SubmitPrReviewAsync(
            args.PrNumber, args.HeadSha, args.Verdict, args.Summary, comments);
        return new { submitted = true, url };
    }

    public static async Task<object> CreatePrReview(CreatePrReviewArgs args)
    {
        var comments = args.Comments
            .Select(c => new PrReviewComment(c.FilePath, c.Line, c.Body))
            .ToList();
        try
        {
            var url = await _svc.SubmitPrReviewAsync(
                args.PrNumber, args.HeadSha, args.Verdict, args.Summary, comments);
            _reviewPostedCapture?.Invoke(new ReviewPosted(
                args.PrNumber, args.Verdict, args.Summary, args.Comments.Count, url));
            return new { submitted = true, url, commentCount = args.Comments.Count };
        }
        catch (HttpRequestException ex)
        {
            // Return the GitHub error body so the model can self-correct (e.g. fix line numbers)
            return new
            {
                submitted = false,
                error = ex.Message,
                hint = "Line numbers must refer to lines present in the PR diff. " +
                       "If a line is not in the diff, omit that comment or adjust the line number. " +
                       "You can retry create_pr_review with corrected arguments."
            };
        }
    }

    public static Task<object> ProposeCodeChange(ProposeCodeChangeArgs args)
    {
        var preview = args.Content.Length > 500
            ? args.Content[..500] + "\n..."
            : args.Content;
        var proposal = new CodeChangeProposal(
            args.Branch, args.FilePath, args.CommitMessage, args.Rationale, preview);
        _codeChangeCapture?.Invoke(proposal);
        return Task.FromResult<object>(new
        {
            proposed = true,
            branch = args.Branch,
            filePath = args.FilePath,
            commitMessage = args.CommitMessage
        });
    }

    public static async Task<object> PushFileToBranch(PushFileArgs args)
    {
        await _svc.PushFileAsync(args.FilePath, args.Content, args.CommitMessage, args.Branch);
        return new { pushed = true, branch = args.Branch, filePath = args.FilePath };
    }

    public static Task<object> ProposePullRequest(ProposePrArgs args)
    {
        var proposal = new PrProposal(args.Title, args.Body, args.HeadBranch, args.BaseBranch);
        _prCapture?.Invoke(proposal);
        return Task.FromResult<object>(new
        {
            proposed = true,
            title = args.Title,
            headBranch = args.HeadBranch,
            baseBranch = args.BaseBranch
        });
    }

    public static async Task<object> CreatePullRequest(CreatePrArgs args)
    {
        var pr = await _svc.CreatePullRequestAsync(
            args.Title, args.Body, args.HeadBranch, args.BaseBranch);
        _prUrlCapture?.Invoke(pr.HtmlUrl);
        return new { created = true, number = pr.Number, url = pr.HtmlUrl };
    }
}
