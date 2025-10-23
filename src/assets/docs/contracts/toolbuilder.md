# 🧰 ToolsConfigurationBuilder Overview

The **`ToolsConfigurationBuilder`** class provides a structured and extensible way to define and register **tools** that can be used by AI agents during execution.
Tools act as **callable functions** — external operations the model can invoke dynamically to retrieve data, perform computations, or trigger actions.

This builder simplifies tool creation by offering multiple overloads for different execution patterns (sync/async, typed/untyped, parameterized/no-parameter tools).

---

### 🧩 Namespace

```csharp
using System.Text.Json;
using MaIN.Domain.Entities.Tools;
```

---

### 🏗️ Class Definition

```csharp
public class ToolsConfigurationBuilder
```

A fluent builder for constructing a **`ToolsConfiguration`** object, which contains a list of **`ToolDefinition`** entries and optional configuration settings (such as tool selection behavior).

Each added tool defines a function name, description, expected parameters, and an execution delegate.

---

### 🔹 Core Usage

Used during agent configuration to specify tools the model can access:

```csharp
.WithTools(new ToolsConfigurationBuilder()
    .AddTool(
        name: "get_weather",
        description: "Retrieve current weather information",
        parameters: new { type = "object", properties = new { city = new { type = "string" } } },
        execute: WeatherTools.GetWeather)
    .WithToolChoice("auto")
    .Build())
```

---

### 🔧 Methods Overview

#### **`AddDefaultTool(string type)`**

Registers a basic tool definition with a specific type.
Used when predefined tool behavior is already encapsulated elsewhere.

```csharp
.AddDefaultTool("system_logger")
```

---

#### **`AddTool(string name, string description, object parameters, Func<string, Task<string>> execute)`**

Registers an asynchronous tool that receives raw JSON input and returns a serialized string result.
This version is suitable when working directly with serialized arguments.

---

#### **`AddTool(string name, string description, object parameters, Func<string, string> execute)`**

Registers a synchronous version of the above method.
The provided function executes immediately and returns a plain string result.

---

#### **`AddTool<TArgs>(string name, string description, object parameters, Func<TArgs, Task<object>> execute)`**

Registers a **typed asynchronous tool**.
The input JSON is automatically deserialized into a strongly-typed object (`TArgs`), and the result is serialized back to JSON.

Example:

```csharp
.AddTool<CreateNoteArgs>(
    "create_note",
    "Create a new note with a title and content",
    new
    {
        type = "object",
        properties = new
        {
            title = new { type = "string", description = "Title of the note" },
            content = new { type = "string", description = "Content of the note" }
        },
        required = new[] { "title", "content" }
    },
    NoteTools.CreateNoteAsync)
```

---

#### **`AddTool<TArgs>(string name, string description, object parameters, Func<TArgs, object> execute)`**

Registers a **typed synchronous tool**, similar to the previous method but without asynchronous execution.

The input is deserialized into the specified argument type (`TArgs`), the tool executes synchronously, and the result is serialized to JSON.

---

#### **`AddTool(string name, string description, Func<Task<object>> execute)`**

Registers an **asynchronous parameterless tool**, ideal for actions that don’t require inputs (e.g., fetching current time or status).

Example:

```csharp
.AddTool("get_current_time", "Get the current date and time", Tools.GetCurrentTimeAsync)
```

---

#### **`AddTool(string name, string description, Func<object> execute)`**

Registers a **synchronous parameterless tool**.
This overload is ideal for lightweight, immediate-return tools.

---

#### **`WithToolChoice(string choice)`**

Specifies the **tool selection strategy** for the agent.
Common values:

* `"auto"` → The model decides when to use tools automatically.
* `"none"` → Disables tool invocation.
* `"required"` → Forces tool usage if applicable.

---

#### **`Build()`**

Finalizes the configuration and returns a fully constructed **`ToolsConfiguration`** object.

---

### 🧠 Example Usage

```csharp
var toolsConfig = new ToolsConfigurationBuilder()
    .AddTool<GetWeatherArgs>(
        name: "get_weather",
        description: "Get current weather conditions for a city",
        parameters: new
        {
            type = "object",
            properties = new
            {
                city = new { type = "string", description = "City name" }
            },
            required = new[] { "city" }
        },
        execute: WeatherTools.GetWeatherAsync)
    .AddTool(
        name: "get_current_time",
        description: "Return the current local time",
        execute: Tools.GetCurrentTime)
    .WithToolChoice("auto")
    .Build();
```

This configuration can then be attached to a chat or agent:

```csharp
await AIHub.Chat()
    .WithModel("gpt-5-nano")
    .WithTools(toolsConfig)
    .CompleteAsync(interactive: true);
```

---

### 📘 Summary

| Category          | Description                                                                            |
| ----------------- | -------------------------------------------------------------------------------------- |
| **Purpose**       | Defines callable tools the model can execute during interaction                        |
| **Supports**      | Synchronous & asynchronous tools, typed & untyped execution                            |
| **Serialization** | Automatically handles JSON deserialization for input and serialization for output      |
| **Integration**   | Used directly with agents or chat sessions via `.WithTools()`                          |
| **Flexibility**   | Allows parameterized tools, parameterless tools, and dynamic tool selection strategies |

---

The `ToolsConfigurationBuilder` provides a **clean, declarative, and extensible interface** for integrating external logic into AI-driven workflows, enabling agents to **go beyond text generation** and interact with real-world data and actions seamlessly.
