namespace MaIN.Docs.Api.Services;

public static class ArtifactTools
{
    public record FileEntry(string Path, string Content);
    public record GenerateArgs(string ArchiveName, List<FileEntry> Files, string Description);
    public record ProposeArgs(string ArchiveName, string Description);

    private static ArtifactService _svc = null!;
    private static Action<string>? _urlCapture;
    private static Action<(string ArchiveName, string Description)>? _proposalCapture;

    public static void Init(ArtifactService svc) => _svc = svc;
    public static void SetCapture(Action<string>? capture) => _urlCapture = capture;
    public static void SetProposalCapture(Action<(string ArchiveName, string Description)>? capture) => _proposalCapture = capture;

    public static async Task<object> Generate(GenerateArgs args)
    {
        var files = args.Files.Select(f => (f.Path, f.Content)).ToList();
        var url = await _svc.CreateZipAndPresign(args.ArchiveName, files);
        _urlCapture?.Invoke(url);
        return new { url, archiveName = args.ArchiveName, description = args.Description };
    }

    public static Task<object> Propose(ProposeArgs args)
    {
        _proposalCapture?.Invoke((args.ArchiveName, args.Description));
        return Task.FromResult<object>(new { proposed = true, archiveName = args.ArchiveName });
    }
}
