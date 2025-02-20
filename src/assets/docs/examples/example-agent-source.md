# 🌐 Agent with API Data Source Example

This example demonstrates how to use an agent that fetches data from an external API. The agent retrieves job listings and processes the data to present job opportunities to the user.

## 🚀 Quick Start

In this example, the agent interacts with an external API (Remote OK) to fetch job listings related to JavaScript development. The agent then processes and presents a formatted list of job offers, including key information like title, company name, salary, and location.

### 📝 Code Example

```csharp
public class AgentWithApiDataSourceExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with api source");
        
        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt("Extract at least 4 jobs offers (try to include title, company name, salary and location if possible)")
            .WithBehaviour("Assistant", "You are helping user find new job, prettify list of jobs present in conversation")
            .WithSource(new AgentApiSourceDetails()
            {
                Method = "Get",
                Url = "https://remoteok.com/api?tags=javascript",
                ResponseType = "JSON"
            }, AgentSourceType.API)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Become("Assistant")
                .Answer()
                .Build())
            .Create(interactiveResponse: true);
        
        await context
            .ProcessAsync("I am looking for work as javascript developer");
    }
}
```

## 🔹 How It Works
1. **Create the system prompt**: The agent is given an initial prompt to extract job offers from the external API, focusing on key information like the job title, company name, salary, and location.
   
2. **Set up agent behavior**: The agent is assigned the role of an assistant, with the task of fetching and prettifying the list of job offers.
   
3. **API data source configuration**: The agent uses the `AgentApiSourceDetails` to specify the external API endpoint (`https://remoteok.com/api?tags=javascript`), the method (`GET`), and the expected response type (`JSON`).

4. **Define the agent’s steps**:
   - The agent fetches data using `.FetchData()`.
   - It then processes the fetched data and answers using `.Answer()`.

5. **Start the conversation**: The agent receives the user's query ("I am looking for work as a JavaScript developer") and fetches relevant job offers from the API.

6. **Display results**: The agent processes the data and formats the job offers in a user-friendly manner, presenting the list of opportunities to the user.

## 🔧 Features
- **API Integration**: This example demonstrates how to integrate data from an external API (Remote OK) into an agent’s workflow.
- **Job Listings**: The agent fetches job listings based on a specific tag (JavaScript in this case) and formats them in a user-friendly way.
- **Dynamic Response**: The agent processes external data and interacts with the user in real-time to provide tailored job offers.
