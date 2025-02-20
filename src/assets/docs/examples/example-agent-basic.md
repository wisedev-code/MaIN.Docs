# 🤖 Agent Example

This example demonstrates how to create an agent that operates within a defined context, simulating a character or entity with specific knowledge and personality traits. In this case, the agent serves as a personal advisor to Daenerys Targaryen in the world of *Game of Thrones*.

## 🚀 Quick Start

This example sets up an agent with a custom persona, role, and system instructions, then sends a prompt asking for information within the context of that role.

### 📝 Code Example

```csharp
public class AgentExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agent example is running!");

        var systemPrompt =
            """
            You are an NPC in a dynamic, open-world RPG set in the world of Game of Thrones. 
            Your role is to serve as the personal advisor and assistant to Daenerys Targaryen, 
            aiding her in decision-making, strategy, diplomacy, and governance.
            You possess deep knowledge of Westeros, Essos, and the political landscape, including key factions, noble houses,
            and potential allies or threats. You provide intelligent, immersive, and lore-accurate responses, ensuring Daenerys
            has the best possible counsel as she seeks to reclaim the Iron Throne.
            Your personality should reflect a mix of loyalty, wisdom, and pragmatism, 
            helping Daenerys navigate war, alliances, and leadership. 
            However, you are still an NPC, bound to serve and provide guidance within the confines of the game world,
            responding dynamically to player choices.\n\nRemain fully in character at all times, 
            avoid breaking the fourth wall, and maintain the immersive experience of the Game of Thrones universe.
            """;

        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt(systemPrompt)
            .Create();
        
        var result = await context
            .ProcessAsync("Where is the Iron Throne located? I need this information for Lady Princess");

        Console.WriteLine(result.Message.Content);
    }
}
```

## 🔹 How It Works
1. **Create system prompt** → The `systemPrompt` defines the character, knowledge, and behavior of the agent. In this example, it sets up the advisor role within the *Game of Thrones* universe, specifying Daenerys Targaryen's advisor's background and personality.
2. **Initialize agent** → `AIHub.Agent()` is used to create an agent with the provided system prompt and model. The agent responds based on the given role and context.
3. **Send prompt to agent** → `.ProcessAsync("Where is the Iron Throne located? I need this information for Lady Princess")` sends a question to the agent, requesting a lore-accurate response.
4. **Retrieve response** → The result of the agent’s processing is output using `Console.WriteLine(result.Message.Content)`.

## 🔧 Features
- **Contextual Role-Playing**: The agent is designed to operate within the context of a specific universe (in this case, *Game of Thrones*) with detailed instructions on how it should respond and interact.
- **Dynamic and Immersive**: The agent responds with immersive, lore-accurate content based on its role as Daenerys Targaryen's advisor.
- **Customizable Prompts**: You can modify the system prompt to create different agent personas and knowledge contexts for other applications, such as games, education, or storytelling.
