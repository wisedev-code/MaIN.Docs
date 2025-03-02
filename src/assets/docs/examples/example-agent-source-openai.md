# 🌐 Agent with Web Data Source Example

This example demonstrates how to use an agent that fetches real-time data directly from a webpage. The agent retrieves daily news from a specified website (in this case, BBC) and processes the content to present a formatted newsletter to the user.

## 🚀 Quick Start

In this example, the agent interacts with the web source (BBC homepage) to fetch current news articles. It then processes the data to present a newsletter-style format, including titles, descriptions, and links for each article.

### 📝 Code Example

```csharp
public async Task Start()
{
    Console.WriteLine("Agent with web source (OpenAi)");
    
    OpenAiExample.Setup(); // We need to provide OpenAi API key

    var context = await AIHub.Agent()
        .WithModel("gpt-4o-mini")
        .WithInitialPrompt("Find useful information about daily news, try to include title, description and link.")
        .WithBehaviour("Journalist", "Based on data provided in chat, find useful information about what happened today. Build it in the form of a newsletter.")
        .WithSource(new AgentWebSourceDetails()
        {
            Url = "https://www.bbc.com/", // The webpage to fetch data from
        }, AgentSourceType.Web)
        .WithSteps(StepBuilder.Instance
            .FetchData() // Fetch the data from the web
            .Become("Journalist") // Adopt the role of a journalist
            .Answer() // Process and answer
            .Build())
        .CreateAsync(interactiveResponse: true); // Enable interactive responses

    await context
        .ProcessAsync("Provide today's newsletter"); // User request for daily news summary
}
```

## 🔹 How It Works
1. **Set up OpenAI API**: The OpenAI API is set up with the necessary credentials using `OpenAiExample.Setup()`.

2. **Create the system prompt**: The agent is initialized with a prompt instructing it to fetch useful information from the daily news, emphasizing the need to extract the title, description, and link of each article.

3. **Assign agent behavior**: The agent is given the role of a "Journalist," with the task of summarizing the day's news into a newsletter format.

4. **Web data source configuration**: The agent uses `AgentWebSourceDetails` to specify the URL of the source website (`https://www.bbc.com/`), indicating that the agent should scrape data from this page.

5. **Define the agent's steps**:
   - The agent fetches data using `.FetchData()`, pulling content from the specified webpage.
   - It then assumes the "Journalist" role to process and summarize the data into a newsletter.
   - The agent finally presents the formatted summary using `.Answer()`.

6. **Start the conversation**: The user requests the newsletter ("Provide today's newsletter"), prompting the agent to fetch and process the latest news from the BBC website.

7. **Display results**: The agent formats the fetched news into a structured newsletter and presents it back to the user.

## 🔧 Features
- **Web Integration**: This example demonstrates how the agent fetches real-time data directly from a webpage (BBC in this case) to retrieve and process content.
- **Dynamic Newsletter Generation**: The agent creates a daily newsletter by extracting key details from the webpage, including article titles, descriptions, and links.
- **Real-Time Content Processing**: The agent not only pulls data from the web but also processes and presents it in an organized format based on the user request.

This example showcases how effortlessly the framework can pull data from the web and format it into a user-friendly presentation, making it perfect for building applications that require real-time, dynamic content generation.