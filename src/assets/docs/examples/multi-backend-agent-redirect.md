# 🤖 Multi-Backend Agents Poetry Transformation Example

This example demonstrates how two agents with different language models and distinct writing styles can work together to transform content. The first agent generates elegant poetry that the second agent then reimagines in contemporary street style, showcasing the versatility of using multiple AI backends in a coordinated workflow.

## 🚀 Quick Start

In this example, one agent is a refined poet using a local model, while the other is a modern rap lyricist using OpenAI's cloud model. The first agent creates sophisticated verse about the distant future, which the second agent then transforms into energetic rap lyrics with contemporary slang.

### 📝 Code Example

```csharp
public async Task Start()
{
    Console.WriteLine("Basic multi backend agent&friends example is running!");

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

    var contextSecond = await AIHub.Agent()
        .WithBackend(BackendType.OpenAi)
        .WithModel("gpt-4o")
        .WithInitialPrompt(systemPromptSecond)
        .CreateAsync(interactiveResponse: true);
    
    var context = await AIHub.Agent()
        .WithModel("gemma2:2b")
        .WithInitialPrompt(systemPrompt)
        .WithSteps(StepBuilder.Instance
            .Answer()
            .Redirect(agentId: contextSecond.GetAgentId())
            .Build())
        .CreateAsync();
    
    await context
        .ProcessAsync("Write a poem about distant future");
}
```

## 🔹 How It Works

1. **Create two specialized system prompts**:
   - The first agent is configured as a refined poet with elegant language and lyrical imagery.
   - The second agent is configured as a modern rap lyricist who transforms formal poetry into street-smart rhythmic verses.

2. **Set up different AI backends**:
   - The first agent uses the local `gemma2:2b` model for generating traditional poetry.
   - The second agent uses OpenAI's cloud-based `gpt-4o` model for transforming the poetry into rap lyrics.

3. **Initialize the second agent** → `AIHub.Agent()` with `BackendType.OpenAi` and the rap lyricist system prompt, configured for interactive responses.

4. **Initialize the first agent** → `AIHub.Agent()` with the local `gemma2:2b` model and the refined poet system prompt, configured to redirect its output to the second agent.

5. **Setup workflow process** → Using `StepBuilder.Instance`, the first agent is set to answer the prompt and then redirect its poetic creation to the second agent for stylistic transformation.

6. **Trigger the workflow** → The process begins with a request to write a poem about the distant future, initiating the content generation and transformation sequence.

## 🔧 Features

- **Hybrid model deployment**: Combines the efficiency of local models with the capabilities of cloud-based models, demonstrating flexible architecture.

- **Style transformation pipeline**: Shows how content can be created by one specialized agent and then transformed by another with different stylistic instructions.

- **Cross-platform AI interaction**: Demonstrates how different AI backends (local and OpenAI) can be coordinated to work together in a seamless workflow.

- **Creative content transformation**: Illustrates a practical application of using multiple agents for creative content generation and stylistic adaptation.