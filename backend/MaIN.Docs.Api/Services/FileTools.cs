namespace MaIN.Docs.Api.Services;

public static class FileTools
{
    public record ShowFileArgs(string Path, string Content, string Language = "");

    private static Action<(string Path, string Content, string Language)>? _capture;

    public static void SetCapture(Action<(string Path, string Content, string Language)>? capture)
        => _capture = capture;

    public static Task<object> Show(ShowFileArgs args)
    {
        _capture?.Invoke((args.Path, args.Content, args.Language));
        return Task.FromResult<object>(new { shown = true, path = args.Path });
    }
}
