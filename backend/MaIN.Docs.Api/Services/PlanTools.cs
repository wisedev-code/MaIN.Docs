using System.Text.Json;
using System.Text.Json.Serialization;
using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

public static class PlanTools
{
    [JsonConverter(typeof(StepArgConverter))]
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

    private sealed class StepArgConverter : JsonConverter<StepArg>
    {
        public override StepArg Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Model occasionally double-serializes each step as a JSON string — unwrap and parse
            if (reader.TokenType == JsonTokenType.String)
                return ParseFromDocument(reader.GetString()!);

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unexpected token {reader.TokenType} parsing StepArg");

            string? title = null, description = null, codeSnippet = null, language = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                // Normalize: strip quotes the model may embed as property names ('"title"' → 'title')
                var key = reader.GetString()!.Trim('"').ToLowerInvariant();

                if (!reader.Read()) break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var val = reader.GetString();
                    switch (key)
                    {
                        case "title":       title       = val; break;
                        case "description": description = val; break;
                        case "codesnippet": codeSnippet = val; break;
                        case "language":    language    = val; break;
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            return new StepArg(title ?? "", description ?? "", codeSnippet, language);
        }

        public override void Write(Utf8JsonWriter writer, StepArg value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("title", value.Title);
            writer.WriteString("description", value.Description);
            if (value.CodeSnippet is not null) writer.WriteString("codeSnippet", value.CodeSnippet);
            if (value.Language is not null)    writer.WriteString("language", value.Language);
            writer.WriteEndObject();
        }

        private static StepArg ParseFromDocument(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return new StepArg(
                    GetStr(root, "title", "Title") ?? "",
                    GetStr(root, "description", "Description") ?? "",
                    GetStr(root, "codeSnippet", "CodeSnippet"),
                    GetStr(root, "language", "Language")
                );
            }
            catch
            {
                return new StepArg("", json);
            }
        }

        private static string? GetStr(JsonElement el, params string[] names)
        {
            foreach (var n in names)
                if (el.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                    return p.GetString();
            return null;
        }
    }
}
