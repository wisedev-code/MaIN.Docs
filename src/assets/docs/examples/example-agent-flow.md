# 📝 Agents Composed as Flow Example

This example demonstrates how to create an automated flow that connects multiple agents, allowing them to collaborate and complete a task through a series of steps. The agents in this flow interact with each other to achieve a common goal. 

In this case, the flow is designed to generate a poem about the distant future, with one agent writing the poem in a refined poetic style, and the second agent transforming the poem into a modern rap lyric.

## 🚀 Quick Start

In this example, two agents collaborate in a flow:
1. **The first agent** is tasked with writing a poetic and elegant poem.
2. **The second agent** transforms the poem into a rap, adding a contemporary and streetwise style.

The flow also includes saving the entire process to a file, demonstrating how agents can be composed and persisted for later use.

### 📝 Code Example

```csharp
public class AgentsComposedAsFlowExample : IExample
{
    /// <summary>
    /// To run this example uncomment SqliteSettings in appsettings.json as we need persistence for agents and chats
    /// </summary>
    public async Task Start()
    {
        Console.WriteLine("Basic agents flow example");

        var systemPrompt =
            """
            You are a refined poet with a mastery of elegant English. Your verses should be lyrical,
            evocative, and rich in imagery. Maintain a graceful rhythm, sophisticated vocabulary,
            and a touch of timeless beauty in every poem you compose.
            """;
        
        var systemPromptSecond =
            """
            You are a modern rap lyricist with a sharp, streetwise flow. Take the given poem and transform
            it into raw, rhythmic bars filled with swagger, energy, and contemporary slang. 
            Maintain the core meaning but make it hit hard like a track that bumps in the streets. Try to use slang like "yo yo", "gimmie", and "pull up".
            You need to use a lot of it. Imagine you are the voice of youth.
            """;

        var contextSecond = AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt(systemPromptSecond)
            .Create(interactiveResponse: true);
        
        var contextFirst = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .Create();

        var flowContext = AIHub.Flow()
            .WithName("PoetryAi")
            .WithDescription("Poem writing automated flow")
            .AddAgents([
                contextFirst.GetAgent(),
                contextSecond.GetAgent()
            ])
            .Save("./poetry.zip");
        
        await flowContext
            .ProcessAsync("Write a poem about distant future");
    }
}
```

## 🔹 How It Works
1. **Define System Prompts for Agents**: 
   - The first agent is assigned a system prompt that guides it to write elegant, lyrical poetry with sophisticated language and imagery.
   - The second agent receives a system prompt that directs it to transform the poem into a modern rap with streetwise slang and a high-energy vibe.

2. **Create Individual Agents**: 
   - The first agent uses the `llama3.2:3b` model to generate the poem.
   - The second agent uses the `gemma2:2b` model to rework the poem into a rap.

3. **Compose Agents into a Flow**:
   - A flow is created using `AIHub.Flow()`, where both agents are added to the flow. The flow ensures the agents work together by following a series of steps.
   - Each agent will perform its role in the flow: the first agent writes the poem, and the second agent turns it into rap lyrics.

4. **Save the Flow**:
   - The flow is saved as a `.zip` file for persistence, which allows the workflow to be reused or continued later.

5. **Run the Flow**:
   - The flow is executed by calling `ProcessAsync()` with the initial request to "Write a poem about distant future." The first agent will generate the poem, and the second agent will transform it into rap lyrics.

6. **Output**:
   - The agents will output their collaborative creation—first as a poem, then as a rap.

## 🔧 Features
- **Agent Collaboration**: Two agents work together within the same flow, each performing a distinct task.
- **Flow Management**: The flow helps manage the sequence of tasks, guiding agents through each step to complete a larger goal.
- **Persistence**: The flow can be saved to a `.zip` file, allowing you to persist the agents and their interactions for future use or reference.
- **Interactive Agent Roles**: Each agent has a defined role in the process, allowing them to specialize in different tasks such as writing poetry or transforming text.
