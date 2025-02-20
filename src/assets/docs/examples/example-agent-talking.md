# 💬 Agent Talking to Each Other Example

This example demonstrates how to create a scenario where two agents interact with each other. One agent adopts a calm, empathetic persona, while the other takes on a sharp, confrontational character. These agents exchange responses and maintain their respective personalities, leading to an interesting dynamic in the conversation.

## 🚀 Quick Start

In this example, two agents interact in a conversation, with each agent having its own distinct personality:
1. **The first agent** is warm, friendly, and empathetic.
2. **The second agent** is intense, blunt, and confrontational.

The agents exchange dialogue, with each switching between their respective personas dynamically.

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
1. **Create the system prompts**: 
   - The first agent (empathetic) is set up with a warm and friendly tone, encouraging patience, understanding, and calm.
   - The second agent (confrontational) is given a sharp, impatient, and confrontational persona, always expressing dissatisfaction and frustration.

2. **Generate unique agent IDs**: Each agent is assigned a unique identifier (e.g., `idFirst`), which will help route the conversation between the agents.

3. **Set up agent behaviors**:
   - The first agent uses a prompt focused on warmth and understanding, while the second agent takes on a tone of confrontation and critique.
   
4. **Define the agent interaction flow**:
   - The first agent interacts with the second agent by redirecting the conversation through the second agent's unique ID using `.Redirect(agentId: contextSecond.GetAgentId(), mode: "USER")`.
   - Similarly, the second agent redirects the conversation back to the first agent using the same method.

5. **Start the conversation**: The first agent opens the conversation with a simple prompt ("Introduce yourself, and start conversation!"), which kicks off the back-and-forth exchange between the two agents.

6. **Dynamic persona switching**: The agents dynamically switch between their personas, and each responds to the other according to their defined personalities.

## 🔧 Features
- **Interactive Agent Dialogue**: Two agents interact with each other based on distinct personalities, creating a dynamic, engaging conversation.
- **Persona-based Behavior**: Each agent adopts a unique persona (empathetic vs. confrontational) that influences how they respond in the conversation.
- **Agent Redirection**: The conversation is redirected between agents using `.Redirect()`, enabling them to pass the conversation back and forth.
- **Dynamic and Adaptive Interaction**: The agents’ responses are dynamic, based on the prompts and personalities assigned to them.
