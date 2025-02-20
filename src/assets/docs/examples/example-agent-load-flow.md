# 📂 Agents Flow Loaded Example

This example demonstrates how to load an existing agent flow and process it. The flow, which contains multiple agents performing different tasks, is loaded from a saved file (in this case, `poetry.zip`), and then executed to complete the task.

In this case, we load the flow that was created in the previous **Agents Composed as Flow Example**, which involves two agents collaborating to write a poem about the distant future and then transforming it into a rap.

## 🚀 Quick Start

The example showcases how to load a flow from a `.zip` file that contains pre-defined agents. Once the flow is loaded, it can be executed by providing a prompt.

### 📝 Code Example

```csharp
public class AgentsFlowLoadedExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agents flow example (loading)");

        var flowContext = AIHub.Flow()
            .Load("./poetry.zip");
        
        await flowContext
            .ProcessAsync("Write a poem about distant future");
    }
}
```

## 🔹 How It Works
1. **Load Flow**:
   - The `AIHub.Flow().Load("./poetry.zip")` method loads the pre-saved flow from the `poetry.zip` file. This file contains the configuration and details of the agents involved in the flow.
   
2. **Run Flow**:
   - After loading the flow, the `ProcessAsync()` method is called with a prompt. In this case, the prompt is `"Write a poem about distant future"`. The flow will start executing with the loaded agents, following the predefined steps.

3. **Execution of Tasks**:
   - The agents in the loaded flow will work together to generate a poem and then transform it into a rap, as defined in the previous example.

## 🔧 Features
- **Flow Loading**: This example allows you to load a previously saved flow, ensuring that you don't need to recreate the agent setup each time.
- **Reusability**: By saving the flow as a `.zip` file, you can reuse the same set of agents and interactions across different sessions or projects.
- **Seamless Execution**: Once the flow is loaded, it runs seamlessly with a single command to process the task at hand.
