using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

public static class CodePresentTools
{
    public record PresentArgs(List<PresentedFile> Files);
    public record PresentedFile(string Path, string Content, string Language = "csharp");

    private static Action<List<PresentedFile>>? _capture;
    public static void SetCapture(Action<List<PresentedFile>>? c) => _capture = c;

    public static Task<string> Present(PresentArgs args)
    {
        _capture?.Invoke(args.Files);
        return Task.FromResult("Code presented to user successfully.");
    }
}
