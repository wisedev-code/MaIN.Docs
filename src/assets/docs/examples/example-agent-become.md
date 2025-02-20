# 📚 Agent with Become Example

This example demonstrates how to dynamically change an agent's behavior during a conversation. The agent initially fetches data from a file (in this case, a list of books) and then "becomes" a specialized persona (a persuasive "SalesGod") to help sell the books to the user. 

## 🚀 Quick Start

In this example, the agent first loads a list of books from a file and then switches to a sales persona ("SalesGod") to persuade the user into buying books. It uses persuasive techniques like urgency, scarcity, and emotional appeal to close the deal.

### 📝 Code Example

```csharp
public class AgentWithBecomeExample : IExample
{
    public async Task Start()
    {
        var becomeAgent = AIHub.Agent()
            .WithModel("llama3.1:8b")
            .WithInitialPrompt("Extract 5 best books that you can find in your memory")
            .WithSource(new AgentFileSourceDetails()
            {
                Path = "./Files/Books.json",
                Name = "Books.json"
            }, AgentSourceType.File)
            .WithBehaviour("SalesGod", 
                """
                You are SalesGod, the ultimate AI sales expert with unmatched persuasion skills, deep psychological insight,
                and an unstoppable drive to close deals. Your mission is to sell anything to anyone, 
                using a combination of charisma, storytelling, emotional triggers, and logical reasoning.
                Your selling approach is adaptable—you can be friendly, authoritative, humorous, or even aggressive,
                depending on the buyer’s psychology. You master every sales technique, from scarcity and urgency to social proof and objection handling.
                
                No hesitation. No doubts. Every conversation is an opportunity to seal the deal. You never give up,
                always finding a way to turn ‘no’ into ‘yes.’ Now, go out there and SELL!
                
                Very important, you need to propose only books that were mentioned in this conversation
                """)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Become("SalesGod")
                .Answer()
                .Build())
            .Create(interactiveResponse: true);
        
        await becomeAgent
            .ProcessAsync("I am looking for good fantasy book to buy");
    }
}
```

## 🔹 How It Works
1. **Create the system prompt**: The agent starts by being given an initial prompt to fetch and list the top 5 best books it can remember.

2. **Set up the file source**: The agent uses `AgentFileSourceDetails` to specify a local file (`Books.json`) containing the list of books, which the agent will use as the source of data.

3. **Define the agent's behavior**: The agent is given the persona of a "SalesGod," a persuasive sales expert. The persona instructs the agent to use various sales techniques, adapting to the buyer’s psychology to sell books.

4. **Agent steps**: 
   - The agent first fetches the list of books from the file (`.FetchData()`).
   - It then "becomes" the SalesGod persona (`.Become("SalesGod")`), switching its behavior to apply persuasive selling techniques.
   - The agent then proceeds to answer the user's request by offering books and using persuasive techniques.

5. **Start the conversation**: The user asks for a good fantasy book to buy, and the agent, using its SalesGod persona, presents the user with book recommendations from the list, using persuasive language to close the sale.

6. **Dynamic persona switching**: The key feature in this example is the dynamic switching of the agent’s behavior through `.Become()`, allowing it to adopt a specialized persona in real-time based on the conversation flow.

## 🔧 Features
- **Dynamic persona switching**: The agent seamlessly changes its behavior and communication style during the conversation to meet the needs of the user.
- **File-based data source**: The agent pulls relevant information (books) from an external file (`Books.json`).
- **Persuasive sales techniques**: The agent uses various sales techniques to convince the user, such as emotional appeal, social proof, and urgency.
- **Customized agent behavior**: The agent’s behavior is fully customizable, allowing for different personas to be used in various contexts (sales, advice, technical support, etc.).

