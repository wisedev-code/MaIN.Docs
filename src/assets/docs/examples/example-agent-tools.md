# 🧠 Anthropic Tool Example

The **AnthropicToolExample** demonstrates how to integrate **Anthropic’s Claude** model with **tool usage**, enabling the model to interact with external functions such as listing, reading, and saving notes.

### 📝 Code Example

```csharp
public async Task Start()
{
    AnthropicExample.Setup();
    Console.WriteLine("(Anthropic) Tool example is running!");

    var context = await AIHub.Agent()
        .WithModel("claude-sonnet-4-5-20250929")
        .WithSteps(StepBuilder.Instance
            .Answer()
            .Build())
        .WithTools(new ToolsConfigurationBuilder()
            .AddTool<ListNotesArgs>(
                "list_notes",
                "List all available notes",
                new
                {
                    type = "object",
                    properties = new
                    {
                        folder = new { type = "string", description = "Notes folder", @default = "notes" }
                    }
                },
                NoteTools.ListNotes)
            .AddTool<ReadNoteArgs>(
                "read_note",
                "Read the content of a specific note",
                new
                {
                    type = "object",
                    properties = new
                    {
                        noteName = new
                            { type = "string", description = "Name of the note (without .txt extension)" }
                    },
                    required = new[] { "noteName" }
                },
                NoteTools.ReadNote)
            .AddTool<SaveNoteArgs>(
                "save_note",
                "Save or update a note with new content",
                new
                {
                    type = "object",
                    properties = new
                    {
                        noteName = new
                            { type = "string", description = "Name of the note (without .txt extension)" },
                        content = new { type = "string", description = "Content to save in the note" }
                    },
                    required = new[] { "noteName", "content" }
                },
                NoteTools.SaveNote)
            .WithToolChoice("auto")
            .Build())
        .CreateAsync(interactiveResponse: true);

    await context.ProcessAsync("What notes do I currently have?");
    
    Console.WriteLine("--//--");
    
    await context.ProcessAsync("Create a new note for a shopping list that includes healthy foods.");
}
```

## 🔹 How It Works

1. **Initialize Anthropic API** → `AnthropicExample.Setup()` configures access to the Anthropic API.
2. **Create an AI agent** → `AIHub.Agent()` initializes a Claude-powered agent instance.
3. **Select a model** → `.WithModel("claude-sonnet-4-5-20250929")` specifies the Claude model version to use.
4. **Define reasoning steps** → `.WithSteps(StepBuilder.Instance.Answer().Build())` sets up how the agent processes input.
5. **Add tools** → `.WithTools(...)` registers functions that the model can call:

   * **`list_notes`** → Lists available notes in a folder.
   * **`read_note`** → Reads the content of a specified note.
   * **`save_note`** → Creates or updates a note with new content.
6. **Automatic tool selection** → `.WithToolChoice("auto")` allows Claude to decide when to use a tool.
7. **Enable interactive responses** → `.CreateAsync(interactiveResponse: true)` starts the agent with live, tool-enabled interaction.
8. **Process user inputs** → `context.ProcessAsync()` handles queries and executes tool actions when appropriate.

This example demonstrates how **Claude can combine natural language understanding with structured tool use**, enabling intelligent, context-aware operations such as managing and updating note data through simple conversational commands.
