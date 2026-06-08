using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

public static class PlanTools
{
    public record StepArg(string Title, string Description, string? CodeSnippet = null, string? Language = null);
    public record ProposePlanArgs(string Title, string Context, List<StepArg> Steps);

    private static Action<PlanProposal>? _capture;

    public static void SetCapture(Action<PlanProposal>? capture) => _capture = capture;

    public static Task<object> Propose(ProposePlanArgs args)
    {
        var plan = new PlanProposal(
            args.Title,
            args.Context,
            args.Steps.Select(s => new PlanStep(s.Title, s.Description, s.CodeSnippet, s.Language)).ToList());

        _capture?.Invoke(plan);
        return Task.FromResult<object>(new { proposed = true, title = args.Title, stepCount = args.Steps.Count });
    }
}
