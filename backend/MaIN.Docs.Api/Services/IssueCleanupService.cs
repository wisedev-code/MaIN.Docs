namespace MaIN.Docs.Api.Services;

public class IssueCleanupService(GitHubService github, ILogger<IssueCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), ct);
            await RunCleanupAsync(ct);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            var issues = await github.ListIssuesAsync(label: "proposal");
            var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
            var expired = issues.Where(i => i.CreatedAt < cutoff).ToList();

            logger.LogInformation("Issue cleanup: {Total} proposal issues, {Expired} expired", issues.Count, expired.Count);

            foreach (var issue in expired)
            {
                if (ct.IsCancellationRequested) break;
                await github.CloseWithCommentAsync(issue.Number,
                    "⏱️ Auto-closed: no contributor engaged within 3 days. Reopen or add a label if this is still relevant.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Issue cleanup failed");
        }
    }
}
