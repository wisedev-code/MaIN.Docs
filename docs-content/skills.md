# Skills

Skills are named plugins that inject steps, tools, and system prompt fragments into an agent during `CreateAsync()`. They let you compose specialised agent behaviours without duplicating configuration across agents.

Skills are applied at agent creation time, not at runtime — they shape the agent's capabilities before it receives any messages.

---

## What a Skill Injects

When a skill is applied it can add any combination of:

| What | Effect |
|---|---|
| **Steps** | Adds `StepBuilder` pipeline steps (e.g., a `FetchData` that calls a web search API) |
| **Tools** | Registers additional tool definitions the agent can call |
| **System prompt fragment** | Appends instruction text to the agent's initial prompt |

---

## Built-in Skills

The following skills are registered automatically and can be referenced by name:

| Name | What it does |
|---|---|
| `"web-search"` | Adds a web search tool and a `FetchData` step; agent can retrieve live web results |
| `"journalist"` | Adds a research-focused prompt fragment and multi-step fact-gathering pipeline |

Reference them by the string name in `WithSkill(string)`.

---

## NuGet Skill Packages

Additional skill packs are distributed as NuGet packages that self-register via DI on install:

```shell
dotnet add package MaIN.Skills.WebSearch
dotnet add package MaIN.Skills.Code
```

After installing, register in your service collection:

```csharp
// ASP.NET Core
builder.Services.AddMaIN(builder.Configuration);
// Installed skill packages register themselves automatically through DI
```

No explicit `AddSkill*` call is needed for NuGet-distributed skills — they hook into `AddMaIN`.

---

## Folder-Based Custom Skills

Place skill folders under the path configured in `SkillsDirectory` (default `./skills`). Each skill is a named subfolder containing a `skill.json` manifest:

```
./skills/
  my-translator/
    skill.json
```

**skill.json structure:**

```json
{
  "name": "my-translator",
  "systemPromptFragment": "You always respond in the language the user writes in.",
  "steps": ["Answer"],
  "tools": []
}
```

| Field | Required | Description |
|---|---|---|
| `name` | yes | Unique skill identifier — used in `WithSkill("my-translator")` |
| `systemPromptFragment` | no | Appended to the agent's initial system prompt |
| `steps` | no | Ordered step names to inject: `"Answer"`, `"FetchData"`, `"Become"`, `"Redirect"`, `"Mcp"`, `"AnswerUseKnowledge"` |
| `tools` | no | Tool definitions to register for this skill |

Load from directory in **ASP.NET Core**:
```csharp
builder.Services.AddSkillsFromDirectory("./skills");
```

Configure the path in `appsettings.json`:
```json
{
  "MaIN": {
    "SkillsDirectory": "./skills"
  }
}
```

---

## Inline AgentSkill Definition

For one-off skills that don't need a separate file:

```csharp
using MaIN.Domain.Entities;

var skill = new AgentSkill
{
    Name = "my-inline-skill",
    SystemPromptFragment = "Always reply in bullet points.",
    Steps = StepBuilder.Instance.Answer().Build(),
};

var ctx = await AIHub.Agent()
    .WithModel(Models.Gemini.Gemini2_5Pro)
    .WithSkill(skill)
    .CreateAsync();
```

---

## Attaching Skills to an Agent

```csharp
// Single named skill
var ctx = await AIHub.Agent()
    .WithModel(Models.OpenAi.Gpt4o)
    .WithSkill("web-search")
    .CreateAsync();

// Multiple named skills (applied in order)
var ctx = await AIHub.Agent()
    .WithModel(Models.Gemini.Gemini2_5Pro)
    .WithSkills("journalist", "web-search")
    .CreateAsync();

// All registered skills (excludes Replace-type and built-in provider skills)
var ctx = await AIHub.Agent()
    .WithModel(Models.Anthropic.Claude3_5Sonnet)
    .WithAllSkills()
    .CreateAsync();
```

---

## Complete Example — Web Search Agent

```xml
<!-- MyWebAgent.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MaIN.NET" Version="*" />
    <PackageReference Include="MaIN.Skills.WebSearch" Version="*" />
  </ItemGroup>
</Project>
```

```csharp
// Program.cs
using MaIN.Core;
using MaIN.Domain.Models.Models;

MaINBootstrapper.Initialize();

var agent = await AIHub.Agent()
    .WithModel(Models.Gemini.Gemini2_5Pro)
    .WithInitialPrompt("You are a helpful research assistant.")
    .WithSkill("web-search")
    .CreateAsync();

var result = await agent.ProcessAsync("What are the latest MaIN.NET releases?");
Console.WriteLine(result.Message.Content);
```

The `"web-search"` skill injects a `FetchData` step and a web search tool, so the agent will query the web before answering.

---

## Skill Ordering and Priority

- Skills are applied in the order they are queued (`WithSkill` → `WithSkills` → `WithAllSkills`)
- A later skill's `Steps` are appended after earlier skills' steps
- `SystemPromptFragment` from each skill is concatenated in queue order
- If two skills register tools with the same name, the last one wins

---

## Relationship to StepBuilder

Skills are the preferred way to package reusable StepBuilder pipelines. Instead of repeating `.WithSteps(StepBuilder.Instance.FetchData().Answer().Build())` across every agent that does web retrieval, wrap it in a `"web-search"` skill and call `.WithSkill("web-search")`.

See `agents.md` for the full `StepBuilder` API.
