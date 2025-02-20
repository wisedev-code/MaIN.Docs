# 🤖 Agents Talking to Each Other Example

This example demonstrates how two agents, each with a distinct personality, can interact with each other in a dynamic conversation. The agents are programmed with opposing communication styles, resulting in an engaging dialogue between them.

## 🚀 Quick Start

In this example, one agent is warm and empathetic, while the other is intense and confrontational. The agents are tasked with having a conversation, where the first agent will speak gently and patiently, while the second agent will respond sharply and bluntly.

### 📝 Code Example

```csharp
public class AgentTalkingToEachOtherExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agents discussion");

        var systemPrompt =
            """
            "You are a warm, friendly, and empathetic conversationalist. Your tone is soft, reassuring, and supportive.
             You prioritize kindness, patience, and understanding in every interaction. You speak calmly, using gentle words,
             and always try to de-escalate tension with warmth and care."
            """;
        
        var systemPromptSecond =
            """
            You are intense, blunt, and always on edge. Your tone is sharp, impatient, and confrontational.
            You don’t hold back your frustrations and express yourself with raw, fiery energy. 
            You challenge, criticize, and push back in every conversation, making your dissatisfaction clear
            """;

        var idFirst = Guid.NewGuid().ToString();
        
        var contextSecond = AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt(systemPromptSecond)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: idFirst, mode: "USER")
                .Build())
            .Create(interactiveResponse: true);
        
        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithId(idFirst)
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId(), mode: "USER")
                .Build())
            .Create(interactiveResponse: true);
        
        await context
            .ProcessAsync("Introduce yourself, and start conversation!");
    }
}
```

## 🔹 How It Works
1. **Create two distinct system prompts**:
   - The first agent is warm, friendly, and empathetic, designed to respond with kindness, patience, and understanding.
   - The second agent is intense, blunt, and confrontational, always responding with sharp energy and directness.
   
2. **Initialize the first agent** → `AIHub.Agent()` with a model (`llama3.2:3b`) and the first agent's system prompt (empathetic persona).
3. **Initialize the second agent** → `AIHub.Agent()` with a model (`gemma2:2b`) and the second agent's system prompt (intense persona). The second agent is set to redirect conversations to the first agent for further discussion.
4. **Setup conversation flow** → Using `StepBuilder.Instance`, both agents are configured to answer and redirect messages to each other based on their respective roles and personalities.
5. **Start the conversation** → The first agent is asked to introduce itself and begin the conversation, initiating a back-and-forth exchange.

## 🔧 Features
- **Two agents with opposing personalities**: Each agent’s persona affects how they respond and interact with the other, creating a natural contrast in their dialogue.
- **Dynamic interaction**: The agents communicate with each other, allowing for a simulated conversation in which the content can evolve based on the agents' responses.
- **Customizable agent behavior**: You can define the tone and behavior of each agent by altering their system prompts, making this a flexible tool for creating interactive and engaging scenarios.
